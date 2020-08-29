using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SS3D.Engine.Examine;


namespace SS3D.Content.Systems.Examine
{
    /// <summary>
    /// For composite GameObjects composed of multiple Examinable GameObjects, this script allows
	/// us to return specifically which subordinate GameObject the cursor is over.
    /// </summary>
	public class CompositeItemSelector : MonoBehaviour
	{
		
		public GameObject target;
		public RawImage ri;
		
		private List<MeshColourAffiliation> meshes;
		private List<ExaminableColourAffiliation> examinables;
		private Stack<Color> colours;
		
		
		private Mesh mesh;
		private Material singleColourMaterial;
		private Shader singleColourShader;
		public Camera cam;
		public Text output;
		private Texture2D tex;
		private Rect imageArea;
		private RenderTexture rt;
		private Transform ancestorSearch;
		
		private float rValue;
		private float gValue;
		private float bValue;
		private float decrement;
		
		private int recordedScreenWidth;
		private int recordedScreenHeight;
		
		public void Start(){
			
			// Initialise our collections
			meshes = new List<MeshColourAffiliation>();
			examinables = new List<ExaminableColourAffiliation>();
			colours = new Stack<Color>();

			singleColourShader = Shader.Find("Unlit/singleColourShader");
			singleColourMaterial = new Material(singleColourShader);
			GeneratePickingFramework();
			//ri.texture = rt;
			
			target = GetAncestor(target);
			GetListOfMeshes(target);

		}
		
		public void OnPostRender()
		{
			if (AllTexturesAreValid())
			{		
				// Render all of our meshes to the invisible RenderTexture
				foreach (MeshColourAffiliation mesh in meshes)
				{
					singleColourMaterial.SetVector("colour", mesh.GetColour());
					singleColourMaterial.SetPass(0);
					UnityEngine.Graphics.DrawMeshNow(mesh.GetMesh(), mesh.GetTransform().position, mesh.GetTransform().rotation);
				}
				
				// Actually test the colour
				int currentX = (int) Input.mousePosition.x;
				int currentY = (int) Input.mousePosition.y;
					//output.text = Input.mousePosition.x + ": " + currentX;
				
				if (currentX >= 0 && currentY >= 0 && currentX < Screen.width && currentY < Screen.height)
				{
					imageArea = new Rect(currentX, currentY, 1, 1);
					tex.ReadPixels(imageArea, 0, 0, false);
					Color point = tex.GetPixel(currentX,currentY);
					bool hit = false;
					foreach (ExaminableColourAffiliation examinable in examinables)
					{
						if (point == examinable.GetColour())
						{
							output.text = examinable.GetName();
							hit = true;
						}
						if (!hit) output.text = "";
					}
					
				}
				else
				{
					output.text = "";
				}				
			}
		
		}
		
		private void GeneratePickingFramework(){
			
			rValue = 1.0f;
			gValue = 1.0f;
			bValue = 1.0f;
			decrement = 0.6f;
			
			imageArea = new Rect(0, 0, Screen.width, Screen.height);
			rt = new RenderTexture(Screen.width, Screen.height, 16);
			tex = new Texture2D(1, 1);
			recordedScreenWidth = Screen.width;
			recordedScreenHeight = Screen.height;
			cam.targetTexture = rt;
		}
		
		private bool AllTexturesAreValid(){
			
			// If textures don't currently exist, create them at the correct size.
			//if (readyToCreateAssets)
				
			return true; //---------------------------------------------------------------------------------------------------
			if (recordedScreenWidth != Screen.width || recordedScreenHeight != Screen.height)	
			{
				imageArea = new Rect(0, 0, Screen.width, Screen.height);
				cam.targetTexture = null;
				rt.Release();
				rt = new RenderTexture(Screen.width, Screen.height, 16);
				cam.targetTexture = rt;

				tex.Resize(Screen.width, Screen.height);
				recordedScreenWidth = Screen.width;
				recordedScreenHeight = Screen.height;
				return false;
			}
			return true;
		}
		
		private void ChangeToNextColour(){
			if (rValue < 0.0f)
			{
				return;
			}
			bValue -= decrement;
			if (bValue < 0.0f)
			{
				bValue = 1.0f;
				gValue -= decrement;
				if (gValue < 0.0f)
				{
					gValue = 1.0f;
					rValue -= decrement;
					if (rValue < 0.0f){
						Debug.Log("CompositeItemSelector: Too many colours cycled through.");
					}
				}
				
			}
		}
		
		private GameObject GetAncestor (GameObject descendant)
		{
			while (descendant.transform.parent != null && descendant.transform.parent.name != "TileMap")
			{
				descendant = descendant.transform.parent.gameObject;
			}
			return descendant;
		}
		
		private void GetListOfMeshes(GameObject ancestor)
		{			
			colours.Push(new Color(rValue, gValue, bValue, 1.0f));
			AddChildToLists(ancestor.transform);
			string display = "Meshes within the meshes list:\n";
			foreach (MeshColourAffiliation mesh in meshes)
			{
				display += "    " + mesh.GetTransform().gameObject.name + "\n";
			}
			Debug.Log(display);
		}
	
		// This method calls recursively to find all descendants of a GameObject. If
		// they have meshes to render, and/or are Examinable, they are recorded in our
		// meshes and examinables lists.
		private void AddChildToLists(Transform child)
		{
			
			if (child.gameObject.activeSelf == false)
			{
				return;
			}
			
			// Determine whether the GameObject has a mesh or is Examinable
			MeshFilter mf = child.gameObject.GetComponent<MeshFilter>();
			IExaminable examinable = child.gameObject.GetComponent<IExaminable>();
			
			// If examinable, create a unique colour to affiliate the examinable with. All non-
			// examinable children will also be affiliated with this colour.
			if (examinable != null)
			{
				ChangeToNextColour();
				colours.Push(new Color(rValue, gValue, bValue, 1.0f));
				examinables.Add(new ExaminableColourAffiliation(examinable, colours.Peek(), child.gameObject.name));
			}
			
			// If mesh exists, record the colour affiliation of it 
			if (mf != null && child.gameObject.GetComponent<Renderer>().enabled)
			{
				meshes.Add(new MeshColourAffiliation(mf.mesh, colours.Peek(), child, null));
			}
			
			// Recursively call this method on each child
			for (int i = 0; i < child.childCount; i++)
			{
				AddChildToLists(child.GetChild(i));
			}
			
			// This object and all children have been affiliated with the colour. Remove it.
			if (examinable != null)
			{
				colours.Pop();
			}
		}
	
		// This class is used to link particular meshes to the colour used to represent them
		// with the CompositeItemSelector render texture. It is possible for multiple meshes
		// to be affiliated with the same colour (for example, the meshes are all subordinate
		// to an Examinable GameObject, without any of them being individually Examinable)
		private class MeshColourAffiliation
		{
			private Mesh mesh;
			private Color colour;
			private Transform transform;
			private MeshRenderer renderer;
			
			public MeshColourAffiliation(Mesh mesh, Color colour, Transform transform, MeshRenderer renderer)
			{
				this.mesh = mesh;
				this.colour = colour;
				this.transform = transform;
				this.renderer = renderer;
			}
			
			public Mesh GetMesh()
			{
				return mesh;
			}
			
			public Color GetColour()
			{
				return colour;
			}
			
			public Transform GetTransform()
			{
				return transform;
			}			
			
			public MeshRenderer GetRenderer(){
				return renderer;
			}
			
		}
		
		// This class is used to link Examinable GameObjects to the colour used to represent
		// them. There should be a 1:1 correlation between colours and Examinable GameObjects.
		// Each Examinable will have one or more associated meshes, all of which share its colour.
		private class ExaminableColourAffiliation
		{
			private IExaminable examinable;
			private Color colour;
			private string name;
			
			public ExaminableColourAffiliation(IExaminable examinable, Color colour, string name)
			{
				this.examinable = examinable;
				this.colour = colour;
				this.name = name;
			}
			
			public IExaminable GetExaminable()
			{
				return examinable;
			}
			
			public Color GetColour()
			{
				return colour;
			}
			
			public string GetName()
			{
				return name;
			}
			
		}
	}
}