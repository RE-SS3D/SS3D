using System.Text;
using System.Collections;
using Mirror;
using SS3D.Engine.Examine;
using SS3D.Engine.FOV;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Content.Systems.Examine
{
    /// <summary>
    /// Displays the examine ui
    /// </summary>
    public class Examinator : NetworkBehaviour
    {
        public GameObject UiPrefab;
        public LayerMask ObstacleMask;
        public float ViewRange = 25f;

        private Camera camera;
        
        private GameObject uiInstance;
        //private ExamineUI examineUi;
		private IExamineUI examineUi;
		private IEnumerator coroutine;
		

        private Vector2 lastMousePosition;
        private Vector3 lastCameraPosition;
        private Quaternion lastCameraRotation;
		public CompositeItemSelector selector;
        private GameObject currentTarget;
		private bool currentTargetIsComposite;

        private void Start()
        {
            // Mirror is kinda whack
            if (!hasAuthority)
            {
                Destroy(this);
            }

            camera = CameraManager.singleton.examineCamera;
            selector = camera.GetComponent<CompositeItemSelector>();
	        
	        Assert.IsNotNull(UiPrefab);
            uiInstance = Instantiate(UiPrefab);
            examineUi = uiInstance.GetComponent<IExamineUI>();   /////-------------
            Assert.IsNotNull(examineUi);
            uiInstance.SetActive(false);
            
        }

        private void Update()
        {
            if (!isClient)
            {
                return;
            }

            if (Input.GetButton("Examine"))
            {
                Vector3 mousePosition = Input.mousePosition;
                Vector2 position = new Vector2(mousePosition.x, mousePosition.y);
                Vector3 cameraPos = camera.transform.position;
                Quaternion rotation = camera.transform.rotation;

                if (Vector2.Distance(position, lastMousePosition) > 1 ||
                    Vector3.Distance(cameraPos, lastCameraPosition) > 0.05 ||
                    Quaternion.Angle(rotation, lastCameraRotation) > 0.1)
                {
                    lastMousePosition = position;
                    lastCameraPosition = cameraPos;
                    lastCameraRotation = rotation;
                    CalculateExamine();
                }

                examineUi.SetPosition(position);
            }
            else if (!float.IsNegativeInfinity(lastMousePosition.x))
            {
                lastMousePosition = Vector2.negativeInfinity;
                ClearExamine();
                if (selector == null)
                {
	                selector = camera.GetComponent<CompositeItemSelector>();
                }
                selector.DisableCamera();
            }
        }





        private void CalculateExamine()
        {

            if (camera == null)
            {
                return;
            }
			selector.CalculateSelectedGameObject();
			coroutine = UpdateUserInterface();
			StartCoroutine(coroutine);
		}

		/// This function retrieves the current object from the selector. Because
		/// this object is only available once the rendering has been completed, it
		/// must be called inside a coroutine.
		private IEnumerator UpdateUserInterface()
		{
			// Wait until the off-screen rendering occurs in OnPostRender().
			yield return new WaitForEndOfFrame();
			
			// Retrieve the object the mouse is over.
			GameObject hitObject = selector.GetCurrentExaminable();
			
			
			if (hitObject != null)
			{
				IExaminable[] examinables = hitObject.GetComponents<IExaminable>();
				UpdateExamine(examinables);
			}
			else
			{
				ClearExamine();
			}			
		}		
		
			
			/*
            // Raycast to cursor position
            Ray ray = camera.ScreenPointToRay(lastMousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                // Get examinables
                GameObject hitObject = hit.transform.gameObject;
                IExaminable[] examinables = hitObject.GetComponents<IExaminable>();
                if (examinables.Length > 0)
                {
                    // Do nothing if ray hit current object (and that object is not composed of multiple Examinables)
                    if (currentTarget == hitObject && !currentTargetIsComposite)
                    {
                        return;
                    }
					
					// Check view distance
					if (Vector2.Distance(new Vector2(hit.point.x, hit.point.z),
							new Vector2(transform.position.x, transform.position.z)) > ViewRange)
					{
						ClearExamine();
						return;
					}

					// Check obstacles
					if (Physics.Linecast(transform.position, hit.point, ObstacleMask))
					{
						ClearExamine();
						return;
					}

                    currentTarget = hitObject;
					currentTargetIsComposite = selector.IsCompositeExaminable(hitObject);

					if (currentTargetIsComposite)
					{
						// Need to get ALL possible hits, because the initial hit may have gaps through which we can see other Examinables
						RaycastHit[] hits = Physics.RaycastAll(ray, 200f);
						// Convert the RaycastHits to GameObjects
						GameObject[] gameObjects = new GameObject[hits.Length];
						for (int i = 0; i < hits.Length; i++)
						{
							gameObjects[i] = hits[i].transform.gameObject;
						}
						selector.AddMeshesToLists(gameObjects);
						hitObject = selector.GetCurrentExaminable();
						
						// HitObject will always be null on the first frame - the render hasn't occurred yet
						if (hitObject == null)
						{
							return;
						}
						else
						{
							examinables = hitObject.GetComponents<IExaminable>();
						}
					}
					*/
					
					//UpdateExamine(examinables);

					/*
                    // Check if object is networked synced
                    NetworkIdentity identity = hitObject.GetComponent<NetworkIdentity>();
                    //if (identity == null)   //**********************************NETWORKING TEMPORARILY REMOVED
					if (true == true)
                    {
                        // Examine non-networked items
                        UpdateExamine(examinables);
                    }
                    else
                    {
						if (identity.netId == 0){
							
							// NetID should not be zero. If it is, there is a problem. Seems to affect turfs / fixtures created by clients...
							// Instead of hiding the error, we will explicitly show it.
							 NetIdError();
							 
						} else {
							
							// Network examine
							if (!isServer){
								// Clients must request the Rpc through a Command to the Server
								CmdRequestExamine(identity, transform.gameObject);
							} else {
								// The Server has already done the checks, so simply update the UI.
								UpdateExamine(examinables);
							}
						}
                    }
					
                    return;
                }
            }

            ClearExamine();
			
        }
		*/

		/*
        [Command]
        private void CmdRequestExamine(NetworkIdentity target, GameObject examinator)
        {
            Debug.Log("CmdRequestExamine called");
			string hoverText = null;
            IExaminable[] examinables = target.transform.gameObject.GetComponents<IExaminable>();
            Debug.Log("Checking examinables.Length");
            if (examinables.Length < 1)
            {
                //return;
				hoverText = "No examinables on target";
                Debug.Log("No examinables on target");
            }
            Debug.Log("Finished checking examinables.Length");

            // Check view distance
            Vector3 transformPosition = target.transform.position;
            Debug.Log("Checking view distance");
            if (Vector2.Distance(new Vector2(transformPosition.x, transformPosition.z),
                    new Vector2(examinator.transform.position.x, examinator.transform.position.z)) > ViewRange)
            {
                //return;
                Debug.Log("Out of view range");
                hoverText = "Out of view range";
            }
            Debug.Log("Finished checking view distance");

            // Check obstacles
            Debug.Log("Checking Examinator");
            Examinator e = examinator.GetComponent<Examinator>();
            if (e == null) { Debug.Log("e is null"); }

            Debug.Log("Finished checking Examinator");
            Debug.Log("Checking Examinator ViewRange");
            Debug.Log(e.ViewRange);
            Debug.Log("Finished checking Examinator ViewRange");
            Debug.Log("Checking ObstacleMask");
            LayerMask o = e.ObstacleMask;
            Debug.Log("Finished checking ObstacleMask");
            Debug.Log("Checking ObstacleMask value");
            Debug.Log(o.value);
            Debug.Log("Finished checking ObstacleMask value");

            Debug.Log("Checking obstacles");
            if (Physics.Linecast(examinator.transform.position, transformPosition, examinator.GetComponent<Examinator>().ObstacleMask))
            {
                //return;
				hoverText = "Obstacles in the way";
                Debug.Log("Obstacles in the way");
            }
            Debug.Log("Finished checking obstacles");

            if (hoverText == null){
				hoverText = GetHoverText(examinables, examinator);
            }
			if (hoverText != null)
            {
                Debug.Log("TargetExamine(" + hoverText + ")");
                TargetExamine(hoverText);
			}
            Debug.Log("Finished CmdRequestExamine");
        }
		*/
		
		[TargetRpc]
		private void TargetExamine(string text)
		{
            //examineUi.SetText(text);   //********************************************
			uiInstance.SetActive(true);			
		}

        private void UpdateExamine(IExaminable[] examinables)
        {
			IExamineData[] data = new IExamineData[examinables.Length];
			int i = 0;
			foreach (IExaminable examinable in examinables)
			{
				data[i++] = examinable.GetData();
			}
			examineUi.LoadExamineData(data);
			
			/*
            string text = GetHoverText(examinables, gameObject);
            if (text != null)
            {
                examineUi.SetText(text);
                uiInstance.SetActive(true);
            }
            else
            {
                ClearExamine();
            }
			*/
        }
		/*
        private string GetHoverText(IExaminable[] examinables, GameObject examinator)
        {
            StringBuilder builder = new StringBuilder();

            GameObject go = examinator;
            foreach (var examinable in examinables)
            {
                if (examinable.CanExamine(go))
                {
					
					string displayName = examinable.GetName(go);
					string displayDesc = examinable.GetDescription(go);
					
					// Prevent blank lines being appended (relevant where a GameObject has multiple components implementing iExaminable.
					// (in this case, make displayName blank in all but one of them. For example, see Water Cooler prefab)
					if (displayName != ""){
						builder.AppendLine("<b>" + displayName + "</b>");
					}
                    if (displayDesc != ""){
						builder.AppendLine(displayDesc);
					}
					
                }
            }

            if (builder.Length < 1)
            {
                //return null;
				return "Null returned from HoverText";
            }

            return builder.ToString();
        }
		*/

        private void ClearExamine()
        {
            uiInstance.SetActive(false);
            currentTarget = null;
        }
		
		/*
        private void NetIdError()
        {
            //examineUi.SetText("NetID error on Client.");   //*************************************
            uiInstance.SetActive(true);
        }
		*/
    }
}