using SS3D.Engine.FOV;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Content.Systems.Player
{
    public class CameraFollow : MonoBehaviour
    {
        // Sensitivities and Accelerations
        private const float DISTANCE_ACCELERATION = 15.0f; // How quickly distance changes
        private const float ANGLE_ACCELERATION = 8f;
        private const float DISTANCE_SCALING = 1.18f; // The exponential effect of distance

        private const float HORIZONTAL_ROTATION_SENSITIVITY = 150f;
        private const float VERTICAL_ROTATION_SENSITIVITY = 80f;

        // Limits
        private const float MIN_VERTICAL_ANGLE = 10f;
        private const float MAX_VERTICAL_ANGLE = 80f;

        private const float MIN_DISTANCE = 2f;
        private const float MAX_DISTANCE = 15f;

        private const float CARDINAL_SNAP_TIME = 0.3f;

        // The object to follow
        public GameObject target = null;

        // field of view object
        public FieldOfView fieldOfView = null;

        // the offset
        public float distance = 3f; // total distance from the target
        public float angle = 90f; // horizontal angle of the camera (around the z axis)
        public float vAngle = 60f; // angle above the player
        public float yOffset = 1f;

        [SerializeField, Min(0)] private float horizontalRotationFlickLimit = 5f;

        // Previous button downs for left and right axis movement
        private float prevHorizontalRotationSnap = 0.0f;
        private float prevHorizontalAxisPress = 0.0f;

        private float rotationDuration;
        private float curHorizontalAngle;
        private float currentDistance;

        // Offset of target transform position to camera focus point.
        private Vector3 playerOffset = new Vector3();

        private void Start()
        {
            CameraManager.singleton.playerCamera = this.GetComponent<Camera>();
        }

        /**
         * Updates the target the camera is meant to follow
         */
        public void SetTarget(GameObject target)
        {
            this.target = target;
            if (fieldOfView != null)
            {
                fieldOfView.target = target.transform;
                fieldOfView.enabled = true;
            }

            // Set the player height based on the character controller, if one is found
            var character = target.GetComponent<CharacterController>();
            if (character)
                playerOffset = new Vector3(0, target.GetComponent<CharacterController>().height);
        }

        /**
         * Gather inputs used to determine new camera values
         */
        public void Update()
        {
            // Ignore camera controls when the mouse is over the UI
            //if (EventSystem.current.IsPointerOverGameObject())
            //    return;

            // input handling
            float zoom = Input.GetAxis("Camera Zoom");
            float angleDelta = 0.0f;
            float vAngleDelta = 0.0f;

            // remember when rotation button was pressed
            if (Input.GetButtonDown("Camera Rotation"))
            {
                prevHorizontalAxisPress = Time.time;
            }

            // middle mouse button rotation 
            if (Input.GetKey(KeyCode.Mouse2) && (Time.time - prevHorizontalRotationSnap) > CARDINAL_SNAP_TIME)
            {
                angleDelta = Input.GetAxis("Mouse X");
                rotationDuration += Time.deltaTime;

                // Snap rotation to closest 90 degree angle in direction of mouse movement if mouse speed exceeds limit
                if (Mathf.Abs(angleDelta) >= horizontalRotationFlickLimit && rotationDuration <= CARDINAL_SNAP_TIME)
                {
                    angle = Mathf.Round((angle + Mathf.Sign(angleDelta) * 45.1f) / 90.0f) * 90.0f;
                    prevHorizontalRotationSnap = Time.time;
                    return;
                }
            }
            else
            {
                rotationDuration = 0;
            }

            // ctrl + q/e rotation
            if (Input.GetKey(KeyCode.LeftControl))
            {
                // snap camera if rotation button tapped
                if (Input.GetButtonUp("Camera Rotation") && (Time.time - prevHorizontalAxisPress) < CARDINAL_SNAP_TIME)
                {
                    angleDelta = Input.GetAxis("Camera Rotation");
                    angle = Mathf.Round((angle + Mathf.Sign(angleDelta) * 45.1f) / 90.0f) * 90.0f;
                    prevHorizontalAxisPress = 0.0f;
                    return;
                }
                
                // rotate camera if rotation button is held down
                if (Input.GetButton("Camera Rotation") && (Time.time - prevHorizontalAxisPress) > CARDINAL_SNAP_TIME)
                {
                    angleDelta = Input.GetAxis("Camera Rotation") * HORIZONTAL_ROTATION_SENSITIVITY * Time.deltaTime;
                }
            }

            if (Input.GetButton("Camera Vertical Rotation"))
                vAngleDelta = Input.GetAxis("Camera Vertical Rotation") * VERTICAL_ROTATION_SENSITIVITY *
                              Time.deltaTime;

            // Determine new values, clamping as necessary
            distance = Mathf.Clamp(distance - zoom, MIN_DISTANCE, MAX_DISTANCE);
            angle = (angle + angleDelta) % 360f;
            vAngle = Mathf.Clamp(vAngle + vAngleDelta, MIN_VERTICAL_ANGLE, MAX_VERTICAL_ANGLE);
        }

        /**
         * Determine camera position after any physics/player movement
         */
        public void LateUpdate()
        {
            // if there is no target exit out of update
            if (!target)
            {
                return;
            }

            // Smooth the distance and angle before using it
            curHorizontalAngle = Mathf.LerpAngle(curHorizontalAngle, angle, Time.deltaTime * ANGLE_ACCELERATION);
            currentDistance = Mathf.MoveTowards(currentDistance, distance, Time.deltaTime * DISTANCE_ACCELERATION);
            // The position is determined by the orientation and the distance, where distance has an exponential effect.
            Vector3 relativePosition = Quaternion.Euler(0, curHorizontalAngle, vAngle) *
                                       new Vector3(Mathf.Pow(DISTANCE_SCALING, currentDistance), 0, 0);
            // Determine the part of the target we want to follow
            Vector3 targetPosition = target.transform.position +
                                     new Vector3(playerOffset.x, playerOffset.y - yOffset, playerOffset.z);

            // Look at that part from the correct position
            transform.position = targetPosition + relativePosition;
            transform.LookAt(targetPosition);
        }
    }
}