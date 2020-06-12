using Mirror;
using SS3D.Engine.Examine;
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

        private GameObject uiInstance;
        private ExamineUi examineUi;
        private Vector2 lastMousePosition;
        private Vector3 lastCameraPosition;
        private Quaternion lastCameraRotation;
        private IExaminable currentExaminable;
        
        private void Start()
        {
            // Mirror is kinda whack
            if (!hasAuthority)
            {
                Destroy(this);
            }
            
            Assert.IsNotNull(UiPrefab);
            uiInstance = Instantiate(UiPrefab);
            examineUi = uiInstance.GetComponent<ExamineUi>();
            Assert.IsNotNull(examineUi);
            uiInstance.SetActive(false);
        }

        private void Update()
        {
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
            Ray ray = camera.ScreenPointToRay(lastMousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                GameObject hitObject = hit.transform.gameObject;
                IExaminable examinable = hitObject.GetComponent<IExaminable>();
                if (examinable != null)
                {
                    if (currentExaminable == examinable)
                    {
                        return;
                    }

                    currentExaminable = examinable;
                    
                    NetworkIdentity identity = hitObject.GetComponent<NetworkIdentity>();
                    if (identity == null)
                    {
                        UpdateExamine(examinable);
                    }
                    else
                    {
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
            IExaminable examinable = target.GetComponent<IExaminable>();
            if (examinable == null || !examinable.CanExamine(gameObject))
            {
                return;
            }
            
            TargetExamine(examinable.GetDescription(gameObject));
        }

        [TargetRpc]
        private void TargetExamine(string text)
        {
            examineUi.SetText(text);
            uiInstance.SetActive(true);
        }

        private void UpdateExamine(IExaminable examinable)
        {
            if (examinable.CanExamine(gameObject))
            {
                examineUi.SetText(examinable.GetDescription(gameObject));
                uiInstance.SetActive(true);
            }
            else
            {
                ClearExamine();
            }
        }

        private void ClearExamine()
        {
            uiInstance.SetActive(false);
            currentExaminable = null;
        }
    }
}