﻿using System.Text;
using System.Collections;
using Mirror;
using SS3D.Engine.FOV;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Examine
{
    /// <summary>
    /// Displays the examine ui
    /// </summary>
    public class Examinator : NetworkBehaviour
    {
        public GameObject UiPrefab;

        private Camera camera;
        
        private GameObject uiInstance;
		private ExamineUI examineUi;
		private IEnumerator coroutine;
		

        private Vector2 lastMousePosition;
        private Vector3 lastCameraPosition;
        private Quaternion lastCameraRotation;
		public CompositeItemSelector selector;
        private GameObject currentTarget;
		private float MIN_UPDATES_PER_SECOND = 3f;
		private float updateFrequency;
		private float updateTimer;

        private void Start()
        {
            // Prevent duplicate examinators from being in the scene in multiplayer.
            if (!isLocalPlayer)
            {
                Destroy(this);
				return;
            }

			// Establish our minimum frequency timer. This is used to ensure
			// that objects moving into our cursor are detected.
			updateFrequency = 1f / MIN_UPDATES_PER_SECOND;
			updateTimer = 0f;

            camera = CameraManager.singleton.examineCamera;
            selector = camera.GetComponent<CompositeItemSelector>();
	        
	        Assert.IsNotNull(UiPrefab);
            uiInstance = Instantiate(UiPrefab);
            examineUi = uiInstance.GetComponent<ExamineUI>();
            
        }

		/// This checks whether the Examine button is pressed, and (if so) whether
		/// the cursor has moved significantly since last check. If it has, it will
		/// recalculate what it is looking at.
        private void Update()
        {
            if (!isClient)
            {
                return;
            }

			// Update the position, rotation and time variables
			updateTimer += Time.deltaTime;
            Vector3 mousePosition = Input.mousePosition;
            Vector2 position = new Vector2(mousePosition.x, mousePosition.y);
            Vector3 cameraPos = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;

			// If anything has changed too much, we need to recalculate what object we are looking at.
			if ((Vector2.Distance(position, lastMousePosition) > 1) ||
				(Vector3.Distance(cameraPos, lastCameraPosition) > 0.05) ||
				(Quaternion.Angle(rotation, lastCameraRotation) > 0.1) ||
				(updateTimer > updateFrequency))
            {
				updateTimer = 0f;
                lastMousePosition = position;
                lastCameraPosition = cameraPos;
                lastCameraRotation = rotation;
                CalculateExamine();
            }
        }

		/// This function asks the CompositeItemSelector to recalculate what item
		/// the cursor is over. It triggers a coroutine to check what the current
		/// item is after the rendering has been completed.
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
				// If it's over something, get all the assosciated data and update the UI.
				IExaminable[] examinables = hitObject.GetComponents<IExaminable>();
				UpdateExamine(examinables);
			}
			else{
				// If it's over nothing, get rid of the Examine UI.
				examineUi.ClearData(false);
			}
		}		

		/// This function retrieves the Examine data from the object. It will test each IExaminable
		/// to see if it can actually examine it (i.e. within range, within LOS etc), and if so, will
		/// send the data to the UI.
        private void UpdateExamine(IExaminable[] examinables)
        {
			IExamineData[] data = new IExamineData[examinables.Length];
			int i = 0;
			foreach (IExaminable examinable in examinables)
			{	
				if (examinable.GetRequirements().CanExamine(gameObject))
				{
					data[i++] = examinable.GetData();
				}
			}
			if (i > 0)
			{
				examineUi.LoadExamineData(data);
			}
			else
			{
				examineUi.ClearData(false);
			}
        }
    }
}