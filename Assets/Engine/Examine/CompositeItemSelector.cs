using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.UI;
using SS3D.Engine.Input;


namespace SS3D.Engine.Examine
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
		public RenderTexture rt;
		
		// This is how the Examinator gets access to the actual Examinable
		private GameObject currentExaminable;
		
		// Components of our unique colours
		private float rValue;
		private float gValue;
		private float bValue;
		private float decrement;
		private float tolerance;
		
		// Screen resolution
		private int recordedScreenWidth;
		private int recordedScreenHeight;
				
		// Indicate whether mouse is currently over the UI
		private bool mouseOverUI;

		// Confirms whether we need to render the image during OnPostRender.
		private bool setPostRender;
				
		public void Start()
		{
			cam = CameraManager.singleton.examineCamera;
			tex = new Texture2D(1, 1);
			mouseOverUI = false;
			setPostRender = false;
		}

		public void OnPostRender()
		{
			
			// Don't bother with all of this work if the mouse is over the user interface.
			// If it is over the interface, we should already have a reference to the object
			// stored in currentExaminable.
			if (mouseOverUI || !setPostRender)
			{
				return;
			}
			
			// If the window size has changed, amend the RenderTexture correspondingly.
			ResizeTexturesIfRequired();

			// Render all of our meshes to the invisible RenderTexture
			SkinnedMeshRenderer smr;
			Mesh bakedMesh = new Mesh();
			foreach (MeshColourAffiliation mesh in meshes)
			{
				singleColourMaterial.SetVector("colour", mesh.GetColour());
				singleColourMaterial.SetPass(0);

				smr = mesh.GetSkinnedMeshRenderer();
				if (smr == null)
                {
					UnityEngine.Graphics.DrawMeshNow(mesh.GetMesh(), mesh.GetTransform().position, mesh.GetTransform().rotation);
				}
				else
                {
					smr.BakeMesh(bakedMesh);
					UnityEngine.Graphics.DrawMeshNow(bakedMesh, mesh.GetTransform().position, smr.transform.rotation);
				}
			}

			// Test the colour of the pixel where our cursor is
			Vector2 mousePosition = InputHelper.inputs.pointer.position.ReadValue<Vector2>();
			int currentX = (int) mousePosition.x;
			int currentY = (int) mousePosition.y;

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
					//if (point == examinable.GetColour())
					if (matchesColour(point, examinable.GetColour()))	
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
		
		
		/// This function checks to see if the colour is 'close enough' to one of our recorded
		/// colours. We can't just use Equals, because rounding errors can make it unreliable.
		private bool matchesColour(Color colour1, Color colour2)
		{
			if (Math.Abs(colour1.r - colour2.r) > tolerance) return false;
			if (Math.Abs(colour1.g - colour2.g) > tolerance) return false;
			if (Math.Abs(colour1.b - colour2.b) > tolerance) return false;
			return true;
			
		}
		
		
		/// This is how the Examinator actually returns the Object.
		public GameObject GetCurrentExaminable(){
			setPostRender = false;
			return currentExaminable;
		}
		
		
		public void CalculateSelectedGameObject()
		{

			// Tells the CompositeItemSelector that we want to render the meshes in OnPostRender, so
			// that we can use that texture to determine the examinable.
			setPostRender = true;

			// Identify if mouse is over the user interface, or over objects in the game world.
			if (EventSystem.current.IsPointerOverGameObject())
			{
				
				// The cursor is over the UI. If the UI is examinable, this will override objects in the game world.
				mouseOverUI = true;
				currentExaminable = null;

				// Get a list of all the UI elements under the cursor
				var pointerEventData = new PointerEventData(EventSystem.current) {position = InputHelper.inputs.pointer.position.ReadValue<Vector2>()};
				List<RaycastResult> UIhits = new List<RaycastResult>();
				EventSystem.current.RaycastAll(pointerEventData, UIhits);			
				
				// Get the UI to give us the GameObject that a slot is displaying.
				foreach (var hit in UIhits)
				{
					ISlotProvider slot = hit.gameObject.GetComponent<ISlotProvider>();
					if (slot != null)
					{
						currentExaminable = slot.GetCurrentGameObjectInSlot();
					}
				}
			}
			else
			{
				mouseOverUI = false;
			}

			// Raycast to cursor position. Need to get all possible hits, because the initial hit may have gaps through which we can see other Examinables
			Vector2 mousePosition = InputHelper.inputs.pointer.position.ReadValue<Vector2>();
            Ray ray = cam.ScreenPointToRay(new Vector2(mousePosition.x, mousePosition.y));
			RaycastHit[] hits = Physics.RaycastAll(ray, 200f);

			// Convert the RaycastHits to GameObjects
			GameObject[] gameObjects = new GameObject[hits.Length];
			for (int i = 0; i < hits.Length; i++)
			{
				gameObjects[i] = hits[i].transform.gameObject;
			}

			// Store the meshes of these GameObjects in our data structure, so that they can be rendered off-screen later.
			AddMeshesToLists(gameObjects);
		}
			
		/// Amends the render texture to be the same size as the screen, so the coordinates are mapped correctly.
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
						Debug.LogError("CompositeItemSelector: Too many colours cycled through.");
						DisableCamera();
					}
				}
				
			}
		}
		
		/// This method returns the root GameObject 
		private GameObject GetAncestor (GameObject descendant)
		{
			// This statement is very bad - need to confirm standard GameObject hierarchy...
			while (descendant.transform.parent != null && descendant.transform.parent.name != "TileMap" && descendant.transform.parent.name != "Objects" && descendant.transform.parent.name != "InventoryUI Rework Beep")
			{
				descendant = descendant.transform.parent.gameObject;
			}
			return descendant;
		}
		
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
				decrement = 0.05f;
				tolerance = decrement / 4.0f;
				
				
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
			
			//rt = null;
			currentExaminable = null;
		}
		
		/// This method adds all of the objects targeted by the RaycastAll to the
		/// mesh and colour lists.
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
			SkinnedMeshRenderer smr = child.gameObject.GetComponent<SkinnedMeshRenderer>();
			
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
			if (mf != null && mf.sharedMesh != null && child.gameObject.GetComponent<Renderer>().enabled)
			{
				meshes.Add(new MeshColourAffiliation(mf.sharedMesh, colours.Peek(), child));
			}
			if (smr != null && smr.sharedMesh != null && child.gameObject.GetComponent<Renderer>().enabled)
			{
				meshes.Add(new MeshColourAffiliation(smr.sharedMesh, colours.Peek(), child, smr));
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
			private SkinnedMeshRenderer smr;
			
			public MeshColourAffiliation(Mesh mesh, Color colour, Transform transform, SkinnedMeshRenderer smr = null)
			{
				this.mesh = mesh;
				this.colour = colour;
				this.transform = transform;
				this.smr = smr;
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
			
			public SkinnedMeshRenderer GetSkinnedMeshRenderer()
            {
				return smr;
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