using System;
using System.Collections;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ExportToObj
{
	internal class CheckExportContext : IExportContext
	{
		private static double DegreeToRadian(double angle)
		{
			return Math.PI * angle / 180.0;
		}
		private static double RadianToDegree(double angle)
		{
			return angle * 57.295779513082323;
		}
		private Document m_document = null;
		private bool m_cancelled = false;
		private bool isLink = false;
		private Stack<Transform> m_TransformationStack = new Stack<Transform>();
		public int TotalNBofPoints;
		public int TotalNBofFacets;
		public int TotalNBofNodes;
		public int OrderPoints = 1;
		public int OrderFacets = 1;
		public int OrderNormals = 1;
		public int OrderUVs = 1;
		private int MaterialFaceID;
		private int memeElement = 0;
		public List<string> ListElementID01 = new List<string>();
		public List<string> ListElementID02 = new List<string>();
		public List<string> ListElementID03 = new List<string>();
		public List<string> ListElementID04 = new List<string>();
		public List<string> ListElementID05 = new List<string>();
		public List<string> ListElementID06 = new List<string>();
		public List<string> ListElementID07 = new List<string>();
		public List<string> ListElementID08 = new List<string>();
		public List<string> ListElementID09 = new List<string>();
		public List<string> ListElementID10 = new List<string>();
		public List<string> ListLINKID01 = new List<string>();
		public List<string> ListElementID_ALL = new List<string>();
		private List<int> ListMaterialFaceID = new List<int>();
		private List<int> ListOneElementID = new List<int>();
		public List<int> ListMaterialID = new List<int>();
		public Hashtable h_ElementIDListMatID = new Hashtable();
		public ICollection key_ElementIDListMatID = null;
		private Stack<ElementId> elementStack = new Stack<ElementId>();
		private Document ZeLinkDoc = null;
		private ElementId currentMaterialId = ElementId.InvalidElementId;
		// 定义构造函数
		public CheckExportContext(Document document)
		{
			this.m_document = document;
			this.m_TransformationStack.Push(Transform.Identity);
		}
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
			Element currentElement = this.CurrentElement;
			string currentElementName;
			if (currentElement != null)
			{
				currentElementName = currentElement.Name;
			}
			else
			{
				currentElementName = "";
			}
			return currentElementName;
		}
		public bool Start()
		{
			return true;
		}
		public void Finish()
		{
		}
		public bool IsCanceled()
		{
			return this.m_cancelled;
		}
		public void OnPolymesh(PolymeshTopology node)
		{
			Transform transform = this.m_TransformationStack.Peek();
			int numberOfFacets = node.NumberOfFacets;
			int numberOfPoints = node.NumberOfPoints;
			int numberOfUVs = node.NumberOfUVs;
			int numberOfNormals = node.NumberOfNormals;
			this.TotalNBofPoints += numberOfPoints;
			this.TotalNBofFacets += numberOfFacets;
		}
		private void ExportMeshPoints(IList<XYZ> points, Transform trf, IList<XYZ> normals)
		{
		}
		private void ExportMeshPoints(IList<XYZ> points, Transform trf, XYZ normal)
		{
		}
		private void ExportMeshPoints(IList<XYZ> points, Transform trf)
		{
		}
		private void ExportMeshFacets(IList<PolymeshFacet> facets, IList<XYZ> normals)
		{
			bool flag = normals == null;
			if (flag)
			{
			}
		}
		private void ExportMeshUVs(IList<UV> UVs)
		{
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
				bool flag = document.Title.Equals(name);
				if (flag)
				{
					this.ZeLinkDoc = document;
				}
			}
			bool flag2 = name != null;
			if (flag2)
			{
				this.isLink = true;
			}
			bool flag3 = symbolId != null;
			if (flag3)
			{
				bool flag4 = !this.ListLINKID01.Contains(symbolId.IntegerValue.ToString());
				if (flag4)
				{
					this.ListLINKID01.Add(symbolId.IntegerValue.ToString());
				}
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
			this.currentMaterialId = node.MaterialId;
			if (this.currentMaterialId != ElementId.InvalidElementId & !this.currentMaterialId.IntegerValue.ToString().Contains("-"))
			{
				Material material = this.m_document.GetElement(this.currentMaterialId) as Material;
				this.MaterialFaceID = this.currentMaterialId.IntegerValue;
				if (!this.ListMaterialID.Contains(this.currentMaterialId.IntegerValue))
				{
					this.ListMaterialID.Add(this.currentMaterialId.IntegerValue);
					this.MaterialFaceID = this.currentMaterialId.IntegerValue;
				}
			}
		}
	}
}
