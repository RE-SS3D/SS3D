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
        private const float DistanceAcceleration = 15.0f; 
        private const float AngleAcceleration = 8f;
        // The exponential effect of distance
        private const float DistanceScaling = 1.18f; 

        private const float HorizontalRotationSensitivity = 150f;
        private const float VerticalRotationSensitivity = 80f;

        // Limits
        private const float MinVerticalAngle = 10f;
        private const float MaxVerticalAngle = 80f;

        private const float MinDistance = 3f;
        private const float MaxDistance = 15f;

        private const float CardinalSnapTime = 0.3f;
        private const float SnapAngle = 45.1f;

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);
            
            ProcessCameraFollow();
        }

        protected override void HandleLateUpdate(float deltaTime)
        {
            base.HandleLateUpdate(deltaTime);

            ProcessCameraPositionPostPhysics();
        }

        /// <summary>
       /// Gather inputs used to determine new camera values
       /// </summary>
        private void ProcessCameraFollow()
        {
            // Ignore camera controls when the mouse is over the UI
            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
            }

            bool horizontalRotationPressed = Input.GetButton("Camera Rotation");
            bool verticalRotationPressed = Input.GetButton("Camera Vertical Rotation");

            float horizontalRotation = Input.GetAxis("Camera Rotation");
            float verticalRotation = Input.GetAxis("Camera Vertical Rotation");

            bool rotationButtonDown = Input.GetButtonDown("Camera Rotation");
            bool rotationButtonUp = Input.GetButtonUp("Camera Rotation");
            float zoom = Input.GetAxis("Camera Zoom");

            // Check for double tap
            if (rotationButtonDown)
            {
                _prevHorizontalAxisPress = Time.time;
            }

            // If a double tap actually works
            // Round to closest 90 degree angle, going up or down based on whether axis is positive or negative
            if (rotationButtonUp && Time.time - _prevHorizontalAxisPress < CardinalSnapTime)
            {
                _horizontalAngle = Mathf.Round((_horizontalAngle + (horizontalRotation > 0 ? SnapAngle : -SnapAngle)) / 90.0f) * 90.0f;
                _prevHorizontalAxisPress = 0.0f;
                return;
            }

            // input handling
            float horizontalAngleDelta = 0.0f;
            float verticalAngleDelta = 0.0f;

            if (horizontalRotationPressed && (Time.time - _prevHorizontalAxisPress) > CardinalSnapTime)
            { 
                horizontalAngleDelta = horizontalRotation * HorizontalRotationSensitivity * Time.deltaTime;
            }
            if (verticalRotationPressed)
            {
                verticalAngleDelta = verticalRotation * VerticalRotationSensitivity * Time.deltaTime;
            }

            // Determine new values, clamping as necessary
            _distance = Mathf.Clamp(_distance - zoom, MinDistance, MaxDistance);
            _horizontalAngle = (_horizontalAngle + horizontalAngleDelta) % 360f;
            _verticalAngle = Mathf.Clamp(_verticalAngle + verticalAngleDelta, MinVerticalAngle, MaxVerticalAngle);
        }

        /// <summary>
       /// Determine camera position after any physics/player movement
       /// </summary>
        private void ProcessCameraPositionPostPhysics()
        {
            // if there is no target exit out of update
            if (!_target)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Smooth the distance and angle before using it
            _currentHorizontalAngle = Mathf.LerpAngle(_currentHorizontalAngle, _horizontalAngle, Time.deltaTime * AngleAcceleration);
            _currentDistance = Mathf.MoveTowards(_currentDistance, _distance, Time.deltaTime * DistanceAcceleration);
            // The position is determined by the orientation and the distance, where distance has an exponential effect.
            Vector3 relativePosition = Quaternion.Euler(0, _currentHorizontalAngle, _verticalAngle) * new Vector3(Mathf.Pow(DistanceScaling, _currentDistance), 0, 0);
            // Determine the part of the target we want to follow
            Vector3 targetPosition = _target.transform.position + _playerOffset;

            // Look at that part from the correct position
            Position = targetPosition + relativePosition;
            LookAt(targetPosition);
        }

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
    }
}
