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
        private GameObject currentTarget;

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
                    // Do nothing if ray hit current object
                    if (currentTarget == hitObject)
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

                    // Check if object is networked synced
                    NetworkIdentity identity = hitObject.GetComponent<NetworkIdentity>();
                    if (identity == null)
                    {
                        // Client examine
                        UpdateExamine(examinables);
                    }
                    else
                    {
                        // Server examine
                        CmdRequestExamine(identity);
                    }

                    return;
                }
            }

            ClearExamine();
        }

        [Command]
        private void CmdRequestExamine(NetworkIdentity target)
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
                TargetExamine(hoverText);
            }
        }

        [TargetRpc]
        private void TargetExamine(string text)
        {
            examineUi.SetText(text);
            uiInstance.SetActive(true);
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
                    builder.AppendLine(examinable.GetDescription(go));
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