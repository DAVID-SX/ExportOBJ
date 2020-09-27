using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExportObj
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class TestClass : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            Material mat = doc.GetElement(new ElementId(524616)) as Material;
            string result = "";
            result += "newmtl " + Regex.Replace(mat.Name, @"[\s]{1,}", "_") + "\n";
            result += "Ka " + (mat.Color.Red / 255.0).ToString("0.000000000000000") + " "
                            + (mat.Color.Green / 255.0).ToString("0.000000000000000") + " "
                            + (mat.Color.Blue / 255.0).ToString("0.000000000000000") + "\n";
            result += "Kd " + (mat.Color.Red / 255.0).ToString("0.000000000000000") + " "
                            + (mat.Color.Green / 255.0).ToString("0.000000000000000") + " "
                            + (mat.Color.Blue / 255.0).ToString("0.000000000000000") + "\n";
            result += "d " + (1.0 - Convert.ToDouble(mat.Transparency) / 100).ToString() + "\n";
            if (GetMapPath(doc, mat) != null)
            {
                string mapPath = GetMapPath(doc, mat);
                string fullMapPath = GetFullMapPath(mapPath);
                string mapName = Path.GetFileName(fullMapPath);
                result += "map_Ka " + Regex.Replace(mat.Name, @"[\s]{1,}","_") + "\n";
                result += "map_Kd " + mapName.Replace(" ", "_") + "\n";
            }
            TaskDialog.Show("result", result);
            return Result.Succeeded;

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
                        if (matAsset[i].Name == "generic_diffuse" || matAsset[i].Name == "masonrycmu_color" || matAsset[i].Name == "ceramic_color")
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
        public static string GetFullMapPath(string str)
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
    }
}
