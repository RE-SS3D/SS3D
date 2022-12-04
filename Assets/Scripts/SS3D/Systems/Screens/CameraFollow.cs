using SS3D.Core.Behaviours;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

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
        [SerializeField] private float _cameraDistance = 3f;
        /// <summary>
        /// horizontal angle of the camera (around the z axis)
        /// </summary>
        [SerializeField] private float _horizontalAngle = 90f;
        /// <summary>
        /// angle above the player
        /// </summary>
        [SerializeField] private float _verticalAngle = 60f;
        /// <summary>
        /// Camera speed during transition
        /// </summary>
        private float _transitionSpeed;
        /// <summary>
        /// Becomes true at the begging of transition
        /// </summary>
        private bool _inTransition = false;
        /// <summary>
        /// If distance between camera and end point less then this value, transition ends
        /// </summary>
        private float _endTransitionDistance;

        /// <summary>
        /// While in transition stores target's position at previous update 
        /// </summary>
        private Vector3 _prevTargetPosition;
        // Previous button downs for left and right axis movement
        private float _prevHorizontalAxisPress;
        private float _prevHorizontalRotation;
        private float _currentHorizontalAngle;
        private float _currentDistance;
        /// <summary>
        /// Offset of target transform position to camera focus point.
        /// </summary>
        private Vector3 _playerOffset;

        // Sensitivities and Accelerations
        // How quickly distance changes
        private const float DistanceAcceleration = 15.0f; 
        private const float AngleAcceleration = 8f;
        private const float TransitionAcceleration = 0.4f;
        /// <summary>
        /// Offset value for the end point of transition. The lower the value, the more transition slows down at the end
        /// </summary>
        private const float NewPositionOffsetMult = 0.1f;
        /// <summary>
        /// The exponential effect of distance
        /// </summary>
        private const float DistanceScaling = 1.18f; 

        private const float HorizontalRotationSensitivity = 150f;
        private const float VerticalRotationSensitivity = 80f;

        // Limits
        private const float MinTransitionSpeed = 3f;
        private const float MaxTransitionSpeed = 6f;

        private const float MinVerticalAngle = 10f;
        private const float MaxVerticalAngle = 80f;

        private const float MinDistance = 3f;
        private const float MaxDistance = 15f;
        /// <summary>
        /// If time between rotationButtonDown and rotationButtonUp is less then this value camera jumps to the angle multiples of 90. 
        /// If bigger then this value, camera smoothly rotates
        /// </summary>
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
                _prevHorizontalRotation = horizontalRotation;
            }

            // If a double tap actually works
            // Round to closest 90 degree angle, going up or down based on whether axis is positive or negative
            if (rotationButtonUp && Time.time - _prevHorizontalAxisPress < CardinalSnapTime)
            {
                _horizontalAngle = Mathf.Round((_horizontalAngle + (_prevHorizontalRotation > 0 ? -SnapAngle : SnapAngle)) / 90.0f) * 90.0f;
                _prevHorizontalAxisPress = 0.0f;
                return;
            }

            // input handling
            float horizontalAngleDelta = 0.0f;
            float verticalAngleDelta = 0.0f;

            if (horizontalRotationPressed && (Time.time - _prevHorizontalAxisPress) > CardinalSnapTime)
            { 
                horizontalAngleDelta = -horizontalRotation * HorizontalRotationSensitivity * Time.deltaTime;
            }
            if (verticalRotationPressed)
            {
                verticalAngleDelta = verticalRotation * VerticalRotationSensitivity * Time.deltaTime;
            }

            // Determine new values, clamping as necessary
            _cameraDistance = Mathf.Clamp(_cameraDistance - zoom, MinDistance, MaxDistance);
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

            if (_inTransition)
            {
                TransitToNewTarget();
            }
            else
            {
                MoveAroundTarget();
            }
        }
        /// <summary>
        /// Move around and with target
        /// </summary>
        private void MoveAroundTarget()
        {
            // Smooth the distance and angle before using it
            _currentHorizontalAngle = Mathf.LerpAngle(_currentHorizontalAngle, _horizontalAngle, Time.deltaTime * AngleAcceleration);
            _currentDistance = Mathf.MoveTowards(_currentDistance, _cameraDistance, Time.deltaTime * DistanceAcceleration);
            // The position is determined by the orientation and the distance, where distance has an exponential effect.
            Vector3 relativePosition = Quaternion.Euler(0, _currentHorizontalAngle, _verticalAngle) * new Vector3(Mathf.Pow(DistanceScaling, _currentDistance), 0, 0);
            // Determine the part of the target we want to follow
            Vector3 targetPosition = _target.transform.position + _playerOffset;

            // Look at that part from the correct position
            Position = targetPosition + relativePosition;
            LookAt(targetPosition);
        }
        /// <summary>
        /// Move to the new target
        /// </summary>
        private void TransitToNewTarget()
        {
            // The position is determined by the orientation and the distance, where distance has an exponential effect.
            Vector3 relativePosition = Quaternion.Euler(0, _currentHorizontalAngle, _verticalAngle) * new Vector3(Mathf.Pow(DistanceScaling, _currentDistance), 0, 0);
            // End point of transition
            Vector3 newPosition = _target.transform.position + _playerOffset + relativePosition;
            // If camera is close enough to the target, transition ends
            if (Vector3.Distance(Position, newPosition) <= _endTransitionDistance)
            {
                _inTransition = false;
                return;
            }
            //The lower the offset, the more transition slows down at the end
            Vector3 newPositionOffset = Vector3.Normalize(newPosition - Position) * NewPositionOffsetMult;
            Position = Vector3.Lerp(Position, newPosition + newPositionOffset, _transitionSpeed * Time.deltaTime);
            Position += _target.transform.position - _prevTargetPosition;
            _prevTargetPosition = _target.transform.position;
        }
        /// <summary>
        /// Set variables for moving to the new target
        /// </summary>
        /// <param name="newTarget"></param>
        private void TransitToNewTargetStart(GameObject newTarget)
        {
            if (_target == null)
                return;
            _inTransition = true;
            float distance = Vector3.Distance(transform.position, newTarget.transform.position);
            // Larger distance - larger speed
            _transitionSpeed = Math.Clamp(distance * TransitionAcceleration, MinTransitionSpeed, MaxTransitionSpeed);
            // Smoothes movement at the end
            _endTransitionDistance = 0.05f / _transitionSpeed;
            _prevTargetPosition = newTarget.transform.position;
        }

        /// <summary>
        /// Update the target the camera is meant to follow
        /// </summary>
        /// <param name="newTarget">The target for the camera to follow</param>
        public void SetTarget(GameObject newTarget)
        {
            if (_target)
                Debug.Log(_target.transform.position);
            // Set the player height based on the character controller, if one is found
            CharacterController character = newTarget.GetComponent<CharacterController>();
            if (character)
            {
                _playerOffset = new Vector3(0, character.height);
            }
            TransitToNewTargetStart(newTarget);
            _target = newTarget;
        }
    }
}
