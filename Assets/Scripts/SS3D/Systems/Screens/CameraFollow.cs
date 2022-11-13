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
        [SerializeField] private GameObject target;
        /// <summary>
        /// total distance from the target
        /// </summary>
        [SerializeField] private float cameraDistance = 3f;
        /// <summary>
        /// horizontal angle of the camera (around the z axis)
        /// </summary>
        [SerializeField] private float horizontalAngle = 90f;
        /// <summary>
        /// angle above the player
        /// </summary>
        [SerializeField] private float verticalAngle = 60f;
        /// <summary>
        /// Camera speed during transition
        /// </summary>
        private float transitionSpeed;
        /// <summary>
        /// Becomes true at the begging of transition
        /// </summary>
        private bool inTransition = false;
        /// <summary>
        /// If distance between camera and end point less then this value, transition ends
        /// </summary>
        private float endTransitionDistance;
        /// <summary>
        /// Offset value for the end point of transition. The lower the value, the more transition slows down at the end
        /// </summary>
        private float newPositionOffsetMult = 0.1f;
        /// <summary>
        /// While in transition stores target's position at previous update 
        /// </summary>
        private Vector3 prevTargetPostion;
        // Previous button downs for left and right axis movement
        private float prevHorizontalAxisPress;
        private float prevHorizontalRotation;
        private float currentHorizontalAngle;
        private float currentDistance;
        /// <summary>
        /// Offset of target transform position to camera focus point.
        /// </summary>
        private Vector3 playerOffset;

        // Sensitivities and Accelerations
        // How quickly distance changes
        private const float DistanceAcceleration = 15.0f; 
        private const float AngleAcceleration = 8f;
        private const float TransitionAcceleration = 0.4f;
        /// <summary>
        /// The exponential effect of distance
        /// </summary>
        private const float DistanceScaling = 1.18f; 

        private const float HorizontalRotationSensitivity = 150f;
        private const float VerticalRotationSensitivity = 80f;

        // Limits
        private const float _minTransitionSpeed = 3f;
        private const float _maxTransitionSpeed = 6f;

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
                prevHorizontalAxisPress = Time.time;
                prevHorizontalRotation = horizontalRotation;
            }

            // If a double tap actually works
            // Round to closest 90 degree angle, going up or down based on whether axis is positive or negative
            if (rotationButtonUp && Time.time - prevHorizontalAxisPress < CardinalSnapTime)
            {
                horizontalAngle = Mathf.Round((horizontalAngle + (prevHorizontalRotation > 0 ? -SnapAngle : SnapAngle)) / 90.0f) * 90.0f;
                prevHorizontalAxisPress = 0.0f;
                return;
            }

            // input handling
            float horizontalAngleDelta = 0.0f;
            float verticalAngleDelta = 0.0f;

            if (horizontalRotationPressed && (Time.time - prevHorizontalAxisPress) > CardinalSnapTime)
            { 
                horizontalAngleDelta = -horizontalRotation * HorizontalRotationSensitivity * Time.deltaTime;
            }
            if (verticalRotationPressed)
            {
                verticalAngleDelta = verticalRotation * VerticalRotationSensitivity * Time.deltaTime;
            }

            // Determine new values, clamping as necessary
            cameraDistance = Mathf.Clamp(cameraDistance - zoom, MinDistance, MaxDistance);
            horizontalAngle = (horizontalAngle + horizontalAngleDelta) % 360f;
            verticalAngle = Mathf.Clamp(verticalAngle + verticalAngleDelta, MinVerticalAngle, MaxVerticalAngle);
        }

        /// <summary>
       /// Determine camera position after any physics/player movement
       /// </summary>
        private void ProcessCameraPositionPostPhysics()
        {
            // if there is no target exit out of update
            if (!target)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (inTransition)
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
            currentHorizontalAngle = Mathf.LerpAngle(currentHorizontalAngle, horizontalAngle, Time.deltaTime * AngleAcceleration);
            currentDistance = Mathf.MoveTowards(currentDistance, cameraDistance, Time.deltaTime * DistanceAcceleration);
            // The position is determined by the orientation and the distance, where distance has an exponential effect.
            Vector3 relativePosition = Quaternion.Euler(0, currentHorizontalAngle, verticalAngle) * new Vector3(Mathf.Pow(DistanceScaling, currentDistance), 0, 0);
            // Determine the part of the target we want to follow
            Vector3 targetPosition = target.transform.position + playerOffset;

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
            Vector3 relativePosition = Quaternion.Euler(0, currentHorizontalAngle, verticalAngle) * new Vector3(Mathf.Pow(DistanceScaling, currentDistance), 0, 0);
            // End point of transition
            Vector3 newPosition = target.transform.position + playerOffset + relativePosition;
            // If camera is close enough to the target, transition ends
            if (Vector3.Distance(Position, newPosition) <= endTransitionDistance)
            {
                inTransition = false;
                return;
            }
            //The lower the offset, the more transition slows down at the end
            Vector3 newPositionOffset = Vector3.Normalize(newPosition - Position) * newPositionOffsetMult;
            Position = Vector3.Lerp(Position, newPosition + newPositionOffset, transitionSpeed * Time.deltaTime);
            Position += target.transform.position - prevTargetPostion;
            prevTargetPostion = target.transform.position;
        }
        /// <summary>
        /// Set variables for moving to the new target
        /// </summary>
        /// <param name="newTarget"></param>
        private void TransitToNewTargetStart(GameObject newTarget)
        {
            if (target == null)
                return;
            inTransition = true;
            float distance = Vector3.Distance(transform.position, newTarget.transform.position);
            // Larger distance - larger speed
            transitionSpeed = Math.Clamp(distance * TransitionAcceleration, _minTransitionSpeed, _maxTransitionSpeed);
            // Smoothes movement at the end
            endTransitionDistance = 0.05f / transitionSpeed;
            prevTargetPostion = newTarget.transform.position;
        }

        /// <summary>
        /// Update the target the camera is meant to follow
        /// </summary>
        /// <param name="target">The target for the camera to follow</param>
        public void SetTarget(GameObject newTarget)
        {
            // Set the player height based on the character controller, if one is found
            CharacterController character = newTarget.GetComponent<CharacterController>();
            if (character)
            {
                playerOffset = new Vector3(0, character.height);
            }
            TransitToNewTargetStart(newTarget);
            target = newTarget;
        }
    }
}
