using SS3D.Core.Behaviours;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Systems.Screens
{
    /// <summary>
    /// This handles the camera following the player when it is spawned.
    /// Also controls the rotation and zoom 
    /// </summary>
    public class CameraFollow : SpessBehaviour
    {
        /// <summary>
        /// The object to follow
        /// </summary>
        [Header("Camera Settings")]
        [SerializeField] private GameObject _target;
        /// <summary>
        /// total distance from the target
        /// </summary>
        [SerializeField] private float _distance = 3f;
        /// <summary>
        /// horizontal angle of the camera (around the z axis)
        /// </summary>
        [SerializeField] private float _horizontalAngle = 90f;
        /// <summary>
        /// angle above the player
        /// </summary>
        [SerializeField] private float _verticalAngle = 60f;

        // Previous button downs for left and right axis movement
        private float _prevHorizontalAxisPress;
        private float _currentHorizontalAngle;
        private float _currentDistance;
        // Offset of target transform position to camera focus point.
        private Vector3 _playerOffset;

        // Sensitivities and Accelerations
        // How quickly distance changes
        private const float DISTANCE_ACCELERATION = 15.0f; 
        private const float ANGLE_ACCELERATION = 8f;
        // The exponential effect of distance
        private const float DISTANCE_SCALING = 1.18f; 

        private const float HORIZONTAL_ROTATION_SENSITIVITY = 150f;
        private const float VERTICAL_ROTATION_SENSITIVITY = 80f;

        // Limits
        private const float MIN_VERTICAL_ANGLE = 10f;
        private const float MAX_VERTICAL_ANGLE = 80f;

        private const float MIN_DISTANCE = 3f;
        private const float MAX_DISTANCE = 15f;

        private const float CARDINAL_SNAP_TIME = 0.3f;

        /// <summary>
        /// Updates the target the camera is meant to follow
        /// </summary>
        /// <param name="target">The target for the camera to follow</param>
        public void SetTarget(GameObject target)
        {
            _target = target;

            // Set the player height based on the character controller, if one is found
            CharacterController character = target.GetComponent<CharacterController>();
            if (character)
            {
                _playerOffset = new Vector3(0, character.height);
            }
        }

       /// <summary>
       /// Gather inputs used to determine new camera values
       /// </summary>
        public void Update()
        {
            // Ignore camera controls when the mouse is over the UI
            if (EventSystem.current.IsPointerOverGameObject())
                return;
        
            // Check for double tap
            if (Input.GetButtonDown("Camera Rotation"))
                _prevHorizontalAxisPress = Time.time;

            // If a double tap actually works
            // Round to closest 90 degree angle, going up or down based on whether axis is positive or negative
            if (Input.GetButtonUp("Camera Rotation") && (Time.time - _prevHorizontalAxisPress) < CARDINAL_SNAP_TIME)
            {
                _horizontalAngle = Mathf.Round((_horizontalAngle + (Input.GetAxis("Camera Rotation") > 0 ? 45.1f : -45.1f)) / 90.0f) * 90.0f;
                _prevHorizontalAxisPress = 0.0f;
                return;
            }

            // input handling
            float zoom = Input.GetAxis("Camera Zoom");
            float horizontalAngleDelta = 0.0f;
            float verticalAngleDelta = 0.0f;

            if (Input.GetButton("Camera Rotation") && (Time.time - _prevHorizontalAxisPress) > CARDINAL_SNAP_TIME)
            { 
                horizontalAngleDelta = Input.GetAxis("Camera Rotation") * HORIZONTAL_ROTATION_SENSITIVITY * Time.deltaTime;
            }
            if (Input.GetButton("Camera Vertical Rotation"))
            {
                verticalAngleDelta = Input.GetAxis("Camera Vertical Rotation") * VERTICAL_ROTATION_SENSITIVITY * Time.deltaTime;
            }

            // Determine new values, clamping as necessary
            _distance = Mathf.Clamp(_distance - zoom, MIN_DISTANCE, MAX_DISTANCE);
            _horizontalAngle = (_horizontalAngle + horizontalAngleDelta) % 360f;
            _verticalAngle = Mathf.Clamp(_verticalAngle + verticalAngleDelta, MIN_VERTICAL_ANGLE, MAX_VERTICAL_ANGLE);
        }

       /// <summary>
       /// Determine camera position after any physics/player movement
       /// </summary>
        public void LateUpdate()
        {
            // if there is no target exit out of update
            if (!_target)
            {
                return;
            }

            if (EventSystem.current.IsPointerOverGameObject())
                return;

            // Smooth the distance and angle before using it
            _currentHorizontalAngle = Mathf.LerpAngle(_currentHorizontalAngle, _horizontalAngle, Time.deltaTime * ANGLE_ACCELERATION);
            _currentDistance = Mathf.MoveTowards(_currentDistance, _distance, Time.deltaTime * DISTANCE_ACCELERATION);
            // The position is determined by the orientation and the distance, where distance has an exponential effect.
            Vector3 relativePosition = Quaternion.Euler(0, _currentHorizontalAngle, _verticalAngle) * new Vector3(Mathf.Pow(DISTANCE_SCALING, _currentDistance), 0, 0);
            // Determine the part of the target we want to follow
            Vector3 targetPosition = _target.transform.position + _playerOffset;

            // Look at that part from the correct position
            Position = targetPosition + relativePosition;
            LookAt(targetPosition);
        }
    }
}
