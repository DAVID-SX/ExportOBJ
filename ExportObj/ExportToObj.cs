using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;

namespace ExportToObj
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExportToObj : IExternalCommand
    {
        string mtlPath = "";
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // 获取文档
            Autodesk.Revit.ApplicationServices.Application application = commandData.Application.Application;
            Document document = commandData.Application.ActiveUIDocument.Document;
            // 定义报警消息框的相关信息
            MessageBoxIcon icon = MessageBoxIcon.Exclamation;
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            string caption = "模型数据导出";
            string versionNumber = application.VersionNumber;
            string subVersionNumber = application.SubVersionNumber;
            // 如果版本有问题就弹出对话框
            if (versionNumber != "2019" & versionNumber != "2020")
            {
                MessageBox.Show("该版本仅可在 Revit2019/2020 平台上运行！", caption, buttons, icon);
                return Result.Failed;
            }
            else
            {
                try
                {
                    AllViews allViews = new AllViews();
                    allViews.ObtainAllViews(commandData); //向AllViews的属性ViewListName中增加三维视图的名称
                    using (ExportViewsToVRForm exportViewsToVRForm = new ExportViewsToVRForm(commandData, allViews))
                    {
                        if (exportViewsToVRForm.ShowDialog() == DialogResult.OK)
                        {
                            return Result.Cancelled;
                        }
                        mtlPath = exportViewsToVRForm.MtlPath;
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    return Result.Failed;
                }
                try
                {
                    WriteMtlInfo(document, mtlPath);
                    //TaskDialog.Show("re", mtlPath);
                }
                catch (Exception e)
                {
                    TaskDialog.Show("Failed", e.ToString());
                }
                return Result.Succeeded;
            }
        }
        
        // 将新的材质信息写入mtl文件
        public void WriteMtlInfo(Document doc, string fullTargetFileName)
        {
            // 收集项目中的所有材质
            IList<Element> materialList = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Materials).ToElements();
            string result = "";
            // 获取有贴图材质的贴图路径
            foreach (Material mat in materialList)
            {
                result += "newmtl " + Regex.Replace(mat.Name, @"[\s]{1,}", "_") + "\n";
                result += "Ka " + (mat.Color.Red / 255.0).ToString("0.000000000000000") + " "
                                + (mat.Color.Green / 255.0).ToString("0.000000000000000") + " "
                                + (mat.Color.Blue / 255.0).ToString("0.000000000000000") + "\n";
                result += "Kd " + (mat.Color.Red / 255.0).ToString("0.000000000000000") + " "
                                + (mat.Color.Green / 255.0).ToString("0.000000000000000") + " "
                                + (mat.Color.Blue / 255.0).ToString("0.000000000000000") + "\n";
                result += "d " + (1.0 - Convert.ToDouble(mat.Transparency)/100).ToString() + "\n";
                if (GetMapPath(doc, mat) != null)
                {
                    string mapPath = GetMapPath(doc, mat);
                    string fullMapPath = GetFullMapPath(mapPath);
                    string mapName = Path.GetFileName(fullMapPath);
                    CopyFile(fullMapPath, Path.GetDirectoryName(fullTargetFileName), mat);
                    result += "map_Ka " + mapName + "\n";
                    result += "map_Kd " + mapName + "\n";
                }
            }
            using (StreamWriter sw = new StreamWriter(fullTargetFileName))
            {
                sw.Write(result);
            }
        }

        // 定义获取贴图路径的方法
        public string GetMapPath(Document doc, Material mat)
        {
            if (Convert.ToString(mat.AppearanceAssetId) != "-1") // 如果没有材质没有AppearanceAssetId就跳过
            {
                Asset matAsset = (doc.GetElement(mat.AppearanceAssetId) as AppearanceAssetElement).GetRenderingAsset();
                if (matAsset.Size != 0)
                {
                    for (int i = 0; i < matAsset.Size; i++)
                    {
                        if (matAsset[i].Name == "generic_diffuse" || matAsset[i].Name == "masonrycmu_color" || matAsset[i].Name == "ceramic_color" 
                            || matAsset[i].Name == "surface_albedo")
                        {
                            Asset diffuseAsset = matAsset[i].GetSingleConnectedAsset();
                            if (diffuseAsset is null) continue;
                            else
                            {
                                for (int j = 0; j < diffuseAsset.Size; j++)
                                {
                                    if (diffuseAsset[j].Name == "unifiedbitmap_Bitmap")
                                    {
                                        AssetPropertyString path = diffuseAsset[j] as AssetPropertyString;
                                        string bitMapPath = path.Value;
                                        return bitMapPath;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        // 定义获取完整材质路径的方法
        public string GetFullMapPath(string str)
        {
            // 以盘符加冒号形式开头的表示标准材质路径
            if (Regex.IsMatch(str, @"(^[A-Z]:\\)")) return str;
            // 如果字符串中包含'\'或者'/'则截取最后的文件名，此文件应该来自于默认的材质贴图库
            else if (str.LastIndexOf(@"\") != -1)
            {
                return Path.Combine(@"C:\Program Files (x86)\Common Files\Autodesk Shared\Materials\Textures\1\Mats",
                    str.Substring(str.LastIndexOf(@"\") + 1));
            }
            else if (str.LastIndexOf(@"/") != -1)
            {
                return Path.Combine(@"C:\Program Files (x86)\Common Files\Autodesk Shared\Materials\Textures\1\Mats",
                    str.Substring(str.LastIndexOf(@"/") + 1));
            }
            else
            {
                return Path.Combine(@"C:\Program Files (x86)\Common Files\Autodesk Shared\Materials\Textures\1\Mats", str);
            }
        }

        // 定义复制文件的方法
        public static void CopyFile(string sourceFilePath, string targetFolderPath, Material mat)
        {
            try
            {
                // 若目录不存在，建立目录
                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }
                // 根据目标文件夹及源文件路径复制文件
                String targetFilePath = Path.Combine(targetFolderPath, Path.GetFileName(sourceFilePath));
                //newPath.Add(targetFilePath);
                bool isrewrite = true;   // true=覆盖已存在的同名文件,false则反之
                File.Copy(sourceFilePath, targetFilePath, isrewrite);
            }
            catch (Exception)
            {
                TaskDialog.Show("失败提示！！！", "材质【" + mat.Name + "】的贴图复制失败，请手动更改材质贴图");
            }
        }

        // 定义修改贴图路径的方法
        public string ChangeRenderingTexturePath(Document doc, Material mat, string newPath)
        {
            try
            {
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("更改贴图位置");

                    using (AppearanceAssetEditScope editScope = new AppearanceAssetEditScope(doc))
                    {
                        Asset editableAsset = editScope.Start(mat.AppearanceAssetId);
                        // Getting the correct AssetProperty
                        AssetProperty assetProperty = editableAsset.FindByName("generic_diffuse");
                        if (assetProperty is null)
                        {
                            assetProperty = editableAsset.FindByName("masonrycmu_color");
                        }

                        Asset connectedAsset = assetProperty.GetConnectedProperty(0) as Asset;
                        // getting the right connected Asset
                        if (connectedAsset.Name == "UnifiedBitmapSchema")
                        {
                            AssetPropertyString path = connectedAsset.FindByName(UnifiedBitmap.UnifiedbitmapBitmap) as AssetPropertyString;
                            if (path.IsValidValue(newPath)) path.Value = newPath;
                        }
                        editScope.Commit(true);
                    }
                    t.Commit();
                    t.Dispose();
                }
                return mat.Name;
            }
            catch (Exception)
            {

                TaskDialog.Show("错误提示！！！", "材质【" + mat.Name + "】的贴图更改失败，请手动更改材质贴图");
                return null;
            }
        }
    }
}
