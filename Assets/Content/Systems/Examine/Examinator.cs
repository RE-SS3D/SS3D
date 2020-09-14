using System.Text;
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

        private GameObject uiInstance;
        private ExamineUI examineUi;
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

            Assert.IsNotNull(UiPrefab);
            uiInstance = Instantiate(UiPrefab);
            examineUi = uiInstance.GetComponent<ExamineUI>();
            Assert.IsNotNull(examineUi);
            uiInstance.SetActive(false);
			selector = Camera.main.transform.GetChild(0).GetComponent<CompositeItemSelector>();
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
                Camera camera = Camera.main;
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
				selector.DisableCamera();
            }
        }

        private void CalculateExamine()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

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

                    // Check if object is networked synced
                    NetworkIdentity identity = hitObject.GetComponent<NetworkIdentity>();
                    if (identity == null)
                    {
                        // Examine non-networked items
                        UpdateExamine(examinables);
                    }
                    else
                    {
						if (identity.netId == 0){
							
							// Treat this as a non-networked item. A bit hacky. Seems to affect turfs / fixtures created by clients...
							 UpdateExamine(examinables);
							 
						} else {
							
							// Network examine
							if (!isServer){
								// Clients must request the Rpc through a Command to the Server
								CmdRequestExamine(identity);
							} else {
								// The Server can perform the Rpc directly.
								TargetExamine(identity);
							}				
							
						}
						
						
						

                    }

                    return;
                }
            }

            ClearExamine();
        }


        [Command]
        private void CmdRequestExamine(NetworkIdentity target)
        {
			TargetExamine(target);
		}
		
		
		[TargetRpc]
		private void TargetExamine(NetworkIdentity target)
		{

            IExaminable[] examinables = target.GetComponents<IExaminable>();
            if (examinables.Length < 1)
            {
                return;
            }

            // Check view distance
            Vector3 transformPosition = target.transform.position;
            if (Vector2.Distance(new Vector2(transformPosition.x, transformPosition.z),
                    new Vector2(transform.position.x, transform.position.z)) > ViewRange)
            {
                return;
            }

            // Check obstacles
            if (Physics.Linecast(transform.position, transformPosition, ObstacleMask))
            {
                return;
            }
			
            string hoverText = GetHoverText(examinables);
            if (hoverText != null)
            {
                examineUi.SetText(hoverText);
				uiInstance.SetActive(true);
            }			
		}

        private void UpdateExamine(IExaminable[] examinables)
        {
            string text = GetHoverText(examinables);
            if (text != null)
            {
                examineUi.SetText(text);
                uiInstance.SetActive(true);
            }
            else
            {
                ClearExamine();
            }
        }

        private string GetHoverText(IExaminable[] examinables)
        {
            StringBuilder builder = new StringBuilder();

            GameObject go = gameObject;
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
                return null;
            }

            return builder.ToString();
        }

        private void ClearExamine()
        {
            uiInstance.SetActive(false);
            currentTarget = null;
        }
    }
}