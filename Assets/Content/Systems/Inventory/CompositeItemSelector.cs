using System;
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
	    public Camera cam;
		public Material singleColourMaterial;
		
		// Data structures to store our mesh and colour affiliations
		private List<MeshColourAffiliation> meshes;
		private List<ExaminableColourAffiliation> examinables;
		private List<GameObject> tiles;
		private Stack<Color> colours;
		
		// Single pixel texture simply used to read the pixel colour under the mouse
		private Texture2D tex;
		
		// Single pixel rectangle used to transfer the pixel from the RenderTexture to the Texture2D.
		private Rect imageArea;
		
		// The hidden texture used to render our targeted meshes. Needs to have identical resolution to screen.
		private RenderTexture rt;
		
		// This is how the Examinator gets access to the actual Examinable
		private GameObject currentExaminable;
		
		// Components of our unique colours
		private float rValue;
		private float gValue;
		private float bValue;
		private float decrement;
		
		// Screen resolution
		private int recordedScreenWidth;
		private int recordedScreenHeight;
				
		public void Start()
		{
			cam = CameraManager.singleton.examineCamera;
			tex = new Texture2D(1, 1);
		}

		public void OnPostRender()
		{
			// If the window size has changed, amend the RenderTexture correspondingly.
			ResizeTexturesIfRequired();
			
			// Render all of our meshes to the invisible RenderTexture
			foreach (MeshColourAffiliation mesh in meshes)
			{
				singleColourMaterial.mainTexture = mesh.GetTexture();
				singleColourMaterial.SetVector("colour", mesh.GetColour());
				singleColourMaterial.SetPass(0);
				UnityEngine.Graphics.DrawMeshNow(mesh.GetMesh(), mesh.GetTransform().position, mesh.GetTransform().rotation);
			}
				
			// Test the colour of the pixel where our cursor is
			int currentX = (int) Input.mousePosition.x;
			int currentY = (int) Input.mousePosition.y;

			// If it's within the screen boundaries...
			if (currentX >= 0 && currentY >= 0 && currentX < Screen.width && currentY < Screen.height)
			{
				// Copy the pixel to our Texture2D, so we can read its colour.
				imageArea = new Rect(currentX, Screen.height - currentY - 1, 1, 1); // Note well: y increases downwards for Struct, but upwards for Screen coordinates. Thanks Unity.
				tex.ReadPixels(imageArea, 0, 0, false);
				Color point = tex.GetPixel(0, 0);
				bool hit = false;
				
				// Check the unique colour of each Examinable, to see if it corresponds to the colour at the cursor. 
				foreach (ExaminableColourAffiliation examinable in examinables)
				{
					if (point == examinable.GetColour())
					{
						currentExaminable = examinable.GetExaminable();
						hit = true;
					}
				}					
			}
			else
			{
				currentExaminable = null;
			}										
		}
		
		public GameObject GetCurrentExaminable(){
			return currentExaminable;
		}
		
		
		/// Returns if the GameObject is a composite Examinable object. Currently
		/// only tiles return true. This function will need to be amended if there
		/// is a need for non-Tiles to become composite Examinable objects.
		public bool IsCompositeExaminable(GameObject target)
		{
			target = GetAncestor(target);
			if (target.transform.parent == null)
			{
				return false;
			}
			if (target.transform.parent.gameObject.name == "TileMap")
			{
				return true;
			}
			return false;
		}
		
		private void ResizeTexturesIfRequired(){
			
			// If textures don't currently exist, create them at the correct size.
			if (recordedScreenWidth != Screen.width || recordedScreenHeight != Screen.height)	
			{
				cam.targetTexture = null;
				rt.Release();
				rt = new RenderTexture(Screen.width, Screen.height, 16);
				cam.targetTexture = rt;

				recordedScreenWidth = Screen.width;
				recordedScreenHeight = Screen.height;
			}
		}
		
		
		/// This method simply uses the next available unique colour by decrementing the RGB values.
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
						DisableCamera();
					}
				}
				
			}
		}
		
		/// This method returns the root GameObject (or the Tile, if the TileMap is the root object)
		private GameObject GetAncestor (GameObject descendant)
		{
			while (descendant.transform.parent != null && descendant.transform.parent.name != "TileMap")
			{
				descendant = descendant.transform.parent.gameObject;
			}
			return descendant;
		}
		
		/*
		private void GetListOfMeshes(GameObject ancestor)
		{			
			colours.Push(new Color(rValue, gValue, bValue, 1.0f));
			AddChildToLists(ancestor.transform);
			
			

		}*/
		
		/// This method enables the camera and establishes our data structures.
		private void EnableCamera(){
			if (cam == null)
				cam = CameraManager.singleton.examineCamera;
			if (!cam.enabled)
			{
				
				// Enable the Camera component
				cam.enabled = true;
				
				// Establish our collections
				meshes = new List<MeshColourAffiliation>();
				examinables = new List<ExaminableColourAffiliation>();
				tiles = new List<GameObject>();
				colours = new Stack<Color>();
				
				// Reset the colours
				rValue = 1.0f;
				gValue = 1.0f;
				bValue = 1.0f;
				decrement = 0.2f;
				
				// Record the screen resolution
				recordedScreenWidth = Screen.width;
				recordedScreenHeight = Screen.height;
				
				// Establish the renderTexture
				rt = new RenderTexture(Screen.width, Screen.height, 16);
				cam.targetTexture = rt;
			}
		}
		
		/// This method disables the camera and removes our expensive data structures
		/// so that they can be reclaimed by garbage collection.
		public void DisableCamera()
		{
			// Turn off the camera
			cam.enabled = false;
			
			// Empty our collections
			meshes = null;
			examinables = null;
			tiles = null;
			colours = null;
			
			rt = null;
			currentExaminable = null;
		}
		
		/// This method adds all of the objects targeted by the RaycastAll (within the
		/// Examinator script) to the mesh and colour lists.
		public void AddMeshesToLists(GameObject[] allHitObjects)
		{
			GameObject ancestor;
			bool alreadyInList;
			EnableCamera();
			foreach (GameObject obj in allHitObjects)
			{
				// Check to see if the GameObject is already recorded
				alreadyInList = false;
				ancestor = GetAncestor(obj);
				foreach (GameObject tile in tiles)
				{
					if (ancestor == tile)
					{
						alreadyInList = true;
					}
				}
				// If not already recorded, record it!
				if (!alreadyInList)
				{
					colours.Push(new Color(rValue, gValue, bValue, 1.0f));
					AddChildToLists(ancestor.transform);
					tiles.Add(ancestor);
				}
				
			}
		}
	
		/// This method calls recursively to find all descendants of a GameObject. If
		/// they have meshes to render, and/or are Examinable, they are recorded in our
		/// meshes and examinables lists.
		private void AddChildToLists(Transform child)
		{		
			// Don't record the mesh if the child is disabled!
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
				examinables.Add(new ExaminableColourAffiliation(child.gameObject, colours.Peek(), child.gameObject.name));
			}
			
			// If mesh exists, record the colour affiliation of it 
			if (mf != null && child.gameObject.GetComponent<Renderer>().enabled)
			{
				meshes.Add(new MeshColourAffiliation(mf.mesh, colours.Peek(), child, child.gameObject.GetComponent<Renderer>().material.mainTexture));
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
	
		/// This class is used to link particular meshes to the colour used to represent them
		/// with the CompositeItemSelector render texture. It is possible for multiple meshes
		/// to be affiliated with the same colour (for example, the meshes are all subordinate
		/// to an Examinable GameObject, without any of them being individually Examinable)
		private class MeshColourAffiliation
		{
			private Mesh mesh;
			private Color colour;
			private Transform transform;
			private Texture texture;
			
			public MeshColourAffiliation(Mesh mesh, Color colour, Transform transform, Texture texture)
			{
				this.mesh = mesh;
				this.colour = colour;
				this.transform = transform;
				this.texture = texture;
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
			
			public Texture GetTexture(){
				return texture;
			}
			
		}
		
		/// This class is used to link Examinable GameObjects to the colour used to represent
		/// them. There should be a 1:1 correlation between colours and Examinable GameObjects.
		/// Each Examinable will have one or more associated meshes, all of which share its colour.
		private class ExaminableColourAffiliation
		{
			private GameObject examinable;
			private Color colour;
			private string name;
			
			public ExaminableColourAffiliation(GameObject examinable, Color colour, string name)
			{
				this.examinable = examinable;
				this.colour = colour;
				this.name = name;
			}
			
			public GameObject GetExaminable()
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