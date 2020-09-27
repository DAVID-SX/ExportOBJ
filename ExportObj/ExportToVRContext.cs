using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExportToObj
{
    internal class ExportToObjContext : IExportContext
    {
        /// 构造函数
        public ExportToObjContext(Document document, AllViews families)
        {
            this.m_document = document;
            this.m_TransformationStack.Push(Transform.Identity);
            this.m_AllViews = families;
        }
        /// 接口内方法
        public bool Start()
        {
            return true;
        }

        public void Finish()
        {
        }

        public bool IsCanceled()
        {
            // 字段m_cancelled声明时，初始值为false
            return this.m_cancelled;
        }

        public void OnPolymesh(PolymeshTopology node)
        {

            // 获取Transform
            Transform transform = this.m_TransformationStack.Peek();
            //string text = "Anim";    //?
            // 旋转轴
            Transform transFormed = Transform.CreateRotation(XYZ.BasisX, 270 * Math.PI / 180.0);
            // 与AllViews的交互
            int numberOfUVs = node.NumberOfUVs;
            int numberOfNormals = node.NumberOfNormals;
            this.nbofPoints = node.NumberOfPoints;
            this.nboffacets = node.NumberOfFacets;
            this.TotalFacNB = node.NumberOfFacets;
            this.m_AllViews.TotalNbofPoints = this.m_AllViews.TotalNbofPoints + node.NumberOfPoints;

            ///
            ///根据法线的情况导出数据点
            ///
            // 每个点都有一个法线时的导出方式
            if (node.DistributionOfNormals == DistributionOfNormals.AtEachPoint)
            {
                // 写入顶点数据
                for (int i = 0; i < node.GetPoints().Count; i++)
                {
                    XYZ transformedPoint = transFormed.OfPoint(transform.OfPoint(node.GetPoint(i)));
                    string xPoint = transformedPoint.X.ToString().Replace(",", ".");
                    string yPoint = transformedPoint.Y.ToString().Replace(",", ".");
                    string zPoint = transformedPoint.Z.ToString().Replace(",", ".");
                    this.m_AllViews.XYZsbuilder.Append(string.Concat(new string[]
                    {
                                "\nv ",
                                xPoint,
                                " ",
                                yPoint,
                                " ",
                                zPoint
                    }));
                }
                // 写入法线数据
                for (int j = 0; j < node.GetNormals().Count; j++)
                {
                    XYZ transformedNormal = transFormed.OfPoint(transform.OfPoint(node.GetNormal(j)));
                    string xNormal = transformedNormal.X.ToString().Replace(",", ".");
                    string yNormal = transformedNormal.Y.ToString().Replace(",", ".");
                    string zNormal = transformedNormal.Z.ToString().Replace(",", ".");
                    this.m_AllViews.NORMALsbuilder.Append(string.Concat(new string[]
                    {
                                "\nvn ",
                                xNormal,
                                " ",
                                yNormal,
                                " ",
                                zNormal
                    }));
                }
            }
            else
            {
                // 一个面只有一个法线时的写入方式
                if (node.DistributionOfNormals == DistributionOfNormals.OnePerFace)
                {
                    // 写入顶点数据
                    for (int k = 0; k < node.GetPoints().Count; k++)
                    {
                        XYZ transformPoint = transform.OfPoint(node.GetPoint(k));
                        XYZ transformedPoint = transFormed.OfPoint(transformPoint);
                        string xPoint = transformedPoint.X.ToString().Replace(",", ".");
                        string yPoint = transformedPoint.Y.ToString().Replace(",", ".");
                        string zPoint = transformedPoint.Z.ToString().Replace(",", ".");
                        this.m_AllViews.XYZsbuilder.Append(string.Concat(new string[]
                        {
                                    "\nv ",
                                    xPoint,
                                    " ",
                                    yPoint,
                                    " ",
                                    zPoint
                        }));
                    }
                    // 写入法线数据
                    XYZ transformedNormal = transFormed.OfPoint(transform.OfPoint(node.GetNormal(0)));
                    string xNormal = transformedNormal.X.ToString().Replace(",", ".");
                    string yNormal = transformedNormal.Y.ToString().Replace(",", ".");
                    string zNormal = transformedNormal.Z.ToString().Replace(",", ".");
                    this.m_AllViews.NORMALsbuilder.Append(string.Concat(new string[]
                    {
                                "\nvn ",
                                xNormal,
                                " ",
                                yNormal,
                                " ",
                                zNormal
                    }));
                }
                // 所有面只有一个法线时的导出方式
                else
                {
                    // 写入顶点数据
                    for (int l = 0; l < node.GetPoints().Count; l++)
                    {
                        XYZ transformedPoint = transFormed.OfPoint(transform.OfPoint(node.GetPoint(l)));
                        string xPoint = transformedPoint.X.ToString().Replace(",", ".");
                        string yPoint = transformedPoint.Y.ToString().Replace(",", ".");
                        string zPoint = transformedPoint.Z.ToString().Replace(",", ".");
                        this.m_AllViews.XYZsbuilder.Append(string.Concat(new string[]
                        {
                                    "\nv ",
                                    xPoint,
                                    " ",
                                    yPoint,
                                    " ",
                                    zPoint
                        }));
                    }
                }
            }
            ///
            ///根据法线情况导出三角面信息
            ///
            // 每个三角面都有法线时（f*/*）行及（Vn * * *）行的导出方式
            if (node.DistributionOfNormals == DistributionOfNormals.OnEachFacet)
            {
                // 输出格式
                // g ****
                // usemtl *****
                // f **/** **/** **/**
                this.ElementName = this.GetCurrentElementName();
                this.m_AllViews.FCTbyMATsbuilder.Append("\ng " + this.MaterialFaceName + "\nusemtl " + this.MaterialFaceName);
                for (int m = 0; m < node.GetFacets().Count; m++)
                {
                    PolymeshFacet facet = node.GetFacet(m);
                    string firstPointNBOfFacet = (this.facNB + 1 + facet.V1).ToString();
                    string secondPointNBOfFacet = (this.facNB + 1 + facet.V2).ToString();
                    string thirdPointNBOfFacet = (this.facNB + 1 + facet.V3).ToString();
                    this.m_AllViews.FCTbyMATsbuilder.Append(string.Concat(new string[]
                    {
                                "\nf ",
                                firstPointNBOfFacet,
                                "/",
                                firstPointNBOfFacet,
                                " ",
                                secondPointNBOfFacet,
                                "/",
                                secondPointNBOfFacet,
                                " ",
                                thirdPointNBOfFacet,
                                "/",
                                thirdPointNBOfFacet
                    }));
                }
                // 写入法线信息
                this.m_AllViews.TotalfacNB = this.m_AllViews.TotalfacNB + this.m_AllViews.TotalNbofPoints;
                this.facNB += this.nbofPoints;
                for (int n = 0; n < node.GetNormals().Count; n++)
                {
                    string xNormal = transform.OfPoint(node.GetNormal(n)).X.ToString().Replace(",", ".");
                    string yNormal = transform.OfPoint(node.GetNormal(n)).Y.ToString().Replace(",", ".");
                    string zNormal = transform.OfPoint(node.GetNormal(n)).Z.ToString().Replace(",", ".");
                    this.m_AllViews.NORMALsbuilder.Append(string.Concat(new string[]
                    {
                                "\nvn ",
                                xNormal,
                                " ",
                                yNormal,
                                " ",
                                zNormal
                    }));
                }
            }
            else
            {
                this.ElementName = this.GetCurrentElementName();
                this.m_AllViews.FCTbyMATsbuilder.Append("\ng " + this.MaterialFaceName + "\nusemtl " + this.MaterialFaceName);
                for (int num3 = 0; num3 < node.GetFacets().Count; num3++)
                {
                    PolymeshFacet facet2 = node.GetFacet(num3);
                    string firstPointNBOfFacet = (this.facNB + 1 + facet2.V1).ToString();
                    string secondPointNBOfFacet = (this.facNB + 1 + facet2.V2).ToString();
                    string thirdPointNBOfFacet = (this.facNB + 1 + facet2.V3).ToString();
                    this.m_AllViews.FCTbyMATsbuilder.Append(string.Concat(new string[]
                    {
                                "\nf ",
                                firstPointNBOfFacet,
                                "/",
                                firstPointNBOfFacet,
                                " ",
                                secondPointNBOfFacet,
                                "/",
                                secondPointNBOfFacet,
                                " ",
                                thirdPointNBOfFacet,
                                "/",
                                thirdPointNBOfFacet
                    }));
                }
                this.m_AllViews.TotalfacNB += this.m_AllViews.TotalNbofPoints;
                this.facNB += this.nbofPoints;
            }
            // 写入Vt信息
            if (node.NumberOfUVs > 0)
            {
                double num4 = 1.0;
                double num5 = 1.0;
                if (this.key_Materials != null)
                {
                    if (this.key_Materials.Count > 0)
                    {
                        foreach (object obj in this.key_Materials)
                        {
                            int num6 = (int)obj;
                            string b = Convert.ToString(this.h_MaterialNames[num6]);
                            if (this.MaterialFaceName == b)
                            {
                                num4 = Convert.ToDouble(this.h_modfU[num6]);
                                num5 = Convert.ToDouble(this.h_modfV[num6]);
                            }
                        }
                    }
                }
                for (int i = 0; i < node.GetUVs().Count; i++)
                {
                    UV uv = node.GetUV(i);
                    double num8 = uv.U / num4;
                    double num9 = uv.V / num5;
                    string text46 = num8.ToString();
                    string str = text46.Replace(",", ".");
                    string text47 = num9.ToString();
                    string str2 = text47.Replace(",", ".");
                    this.m_AllViews.UVsbuilder.Append("\nvt " + str + " " + str2);
                }
            }

        }

        public void OnRPC(RPCNode node)
        {
        }

        public RenderNodeAction OnViewBegin(ViewNode node)
        {
            return 0;
        }

        public void OnViewEnd(ElementId elementId)
        {
        }

        public RenderNodeAction OnElementBegin(ElementId elementId)
        {
            this.elementStack.Push(elementId);
            return 0;
        }

        public void OnElementEnd(ElementId elementId)
        {
            this.elementStack.Pop();
        }

        public RenderNodeAction OnFaceBegin(FaceNode node)
        {
            return 0;
        }

        public void OnFaceEnd(FaceNode node)
        {
        }

        public RenderNodeAction OnInstanceBegin(InstanceNode node)
        {
            this.m_TransformationStack.Push(this.m_TransformationStack.Peek().Multiply(node.GetTransform()));
            return 0;
        }

        public void OnInstanceEnd(InstanceNode node)
        {
            this.m_TransformationStack.Pop();
        }

        public RenderNodeAction OnLinkBegin(LinkNode node)
        {
            ElementId symbolId = node.GetSymbolId();
            RevitLinkType revitLinkType = this.m_document.GetElement(symbolId) as RevitLinkType;
            string name = revitLinkType.Name;
            foreach (object obj in this.m_document.Application.Documents)
            {
                Document document = (Document)obj;
                if (document.Title.Equals(name))
                {
                    this.ZeLinkDoc = document;
                }
            }
            if (name != null)
            {
                this.isLink = true;
            }
            this.m_TransformationStack.Push(this.m_TransformationStack.Peek().Multiply(node.GetTransform()));
            return 0;
        }

        public void OnLinkEnd(LinkNode node)
        {
            this.m_TransformationStack.Pop();
        }

        public void OnLight(LightNode node)
        {
        }

        public void OnMaterial(MaterialNode node)
        {
            // 链接模型的导出
            if (this.isLink)
            {
                this.currentMaterialId = node.MaterialId;
                int num = 40;
                if (node != null)
                {
                    if (node.MaterialId != null)
                    {
                        this.currentMaterialId = node.MaterialId;
                        string text = this.currentMaterialId.ToString();
                        string text2 = text;
                        string text3 = text2.Replace(" ", "_");
                        if (text3.Length > num)
                        {
                            int num2 = text3.Length - num;
                            text3 = text3.Remove(text3.Length - num2);
                        }
                        if (text3 != null)
                        {
                            this.StartMatName = text3;
                        }
                        if (this.m_AllViews.ListMaterialName.Contains(this.StartMatName))
                        {
                            this.MaterialFaceName = this.StartMatName;
                        }
                        if (!this.m_AllViews.ListMaterialName.Contains(this.StartMatName))
                        {
                            string text4 = "0.5";
                            string text5 = "0.5";
                            string text6 = "0.5";
                            double num3 = 1.0;
                            if (node.Color != null)
                            {
                                text4 = (Convert.ToDouble(node.Color.Red) / 255.0).ToString();
                                text5 = (Convert.ToDouble(node.Color.Green) / 255.0).ToString();
                                text6 = (Convert.ToDouble(node.Color.Blue) / 255.0).ToString();
                            }
                            if (node.Transparency.ToString() != null)
                            {
                                num3 = 1.0 - Convert.ToDouble(node.Transparency);
                            }
                            if (this.m_AllViews.LinkTransparent)
                            {
                                num3 = 0.5;
                            }
                            string text7 = text4;
                            string text8 = text7.Replace(",", ".");
                            string text9 = text5;
                            string text10 = text9.Replace(",", ".");
                            string text11 = text6;
                            string text12 = text11.Replace(",", ".");
                            string text13 = num3.ToString();
                            string text14 = text13.Replace(",", ".");
                            this.MaterialFaceName = this.StartMatName;
                            this.m_AllViews.MATERIALsbuilder.Append(string.Concat(new string[]
                            {
                                "newmtl ",
                                this.StartMatName,
                                "\nKa ",
                                text8,
                                " ",
                                text10,
                                " ",
                                text12,
                                "\nKd ",
                                text8,
                                " ",
                                text10,
                                " ",
                                text12,
                                "\nd ",
                                text14,
                                "\n"
                            }));
                            this.m_AllViews.ListMaterialName.Add(this.StartMatName);
                        }
                    }
                }
            }
            if (!this.isLink)
            {
                if (node != null)
                {
                    if (node.MaterialId.IntegerValue.ToString() == "-1")
                    {
                        if (this.CurrentElement.Category != null)
                        {
                            this.StartMatName = this.CurrentElement.Category.Name.ToString();
                        }
                        string startMatName = this.StartMatName;
                        string newStartMatName = startMatName.Replace(" ", "_");
                        newStartMatName = newStartMatName.Replace("\"", "");
                        newStartMatName = newStartMatName.Replace(":", "");
                        newStartMatName = newStartMatName.Replace(";", "");
                        newStartMatName = newStartMatName.Replace("'", "");
                        this.StartMatName = newStartMatName;
                        // 名字过长时截取名字
                        if (newStartMatName.Length > 40)
                        {
                            int num5 = newStartMatName.Length - 40;
                            this.StartMatName = newStartMatName.Remove(newStartMatName.Length - num5);
                        }
                        if (this.m_AllViews.ListMaterialName != null)
                        {
                            if (this.m_AllViews.ListMaterialName.Contains(this.StartMatName))
                            {
                                this.MaterialFaceName = this.StartMatName;
                            }
                        }
                        if (this.m_AllViews.ListMaterialName != null)
                        {
                            if (!this.m_AllViews.ListMaterialName.Contains(this.StartMatName))
                            {
                                string mtlRedColorValue = "0.5";
                                string mtlGreenColorValue = "0.5";
                                string mtlBuleColorValue = "0.5";
                                double mtlTransparencyValue = 1.0;
                                if (node.Color != null)
                                {
                                    mtlRedColorValue = (Convert.ToDouble(node.Color.Red) / 255.0).ToString();
                                    mtlGreenColorValue = (Convert.ToDouble(node.Color.Green) / 255.0).ToString();
                                    mtlBuleColorValue = (Convert.ToDouble(node.Color.Blue) / 255.0).ToString();
                                }
                                if (node.Transparency.ToString() != null)
                                {
                                    mtlTransparencyValue = 1.0 - Convert.ToDouble(node.Transparency);
                                }
                                string text19 = mtlRedColorValue;
                                string text20 = text19.Replace(",", ".");
                                string text21 = mtlGreenColorValue;
                                string text22 = text21.Replace(",", ".");
                                string text23 = mtlBuleColorValue;
                                string text24 = text23.Replace(",", ".");
                                string text25 = mtlTransparencyValue.ToString();
                                string text26 = text25.Replace(",", ".");
                                this.MaterialFaceName = newStartMatName;
                                this.m_AllViews.MATERIALsbuilder.Append(string.Concat(new string[]
                                {
                                    "newmtl ",
                                    newStartMatName,
                                    "\nKa ",
                                    text20,
                                    " ",
                                    text22,
                                    " ",
                                    text24,
                                    "\nKd ",
                                    text20,
                                    " ",
                                    text22,
                                    " ",
                                    text24,
                                    "\nd ",
                                    text26,
                                    "\n"
                                }));
                                this.m_AllViews.ListMaterialName.Add(newStartMatName);
                            }
                        }
                    }
                }
                if (node != null & node.MaterialId.IntegerValue.ToString() != "-1")
                {
                    this.currentMaterialId = node.MaterialId;
                    bool flag44 = this.currentMaterialId != ElementId.InvalidElementId & !this.currentMaterialId.IntegerValue.ToString().Contains("-");
                    if (flag44)
                    {
                        Material material = this.m_document.GetElement(this.currentMaterialId) as Material;
                        string name = material.Name;
                        string text27 = name.Replace(" ", "_");
                        bool flag45 = text27.Length > 40;
                        if (flag45)
                        {
                            int num7 = text27.Length - 40;
                            text27 = text27.Remove(text27.Length - num7);
                        }
                        bool flag114 = this.m_AllViews.GroupingOptions == 6;
                        if (flag114)
                        {
                            bool flag115 = this.m_AllViews.ListMaterialName.Contains(text27);
                            if (flag115)
                            {
                                this.MaterialFaceName = text27;
                            }
                            bool flag116 = !this.m_AllViews.ListMaterialName.Contains(text27);
                            if (flag116)
                            {
                                this.MaterialFaceName = text27;
                                this.modfU = 1.0;
                                this.modfV = 1.0;
                                this.angle = 360.0;
                                this.Otherpathvalue = null;
                                ElementId appearanceAssetId2 = material.AppearanceAssetId;
                                bool flag117 = appearanceAssetId2.ToString() != "-1";
                                if (flag117)
                                {
                                    this.m_AllViews.ImageExist = false;
                                    AppearanceAssetElement appearanceAssetElement2 = this.m_document.GetElement(appearanceAssetId2) as AppearanceAssetElement;
                                    this.MaterialType = "Other";
                                    bool flag118 = this.MaterialType == "Other";
                                    if (flag118)
                                    {
                                        try
                                        {
                                            this.GetTheBitmaps(appearanceAssetElement2);
                                        }
                                        catch (Exception ex)
                                        {
                                        }
                                        bool imageExist = this.m_AllViews.ImageExist;
                                        if (imageExist)
                                        {
                                            bool tintOrNot5 = this.getTextureNoImage(appearanceAssetElement2, ExportToObjContext.TextureTypes.Diffuse).TintOrNot;
                                            bool colorOrNot5 = this.getTextureNoImage(appearanceAssetElement2, ExportToObjContext.TextureTypes.Diffuse).ColorOrNot;
                                            string name7 = material.Name;
                                            string text53 = name7.Replace(" ", "_");
                                            bool flag119 = text53.Length > 40;
                                            if (flag119)
                                            {
                                                int num21 = text53.Length - 40;
                                                text53 = text53.Remove(text53.Length - num21);
                                            }
                                            bool flag120 = !this.m_AllViews.ListMaterialName.Contains(text53);
                                            if (flag120)
                                            {
                                                this.m_AllViews.ListMaterialName.Add(text53);
                                                string text54 = (Convert.ToDouble(node.Color.Red) / 255.0).ToString();
                                                string text55 = (Convert.ToDouble(node.Color.Green) / 255.0).ToString();
                                                string text56 = (Convert.ToDouble(node.Color.Blue) / 255.0).ToString();
                                                double value5 = 1.0 - Convert.ToDouble(node.Transparency);
                                                double num22 = Math.Round(value5, 2);
                                                bool flag121 = this.getTextureNoImage(appearanceAssetElement2, ExportToObjContext.TextureTypes.Diffuse).textureCategorie == "water";
                                                if (flag121)
                                                {
                                                    num22 = 0.4;
                                                }
                                                bool flag122 = this.getTextureNoImage(appearanceAssetElement2, ExportToObjContext.TextureTypes.Diffuse).textureCategorie == "plastic";
                                                if (flag122)
                                                {
                                                    num22 = 0.5;
                                                }
                                                bool flag123 = this.getTextureNoImage(appearanceAssetElement2, ExportToObjContext.TextureTypes.Diffuse).textureCategorie == "glazing";
                                                if (flag123)
                                                {
                                                    num22 = 0.2;
                                                }
                                                bool flag124 = colorOrNot5;
                                                if (flag124)
                                                {
                                                    text54 = (Convert.ToDouble(this.getTextureNoImage(appearanceAssetElement2, ExportToObjContext.TextureTypes.Diffuse).GenericColor.Red) / 255.0).ToString();
                                                    text55 = (Convert.ToDouble(this.getTextureNoImage(appearanceAssetElement2, ExportToObjContext.TextureTypes.Diffuse).GenericColor.Green) / 255.0).ToString();
                                                    text56 = (Convert.ToDouble(this.getTextureNoImage(appearanceAssetElement2, ExportToObjContext.TextureTypes.Diffuse).GenericColor.Blue) / 255.0).ToString();
                                                }
                                                bool flag125 = tintOrNot5;
                                                if (flag125)
                                                {
                                                    text54 = (Convert.ToDouble(this.getTextureNoImage(appearanceAssetElement2, ExportToObjContext.TextureTypes.Diffuse).color.Red) / 255.0).ToString();
                                                    text55 = (Convert.ToDouble(this.getTextureNoImage(appearanceAssetElement2, ExportToObjContext.TextureTypes.Diffuse).color.Green) / 255.0).ToString();
                                                    text56 = (Convert.ToDouble(this.getTextureNoImage(appearanceAssetElement2, ExportToObjContext.TextureTypes.Diffuse).color.Blue) / 255.0).ToString();
                                                }
                                                string text57 = text54;
                                                text54 = text57.Replace(",", ".");
                                                string text58 = text55;
                                                text55 = text58.Replace(",", ".");
                                                string text59 = text56;
                                                text56 = text59.Replace(",", ".");
                                                string text60 = num22.ToString();
                                                string text61 = text60.Replace(",", ".");
                                                bool flag126 = this.Otherpathvalue != null;
                                                if (flag126)
                                                {
                                                    bool flag127 = this.Otherpathvalue.Contains("|");
                                                    if (flag127)
                                                    {
                                                        this.Otherpathvalue = this.Otherpathvalue.Substring(0, this.Otherpathvalue.IndexOf("|"));
                                                    }
                                                    bool flag128 = this.Otherpathvalue.Contains("\\") & this.Otherpathvalue.Contains("/");
                                                    if (flag128)
                                                    {
                                                        this.Otherpathvalue = this.Otherpathvalue.Replace("/", "\\");
                                                    }
                                                    bool flag129 = this.Otherpathvalue.Contains("\\\\");
                                                    if (flag129)
                                                    {
                                                        this.Otherpathvalue = this.Otherpathvalue.Replace("\\\\", "\\");
                                                    }
                                                    bool flag130 = File.Exists(this.Otherpathvalue);
                                                    if (flag130)
                                                    {
                                                        this.h_Materials.Add(this.keyNB, this.Otherpathvalue);
                                                        this.key_Materials = this.h_Materials.Keys;
                                                        this.h_MaterialNames.Add(this.keyNB, text53);
                                                        this.h_modfU.Add(this.keyNB, this.modfU);
                                                        this.h_modfV.Add(this.keyNB, this.modfV);
                                                        this.keyNB++;
                                                        this.textureExist = true;
                                                        string fileName3 = Path.GetFileName(this.Otherpathvalue);
                                                        string text62 = fileName3.Replace(" ", "_");
                                                        string text63 = text62;
                                                        this.m_AllViews.MATERIALsbuilder.Append(string.Concat(new string[]
                                                        {
                                                            "newmtl ",
                                                            text53,
                                                            "\nKa ",
                                                            text54,
                                                            " ",
                                                            text55,
                                                            " ",
                                                            text56,
                                                            "\nKd ",
                                                            text54,
                                                            " ",
                                                            text55,
                                                            " ",
                                                            text56,
                                                            "\nd ",
                                                            text61,
                                                            "\nmap_Ka ",
                                                            text63,
                                                            "\nmap_Kd ",
                                                            text63,
                                                            "\n"
                                                        }));
                                                    }
                                                    bool flag131 = !File.Exists(this.Otherpathvalue);
                                                    if (flag131)
                                                    {
                                                        this.m_AllViews.MATERIALsbuilder.Append(string.Concat(new string[]
                                                        {
                                                            "newmtl ",
                                                            text53,
                                                            "\nKa ",
                                                            text54,
                                                            " ",
                                                            text55,
                                                            " ",
                                                            text56,
                                                            "\nKd ",
                                                            text54,
                                                            " ",
                                                            text55,
                                                            " ",
                                                            text56,
                                                            "\nd ",
                                                            text61,
                                                            "\n"
                                                        }));
                                                    }
                                                }
                                                bool flag132 = this.Otherpathvalue == null;
                                                if (flag132)
                                                {
                                                    this.m_AllViews.MATERIALsbuilder.Append(string.Concat(new string[]
                                                    {
                                                        "newmtl ",
                                                        text53,
                                                        "\nKa ",
                                                        text54,
                                                        " ",
                                                        text55,
                                                        " ",
                                                        text56,
                                                        "\nKd ",
                                                        text54,
                                                        " ",
                                                        text55,
                                                        " ",
                                                        text56,
                                                        "\nd ",
                                                        text61,
                                                        "\n"
                                                    }));
                                                }
                                            }
                                        }
                                        bool flag133 = !this.m_AllViews.ImageExist;
                                        if (flag133)
                                        {
                                            string text64 = "0.5";
                                            string text65 = "0.5";
                                            string text66 = "0.5";
                                            double num23 = 1.0;
                                            bool flag134 = node.Color != null;
                                            if (flag134)
                                            {
                                                text64 = (Convert.ToDouble(node.Color.Red) / 255.0).ToString();
                                                text65 = (Convert.ToDouble(node.Color.Green) / 255.0).ToString();
                                                text66 = (Convert.ToDouble(node.Color.Blue) / 255.0).ToString();
                                            }
                                            bool flag135 = node.Transparency.ToString() != null;
                                            if (flag135)
                                            {
                                                num23 = 1.0 - Convert.ToDouble(node.Transparency);
                                            }
                                            string text67 = text64;
                                            string text68 = text67.Replace(",", ".");
                                            string text69 = text65;
                                            string text70 = text69.Replace(",", ".");
                                            string text71 = text66;
                                            string text72 = text71.Replace(",", ".");
                                            string text73 = num23.ToString();
                                            string text74 = text73.Replace(",", ".");
                                            this.MaterialFaceName = text27;
                                            this.m_AllViews.MATERIALsbuilder.Append(string.Concat(new string[]
                                            {
                                                "newmtl ",
                                                text27,
                                                "\nKa ",
                                                text68,
                                                " ",
                                                text70,
                                                " ",
                                                text72,
                                                "\nKd ",
                                                text68,
                                                " ",
                                                text70,
                                                " ",
                                                text72,
                                                "\nd ",
                                                text74,
                                                "\n"
                                            }));
                                            bool flag136 = !this.m_AllViews.ListMaterialName.Contains(text27);
                                            if (flag136)
                                            {
                                                this.m_AllViews.ListMaterialName.Add(text27);
                                            }
                                        }
                                    }
                                }
                                bool flag137 = appearanceAssetId2.ToString() == "-1";
                                if (flag137)
                                {
                                    string name8 = material.Name;
                                    string text75 = name8.Replace(" ", "_");
                                    bool flag138 = text75.Length > 40;
                                    if (flag138)
                                    {
                                        int num24 = text75.Length - 40;
                                        text75 = text75.Remove(text75.Length - num24);
                                    }
                                    bool flag139 = !this.m_AllViews.ListMaterialName.Contains(text75);
                                    if (flag139)
                                    {
                                        this.m_AllViews.ListMaterialName.Add(text75);
                                        string text76 = "0.5";
                                        string text77 = "0.5";
                                        string text78 = "0.5";
                                        double num25 = 1.0;
                                        string value6 = string.Concat(new object[]
                                        {
                                            "newmtl ",
                                            text75,
                                            "\nKa ",
                                            text76,
                                            " ",
                                            text77,
                                            " ",
                                            text78,
                                            "\nKd ",
                                            text76,
                                            " ",
                                            text77,
                                            " ",
                                            text78,
                                            "\nd ",
                                            num25,
                                            "\n"
                                        });
                                        this.m_AllViews.MATERIALsbuilder.Append(value6);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }




        // 属性
        private ElementId CurrentElementId
        {
            get
            {
                return (this.elementStack.Count > 0) ? this.elementStack.Peek() : ElementId.InvalidElementId;
            }
        }
        private Element CurrentElement
        {
            get
            {
                return this.m_document.GetElement(this.CurrentElementId);
            }
        }
        public string GetCurrentElementName()
        {
            if (this.CurrentElement != null)
            {
                return this.CurrentElement.Name;
            }
            else
            {
                return "";
            }
        }

        // 字段
        private Document m_document = null;
        private bool m_cancelled = false;
        private Stack<Transform> m_TransformationStack = new Stack<Transform>();
        private AllViews m_AllViews;
        public string sommets;
        public string nrmals;
        public string uvs;
        public string fcts;
        public string fctsByMaterials;
        public string fctsByEntities;
        public string ElementName;
        public string GroupTypeValue = null;
        public int facNB;
        public int xyzNB = 1;
        public int TotalFacNB;
        public int nb = 1;
        private int nbofPoints;
        private int nboffacets;
        public double modfU;
        public double modfV;
        public double angle;
        public bool textureExist = false;
        private int arrond = 5;
        public Hashtable h_modfU = new Hashtable();
        public Hashtable h_modfV = new Hashtable();
        public ICollection key_modfU = null;
        public Hashtable h_MaterialTexture = new Hashtable();
        public string newimagePath;
        public string Otherpathvalue;
        public string MaterialType;
        private string MaterialFaceName;
        private List<string> ListMaterialName = new List<string>();
        private List<string> ListMaterialGeneral = new List<string>();
        private List<string> ListMaterialNameGenTexture = new List<string>();
        private List<string> ListMaterialNameGenNoTexture = new List<string>();
        private List<string> ListMaterialNameOther = new List<string>();
        private List<string> ListByElementName = new List<string>();
        private List<string> ListByElementEntSubCat = new List<string>();
        private List<string> ListByElementSubCat = new List<string>();
        public bool radiobuttonSelect = false;
        public Hashtable h_Materials = new Hashtable();
        public Hashtable h_MaterialNames = new Hashtable();
        public ICollection key_Materials = null;
        private List<string> ListXYZ = new List<string>();
        private List<string> ListNormals = new List<string>();
        public StringBuilder XYZsbuilder = new StringBuilder();
        public StringBuilder NORMALsbuilder = new StringBuilder();
        public StringBuilder FCTsbuilder = new StringBuilder();
        public StringBuilder FCTbyMATsbuilder = new StringBuilder();
        public StringBuilder FCTbyENTsbuilder = new StringBuilder();
        public StringBuilder FCTbySUBCATsbuilder = new StringBuilder();
        public StringBuilder UVsbuilder = new StringBuilder();
        public StringBuilder MATERIALsbuilder = new StringBuilder();
        private string StartMatName = "RvtToUnityMat";
        private Stack<ElementId> elementStack = new Stack<ElementId>();
        private bool IsForVU = false;
        private bool isLink = false;
        private Document ZeLinkDoc = null;
        private ElementId currentMaterialId = ElementId.InvalidElementId;
        private int keyNB = 1;
        // 自定义方法
        public bool CatIsFurnitureAndMore()
        {
            bool result = false;
            bool flag = this.CurrentElement != null;
            if (flag)
            {
                Reference reference = new Reference(this.CurrentElement);
                bool flag2 = reference != null;
                if (flag2)
                {
                    Element element = this.m_document.GetElement(reference);
                    bool isForVU = this.IsForVU;
                    if (isForVU)
                    {
                        bool flag3 = element.Category.Id.IntegerValue == -2000080 | element.Category.Id.IntegerValue == -2001100 | element.Category.Id.IntegerValue == -2001000 | element.Category.Id.IntegerValue == -2001370;
                        if (flag3)
                        {
                            result = true;
                        }
                    }
                    else
                    {
                        result = true;
                    }
                }
            }
            return result;
        }
        private ExportToObjContext.TextureInfo getTextureNoImage(AppearanceAssetElement materialAsset, ExportToObjContext.TextureTypes tt)
        {
            ExportToObjContext.TextureInfo textureInfo = new ExportToObjContext.TextureInfo();
            bool flag = materialAsset != null;
            if (flag)
            {
                Asset renderingAsset = materialAsset.GetRenderingAsset();
                List<AssetProperty> list = new List<AssetProperty>();
                for (int i = 0; i < renderingAsset.Size; i++)
                {
                    AssetProperty item = renderingAsset.Get(i);
                    list.Add(item);
                }
                list = (from ap in list
                        orderby ap.Name
                        select ap).ToList<AssetProperty>();
                for (int j = 0; j < list.Count; j++)
                {
                    AssetProperty assetProperty = list[j];
                    bool flag2 = assetProperty.Name == "common_Tint_color";
                    if (flag2)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag3 = assetPropertyDoubleArray4d != null;
                        if (flag3)
                        {
                            textureInfo.color = assetPropertyDoubleArray4d.GetValueAsColor();
                        }
                    }
                    bool flag4 = assetProperty.Name == "common_Tint_toggle";
                    if (flag4)
                    {
                        AssetPropertyBoolean assetPropertyBoolean = assetProperty as AssetPropertyBoolean;
                        bool flag5 = assetPropertyBoolean != null;
                        if (flag5)
                        {
                            textureInfo.TintOrNot = assetPropertyBoolean.Value;
                        }
                    }
                    bool flag6 = assetProperty.Name == "ceramic_color";
                    if (flag6)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d2 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag7 = assetPropertyDoubleArray4d2 != null;
                        if (flag7)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d2.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                        }
                    }
                    bool flag8 = assetProperty.Name == "concrete_color";
                    if (flag8)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d3 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag9 = assetPropertyDoubleArray4d3 != null;
                        if (flag9)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d3.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                        }
                    }
                    bool flag10 = assetProperty.Name == "hardwood_tint_color";
                    if (flag10)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d4 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag11 = assetPropertyDoubleArray4d4 != null;
                        if (flag11)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d4.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                        }
                    }
                    bool flag12 = assetProperty.Name == "masonrycmu_color";
                    if (flag12)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d5 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag13 = assetPropertyDoubleArray4d5 != null;
                        if (flag13)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d5.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                        }
                    }
                    bool flag14 = assetProperty.Name == "metal_color";
                    if (flag14)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d6 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag15 = assetPropertyDoubleArray4d6 != null;
                        if (flag15)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d6.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                        }
                    }
                    bool flag16 = assetProperty.Name == "metallicpaint_base_color";
                    if (flag16)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d7 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag17 = assetPropertyDoubleArray4d7 != null;
                        if (flag17)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d7.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                        }
                    }
                    bool flag18 = assetProperty.Name == "mirror_tintcolor";
                    if (flag18)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d8 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag19 = assetPropertyDoubleArray4d8 != null;
                        if (flag19)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d8.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                        }
                    }
                    bool flag20 = assetProperty.Name == "plasticvinyl_color";
                    if (flag20)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d9 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag21 = assetPropertyDoubleArray4d9 != null;
                        if (flag21)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d9.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                        }
                    }
                    bool flag22 = assetProperty.Name == "plasticvinyl_type";
                    if (flag22)
                    {
                        AssetPropertyInteger assetPropertyInteger = assetProperty as AssetPropertyInteger;
                        bool flag23 = assetPropertyInteger != null;
                        if (flag23)
                        {
                            textureInfo.SubType = assetPropertyInteger.Value;
                            textureInfo.transparancy = 1.0;
                            bool flag24 = textureInfo.SubType == 1;
                            if (flag24)
                            {
                                textureInfo.textureCategorie = "plastic";
                            }
                        }
                    }
                    bool flag25 = assetProperty.Name == "glazing_transmittance_map";
                    if (flag25)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d10 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag26 = assetPropertyDoubleArray4d10 != null;
                        if (flag26)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d10.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                            textureInfo.textureCategorie = "glazing";
                        }
                    }
                    bool flag27 = assetProperty.Name == "solidglass_transmittance_custom_color";
                    if (flag27)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d11 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag28 = assetPropertyDoubleArray4d11 != null;
                        if (flag28)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d11.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                            textureInfo.textureCategorie = "glazing";
                        }
                    }
                    bool flag29 = assetProperty.Name == "wallpaint_color";
                    if (flag29)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d12 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag30 = assetPropertyDoubleArray4d12 != null;
                        if (flag30)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d12.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                        }
                    }
                    bool flag31 = assetProperty.Name == "water_tint_color";
                    if (flag31)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d13 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag32 = assetPropertyDoubleArray4d13 != null;
                        if (flag32)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d13.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                            textureInfo.textureCategorie = "water";
                        }
                    }
                    bool flag33 = assetProperty.Name == "generic_diffuse";
                    if (flag33)
                    {
                        AssetPropertyDoubleArray4d assetPropertyDoubleArray4d14 = assetProperty as AssetPropertyDoubleArray4d;
                        bool flag34 = assetPropertyDoubleArray4d14 != null;
                        if (flag34)
                        {
                            textureInfo.GenericColor = assetPropertyDoubleArray4d14.GetValueAsColor();
                            textureInfo.ColorOrNot = true;
                        }
                    }
                    bool flag35 = assetProperty.Name == "generic_transparency";
                    if (flag35)
                    {
                        AssetPropertyDouble assetPropertyDouble = assetProperty as AssetPropertyDouble;
                        bool flag36 = assetPropertyDouble != null;
                        if (flag36)
                        {
                            textureInfo.transparancy = assetPropertyDouble.Value;
                        }
                        bool flag37 = assetPropertyDouble.ToString() == "0";
                        if (flag37)
                        {
                            textureInfo.transparancy = 0.0;
                        }
                    }
                }
            }
            return textureInfo;
        }
        private void GetTheBitmaps(AppearanceAssetElement appearanceElem)
        {
            if (appearanceElem != null)
            {
                Asset renderingAsset = appearanceElem.GetRenderingAsset();
                int size = renderingAsset.Size;
                for (int i = 0; i < size; i++)
                {
                    AssetProperty assetProperty = renderingAsset.Get(i);
                    bool flag2 = assetProperty.NumberOfConnectedProperties < 1;
                    if (!flag2)
                    {
                        bool flag3 = false;
                        bool flag4 = assetProperty.Name.Contains("bump_map") | assetProperty.Name.Contains("pattern_map");
                        if (flag4)
                        {
                            flag3 = true;
                        }
                        Asset asset = assetProperty.GetConnectedProperty(0) as Asset;
                        bool flag5 = !flag3;
                        if (flag5)
                        {
                            bool flag6 = asset.Name == "UnifiedBitmapSchema" || asset.Name == "unifiedbitmap_Bitmap";
                            if (flag6)
                            {
                                AssetPropertyString assetPropertyString = asset.FindByName(UnifiedBitmap.UnifiedbitmapBitmap) as AssetPropertyString;
                                bool flag7 = assetPropertyString != null;
                                if (flag7)
                                {
                                    bool flag8 = assetPropertyString.Value.ToString() != "";
                                    if (flag8)
                                    {
                                        this.Otherpathvalue = assetPropertyString.Value;
                                        bool flag9 = this.Otherpathvalue.Contains("|");
                                        if (flag9)
                                        {
                                            this.Otherpathvalue = this.Otherpathvalue.Substring(0, this.Otherpathvalue.IndexOf("|"));
                                        }
                                        bool flag10 = this.Otherpathvalue.Contains("\\") & this.Otherpathvalue.Contains("/");
                                        if (flag10)
                                        {
                                            this.Otherpathvalue = this.Otherpathvalue.Replace("/", "\\");
                                        }
                                        bool flag11 = this.Otherpathvalue.Contains("\\") & this.Otherpathvalue.Contains("//");
                                        if (flag11)
                                        {
                                            this.Otherpathvalue = this.Otherpathvalue.Replace("//", "\\");
                                        }
                                        bool flag12 = this.Otherpathvalue.Contains("\\\\");
                                        if (flag12)
                                        {
                                            this.Otherpathvalue = this.Otherpathvalue.Replace("\\\\", "\\");
                                        }
                                        bool flag13 = !File.Exists(this.Otherpathvalue);
                                        if (flag13)
                                        {
                                            string str = "C:\\Program Files (x86)\\Common Files\\Autodesk Shared\\Materials\\Textures";
                                            string text = str + "\\" + this.Otherpathvalue;
                                            bool flag14 = File.Exists(text);
                                            if (flag14)
                                            {
                                                this.Otherpathvalue = text;
                                                this.m_AllViews.ImageExist = true;
                                            }
                                        }
                                        bool flag15 = File.Exists(this.Otherpathvalue);
                                        if (flag15)
                                        {
                                            this.m_AllViews.ImageExist = true;
                                        }
                                    }
                                }
                                AssetPropertyDistance assetPropertyDistance = asset.FindByName(UnifiedBitmap.TextureRealWorldScaleX) as AssetPropertyDistance;
                                bool flag16 = assetPropertyDistance != null;
                                if (flag16)
                                {
                                    this.modfU = UnitUtils.Convert(assetPropertyDistance.Value, assetPropertyDistance.DisplayUnitType, DisplayUnitType.DUT_DECIMAL_FEET);
                                }
                                AssetPropertyDistance assetPropertyDistance2 = asset.FindByName(UnifiedBitmap.TextureRealWorldScaleY) as AssetPropertyDistance;
                                bool flag17 = assetPropertyDistance2 != null;
                                if (flag17)
                                {
                                    this.modfV = UnitUtils.Convert(assetPropertyDistance2.Value, assetPropertyDistance2.DisplayUnitType, DisplayUnitType.DUT_DECIMAL_FEET);
                                }
                            }
                        }
                    }
                }
            }
        }
        public class TextureInfo
        {
            public TextureInfo()
            {
                this.texturePath = "";
                this.textureCategorie = "";
                this.sx = (this.sy = (this.ox = (this.oy = (this.angle = 0.0))));
                this.amount = 1.0;
                this.color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue);
                this.GenericColor = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue);
                this.transparancy = 1.0;
                this.SubType = 0;
            }
            public string texturePath;
            public string textureCategorie;
            public double sx;
            public double sy;
            public double ox;
            public double oy;
            public double angle;
            public Color color;
            public bool TintOrNot;
            public bool ColorOrNot;
            public bool ScaleOrNot;
            public Color GenericColor;
            public double transparancy;
            public double amount;
            public int SubType;
        }
        public enum TextureTypes
        {
            Diffuse = 1,
            Bump
        }
    }
}
