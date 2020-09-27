using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using ExportToObj;
using ExportToObj.Properties;

namespace ExportToObj
{
	public partial class ExportViewsToVRForm : System.Windows.Forms.Form
	{
		public ExportViewsToVRForm(ExternalCommandData commandData, AllViews allViews)
		{
			this.InitializeComponent();
			this.p_commandData = commandData;
			this.m_AllViews = allViews;
		}

		private void ViewForm_Load(object sender, EventArgs e)
		{
			this.listBoxViews.DataSource = this.m_AllViews.ViewListName;
			this.m_AllViews.VerticeNb = 0;
			this.m_AllViews.MaxVerticesPerObj = false;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			try
			{
				Autodesk.Revit.ApplicationServices.Application application = this.p_commandData.Application.Application;
				Document document = this.p_commandData.Application.ActiveUIDocument.Document;
				// 收集项目中所有的三维视图
				FilteredElementCollector view3dCollector1 = new FilteredElementCollector(document);
				ICollection<Element> view3dCollection1 = view3dCollector1.OfClass(typeof(View3D)).ToElements();
				FilteredElementCollector view3dCollector2 = new FilteredElementCollector(document);
				ICollection<Element> view3dCollection2 = view3dCollector2.OfClass(typeof(View3D)).ToElements();
				// 收集项目中所有的族类型
				FilteredElementCollector familySymbolCollector1 = new FilteredElementCollector(document);
				ICollection<Element> familySymbolCollection1 = familySymbolCollector1.OfClass(typeof(FamilySymbol)).ToElements();
				FilteredElementCollector familySymbolCollector2 = new FilteredElementCollector(document);
				ICollection<Element> familySymbolCollection2 = familySymbolCollector2.OfClass(typeof(FamilySymbol)).ToElements();
				// 收集项目中所有的族实例
				FilteredElementCollector familyInstanceCollector = new FilteredElementCollector(document);
				ICollection<Element> familyInstanceCollection = familyInstanceCollector.OfClass(typeof(FamilyInstance)).ToElements();
				// 声明对话框中需要的内容
				MessageBoxIcon icon = MessageBoxIcon.Exclamation;
				MessageBoxButtons buttons = MessageBoxButtons.OK;
				string caption = "模型导出";
				// ?
				this.m_AllViews.StandAloneVersion = false;
				// 判断哪个单选按钮被选中
				int radioButtonCheckNum = 6;
				// 未勾选导出其他视图时，判断当前视图类型，满足条件后赋值给view3D
				View3D view3D = null;
				bool isTemplateAndThreeDView = false;
				if (!this.checkBoxOtherView.Checked)
				{
					if (document.ActiveView.ViewType != ViewType.ThreeD)
					{
						MessageBox.Show("当前视图必须为三维视图！");
						base.Close();
					}
					if (document.ActiveView.ViewType == ViewType.ThreeD & document.ActiveView.IsTemplate)
					{
						MessageBox.Show("当前视图为样板视图，不可导出！");
						isTemplateAndThreeDView = true;
						base.Close();
					}
					if (document.ActiveView.ViewType == ViewType.ThreeD & !document.ActiveView.IsTemplate)
					{
						view3D = (document.ActiveView as View3D);
					}
				}
				// 隐藏视图中的标注图元
				Transaction transaction = new Transaction(document);
				transaction.Start("隐藏标注");
				// 未勾选导出其他视图时的隐藏方式
				if (!this.checkBoxOtherView.Checked)
				{
					foreach (object obj in document.Settings.Categories)
					{
						Category category = (Category)obj;
						if (category.get_AllowsVisibilityControl(view3D))
						{
							if (category.CategoryType != CategoryType.Model)
							{
								view3D.SetCategoryHidden(category.Id, true);
							}
						}
					}
				}
				// 勾选导出其他视图时的隐藏方式
				// 可删除部分--》
				if (this.checkBoxOtherView.Checked)
				{
					foreach (Element view3d in view3dCollection1)
					{
						View3D view3D2 = (View3D)view3d;
						foreach (object obj2 in this.listBoxViews.SelectedItems)
						{
							if (view3D2.Name == (string)obj2)
							{
								view3D = view3D2;
								if (view3D != null & view3D.ViewType == ViewType.ThreeD & view3D.IsTemplate)
								{
									isTemplateAndThreeDView = true;
								}
								if (!isTemplateAndThreeDView)
								{
									foreach (object obj3 in document.Settings.Categories)
									{
										Category category2 = (Category)obj3;
										if (category2.get_AllowsVisibilityControl(view3D))
										{
											if (category2.CategoryType != CategoryType.Model)
											{
												view3D.SetCategoryHidden(category2.Id, true);
											}
										}
									}
								}
							}
						}
					}
				}
				transaction.Commit();
				// 》》》
				int num2 = 5000000;
				int num3 = num2 * 2;






				// 未勾选导出其他视图时的导出方式
				if (!this.checkBoxOtherView.Checked)
				{
					if (!isTemplateAndThreeDView)
					{
						if (view3D != null)
						{
							// 导出数据
							CheckExportContext checkExportContext = new CheckExportContext(document);
							new CustomExporter(document, checkExportContext)
							{
								IncludeGeometricObjects = false,
								ShouldStopOnError = false
							}.Export(view3D as Autodesk.Revit.DB.View);
						}
					}
					if (application.VersionNumber.Contains("2019") | application.VersionNumber.Contains("2020"))
					{




						if (view3D != null & !isTemplateAndThreeDView)
						{
							string objFilePath = null;
							SaveFileDialog saveFileDialog = new SaveFileDialog();
							saveFileDialog.InitialDirectory = "C:\\";
							saveFileDialog.Filter = "obj files (*.obj)|*.obj|All files (*.*)|*.*";
							saveFileDialog.FilterIndex = 1;
							saveFileDialog.RestoreDirectory = true;
							saveFileDialog.FileName = null;
							if (saveFileDialog.ShowDialog() == DialogResult.OK)
							{
								try
								{
									objFilePath = saveFileDialog.FileName;
									mtlPath = Path.GetFileNameWithoutExtension( saveFileDialog.FileName) + ".mtl";
								}
								catch (Exception ex)
								{
									MessageBox.Show("Error: Could not read file. Original error: " + ex.Message);
								}
							}
							else
							{
								if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
								{
									isTemplateAndThreeDView = true;
									return;
								}
							}
							ExportToObjContext exportToVRContext = new ExportToObjContext(document, this.m_AllViews);
							CustomExporter customExporter = new CustomExporter(document, exportToVRContext);
							customExporter.IncludeGeometricObjects = false;
							customExporter.ShouldStopOnError = false;
							try
							{
								this.m_AllViews.ExportSubCategories = false;


								if ( radioButtonCheckNum == 6)
								{
									int num4 = 1000;
									FilteredElementCollector view3dCollector = new FilteredElementCollector(document, view3D.Id);
									ICollection<ElementId> view3dIdsCollection = view3dCollector.ToElementIds();
									ICollection<ElementId> collection7 = view3dCollector.ToElementIds();
									collection7.Clear();
									List<int> list = new List<int>();
									List<int> list2 = new List<int>();
									List<int> list3 = new List<int>();
									bool flag32 = false;
									foreach (ElementId elementId in view3dIdsCollection)
									{
										bool flag33 = false;
										Element element2 = document.GetElement(elementId);
										if (element2 != null)
										{
											if (element2.Category != null)
											{
												if (element2.Category.CategoryType == CategoryType.Model)
												{
													flag33 = true;
												}
												if (element2.Category.Id.IntegerValue == -2001340)
												{
													flag32 = true;
												}
												if (element2.Category.Id.IntegerValue == -2001352)
												{
													flag32 = true;
												}
											}
											if (element2.GetTypeId() != null)
											{
												int integerValue = element2.GetTypeId().IntegerValue;
												bool flag40 = flag33 & !flag32;
												if (flag40)
												{
													GeometryElement geometryElement = element2.get_Geometry(new Options
													{
														ComputeReferences = true
													});
													bool flag41 = geometryElement != null;
													if (flag41)
													{
														foreach (GeometryObject geometryObject in geometryElement)
														{
															if (geometryObject is Solid)
															{
																Solid solid = geometryObject as Solid;
																bool flag43 = null != solid;
																if (flag43)
																{
																	bool flag44 = solid.Faces.Size > 0;
																	if (flag44)
																	{
																		flag32 = true;
																		break;
																	}
																}
															}
															GeometryInstance geometryInstance = geometryObject as GeometryInstance;
															bool flag45 = null != geometryInstance;
															if (flag45)
															{
																foreach (GeometryObject geometryObject2 in geometryInstance.SymbolGeometry)
																{
																	Solid solid2 = geometryObject2 as Solid;
																	bool flag46 = null != solid2;
																	if (flag46)
																	{
																		bool flag47 = solid2.Faces.Size > 0;
																		if (flag47)
																		{
																			flag32 = true;
																			break;
																		}
																	}
																}
															}
														}
													}
												}
												if (!list.Contains(integerValue) && flag32)
												{
													list.Add(integerValue);
												}
											}
										}
										flag32 = false;
									}
									for (int i = 0; i < list.Count; i++)
									{
										int item = list[i];
										int num5 = 0;
										bool flag49 = num5 <= num4;
										if (flag49)
										{
											list3.Add(item);
										}
										bool flag50 = num5 > num4;
										if (flag50)
										{
											list2.Add(item);
										}
									}
									if (list3.Count > 0)
									{
										bool flag52 = false;
										foreach (ElementId elementId2 in view3dIdsCollection)
										{
											Element element3 = document.GetElement(elementId2);
											bool flag53 = element3 != null;
											if (flag53)
											{
												int integerValue2 = element3.GetTypeId().IntegerValue;
												bool flag54 = !list3.Contains(integerValue2);
												if (flag54)
												{
													bool flag55 = element3.Category != null;
													if (flag55)
													{
														bool flag56 = element3.Category.Id.IntegerValue == -2001340;
														if (flag56)
														{
															flag52 = true;
														}
													}
													bool flag57 = !flag32;
													if (flag57)
													{
														GeometryElement geometryElement2 = element3.get_Geometry(new Options
														{
															ComputeReferences = true
														});
														bool flag58 = geometryElement2 != null;
														if (flag58)
														{
															foreach (GeometryObject geometryObject3 in geometryElement2)
															{
																bool flag59 = geometryObject3 is Solid;
																if (flag59)
																{
																	Solid solid3 = geometryObject3 as Solid;
																	bool flag60 = null != solid3;
																	if (flag60)
																	{
																		bool flag61 = solid3.Faces.Size > 0;
																		if (flag61)
																		{
																			flag52 = true;
																			break;
																		}
																	}
																}
																GeometryInstance geometryInstance2 = geometryObject3 as GeometryInstance;
																bool flag62 = null != geometryInstance2;
																if (flag62)
																{
																	foreach (GeometryObject geometryObject4 in geometryInstance2.SymbolGeometry)
																	{
																		Solid solid4 = geometryObject4 as Solid;
																		bool flag63 = null != solid4;
																		if (flag63)
																		{
																			bool flag64 = solid4.Faces.Size > 0;
																			if (flag64)
																			{
																				flag52 = true;
																				break;
																			}
																		}
																	}
																}
															}
														}
													}
													bool flag65 = flag52;
													if (flag65)
													{
														bool flag66 = element3.CanBeHidden(view3D);
														if (flag66)
														{
															collection7.Add(elementId2);
														}
													}
												}
											}
											flag52 = false;
										}
										Transaction transaction4 = new Transaction(document);
										transaction4.Start("TempHideType");
										bool flag67 = collection7.Count > 0;
										if (flag67)
										{
											view3D.HideElements(collection7);
										}
										transaction4.Commit();
										customExporter.Export(view3D as Autodesk.Revit.DB.View);
										Transaction transaction5 = new Transaction(document);
										transaction5.Start("TempUnhideType");
										bool flag68 = collection7.Count > 0;
										if (flag68)
										{
											view3D.UnhideElements(collection7);
										}
										transaction5.Commit();
										collection7.Clear();
									}
									bool flag69 = list2.Count > 0;
									if (flag69)
									{
										foreach (int num6 in list2)
										{
											bool flag70 = false;
											bool flag71 = num6 != -1;
											if (flag71)
											{
												foreach (ElementId elementId3 in view3dIdsCollection)
												{
													Element element4 = document.GetElement(elementId3);
													bool flag72 = element4 != null;
													if (flag72)
													{
														int integerValue3 = element4.GetTypeId().IntegerValue;
														bool flag73 = num6 != integerValue3;
														if (flag73)
														{
															bool flag74 = element4.Category != null;
															if (flag74)
															{
																bool flag75 = element4.Category.Id.IntegerValue == -2001340;
																if (flag75)
																{
																	flag70 = true;
																}
															}
															bool flag76 = !flag70;
															if (flag76)
															{
																GeometryElement geometryElement3 = element4.get_Geometry(new Options
																{
																	ComputeReferences = true
																});
																bool flag77 = geometryElement3 != null;
																if (flag77)
																{
																	foreach (GeometryObject geometryObject5 in geometryElement3)
																	{
																		bool flag78 = geometryObject5 is Solid;
																		if (flag78)
																		{
																			Solid solid5 = geometryObject5 as Solid;
																			bool flag79 = null != solid5;
																			if (flag79)
																			{
																				bool flag80 = solid5.Faces.Size > 0;
																				if (flag80)
																				{
																					flag70 = true;
																					break;
																				}
																			}
																		}
																		GeometryInstance geometryInstance3 = geometryObject5 as GeometryInstance;
																		bool flag81 = null != geometryInstance3;
																		if (flag81)
																		{
																			foreach (GeometryObject geometryObject6 in geometryInstance3.SymbolGeometry)
																			{
																				Solid solid6 = geometryObject6 as Solid;
																				bool flag82 = null != solid6;
																				if (flag82)
																				{
																					bool flag83 = solid6.Faces.Size > 0;
																					if (flag83)
																					{
																						flag70 = true;
																						break;
																					}
																				}
																			}
																		}
																	}
																}
															}
															bool flag84 = flag70;
															if (flag84)
															{
																bool flag85 = element4.CanBeHidden(view3D);
																if (flag85)
																{
																	collection7.Add(elementId3);
																}
															}
														}
													}
													flag70 = false;
												}
												Transaction transaction6 = new Transaction(document);
												transaction6.Start("TempHideType");
												bool flag86 = collection7.Count > 0;
												if (flag86)
												{
													view3D.HideElements(collection7);
												}
												transaction6.Commit();
												customExporter.Export(view3D as Autodesk.Revit.DB.View);
												Transaction transaction7 = new Transaction(document);
												transaction7.Start("TempUnhideType");
												bool flag87 = collection7.Count > 0;
												if (flag87)
												{
													view3D.UnhideElements(collection7);
												}
												transaction7.Commit();
												collection7.Clear();
											}
										}
									}
								}
							}
							catch (ExternalApplicationException ex2)
							{
								Debug.Print("ExternalApplicationException " + ex2.Message);
							}
							if (!isTemplateAndThreeDView)
							{
								if (File.Exists(objFilePath))
								{
									File.Delete(objFilePath);
								}
								bool flag90 = Directory.Exists(Path.GetDirectoryName(objFilePath));
								if (flag90)
								{
									using (StreamWriter streamWriter = new StreamWriter(objFilePath, true))
									{

										
											streamWriter.WriteLine(string.Concat(new object[]
											{
												"mtllib ",
												Path.GetFileNameWithoutExtension(objFilePath),
												".mtl\n",
												this.m_AllViews.XYZsbuilder,
												"\n",
												this.m_AllViews.UVsbuilder,
												"\n",
												this.m_AllViews.NORMALsbuilder,
												"\n",
												this.m_AllViews.FCTbyMATsbuilder
											}));
										
										//mtlPath = objFilePath;
										//objFilePath = Path.ChangeExtension(mtlPath, "mtl");
										mtlPath = Path.ChangeExtension(objFilePath, "mtl");
										streamWriter.Close();
									}
									using (StreamWriter streamWriter2 = new StreamWriter(mtlPath))
									{

										if (radioButtonCheckNum == 6)
										{
											bool flag100 = this.m_AllViews.MATERIALsbuilder != null;
											if (flag100)
											{
												streamWriter2.WriteLine(this.m_AllViews.MATERIALsbuilder);
											}
										}
										streamWriter2.Close();
									}
									ExportViewsToVRForm._export_folder_name = Path.GetDirectoryName(objFilePath);
								}
								bool flag101 = Directory.Exists(Path.GetDirectoryName(objFilePath));
								if (flag101)
								{
									bool textureExist = exportToVRContext.textureExist;
									if (textureExist)
									{
										bool flag102 = exportToVRContext.key_Materials != null;
										if (flag102)
										{
											bool flag103 = exportToVRContext.key_Materials.Count > 0;
											if (flag103)
											{
												foreach (object obj8 in exportToVRContext.key_Materials)
												{
													int num7 = (int)obj8;
													string text2 = Convert.ToString(exportToVRContext.h_Materials[num7]);
													bool flag104 = File.Exists(text2);
													if (flag104)
													{
														bool flag105 = text2 != null & text2 != "";
														if (flag105)
														{
															string fileName = Path.GetFileName(text2);
															string text3 = fileName.Replace(" ", "_");
															string str = text3;
															string text4 = ExportViewsToVRForm._export_folder_name + "\\" + str;
															bool flag106 = !File.Exists(text4);
															if (flag106)
															{
																File.Copy(text2, text4);
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}










				base.Close();
			}
			catch (Exception ex5)
			{
				MessageBox.Show(ex5.Message);
			}
		}

		private void checkBoxOtherView_CheckedChanged(object sender, EventArgs e)
		{
			bool @checked = this.checkBoxOtherView.Checked;
			if (@checked)
			{
				this.listBoxViews.Enabled = true;
			}
			bool flag = !this.checkBoxOtherView.Checked;
			if (flag)
			{
				this.listBoxViews.Enabled = false;
			}
		}
		private void listBoxViews_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.listBoxViews.SelectionMode = SelectionMode.MultiExtended;
			this.listBoxViews.DataSource = this.m_AllViews.ViewListName;
		}
		private ExternalCommandData p_commandData;
		private AllViews m_AllViews;
		private const double _eps = 1E-09;
		private const double _feet_to_mm = 304.79999999999995;
		//private string units = null;
		private static string _export_folder_name = null;
		public int TotalElementInView;
		//private string _path;
        private string  mtlPath;
        public string  MtlPath
        {
            get { return mtlPath; }
            set { mtlPath = value; }
        }

        private void checkBoxTrialVersion_CheckedChanged(object sender, EventArgs e)
		{

		}
	}

}
