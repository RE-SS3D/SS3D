using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core.Behaviours;
using UnityEngine;
using System;
using System.Collections;
using SS3D.Core;
using SS3D.Systems.Inputs;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Screens
{
    /// <summary>
    /// This handles the camera following the player when it is spawned.
    /// Also controls the rotation and zoom 
    /// </summary>
    public class CameraFollow : Actor
    {
        #region Fields
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
        /// Becomes true at the begging of transition and false at the end
        /// </summary>
        private bool _inTransition;
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
        private float _currentVerticalAngle;
        private float _currentDistance;
        /// <summary>
        /// Offset of target transform position to camera focus point.
        /// </summary>
        private Vector3 _playerOffset;
        
        private Controls.CameraActions _controls;
        private InputSubSystem _inputSystem;
        
        // Sensitivities and Accelerations
        private const float DistanceAcceleration = 10.0f;
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
        private const float SnapAngle = 45.1f;
        private const float MouseSnapThreshold = 0.04f;

        #endregion

        protected override void OnStart()
        {
            base.OnStart();
            _inputSystem = SubSystems.Get<InputSubSystem>();
            _controls = _inputSystem.Inputs.Camera;
            _controls.Zoom.performed += HandleZoom;
            _controls.SnapRight.performed += HandleSnapRight;
            _controls.SnapLeft.performed += HandleSnapLeft;
            _controls.MouseRotation.performed += HandleMouseRotation;
            _inputSystem.ToggleActionMap(_controls, true);

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            
            _controls.Zoom.performed -= HandleZoom;
            _controls.SnapRight.performed -= HandleSnapRight;
            _controls.SnapLeft.performed -= HandleSnapLeft;
            _controls.MouseRotation.performed -= HandleMouseRotation;
            _inputSystem.ToggleActionMap(_controls, false);
        }
        
        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            ProcessCameraPosition();
        }
        
        private void HandleZoom(InputAction.CallbackContext context) 
        {
           _cameraDistance = Mathf.Clamp(_cameraDistance - context.ReadValue<float>(), MinDistance, MaxDistance); 
        }
        
        // There are two button-type actions for snap, because Tap actions don't return values when performed
        private void HandleSnapLeft(InputAction.CallbackContext context) 
        {
           Snap(true); 
        }
        
        private void HandleSnapRight(InputAction.CallbackContext context) 
        {
           Snap(false); 
        }
        
        private void Snap(bool isLeft)
        {
            int direction = isLeft? 1 : -1;
            _horizontalAngle = Mathf.Round((_horizontalAngle + SnapAngle * direction) / 90.0f) * 90.0f; 
        }
        private void HandleMouseRotation(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>() * _inputSystem.MouseSensitivity;
            if (Math.Abs(value) > MouseSnapThreshold)
            {
                Snap(value > 0);
                _inputSystem.ToggleAction(_controls.MouseRotation, false);
                StartCoroutine(MouseRotationTimeout(.4f));
            }
            else
            {
                _horizontalAngle += value * HorizontalRotationSensitivity;
            }
        }

        private IEnumerator MouseRotationTimeout(float time)
        {
            yield return new WaitForSeconds(time);
            _inputSystem.ToggleAction(_controls.MouseRotation, true);
        }

        /// <summary>
        /// Determine camera position after any physics/player movement
        /// </summary>
        private void ProcessCameraPosition()
        {
            // if there is no target exit out of update
            if (!_target)
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
            _horizontalAngle = (_horizontalAngle + _controls.HorizontalRotation.ReadValue<float>() 
                * HorizontalRotationSensitivity * Time.deltaTime) % 360;
            _verticalAngle = Mathf.Clamp(_verticalAngle + _controls.VerticalRotation.ReadValue<float>() 
                * VerticalRotationSensitivity * Time.deltaTime, MinVerticalAngle, MaxVerticalAngle);
            // Smooth the distance and angle before using it
            _currentHorizontalAngle = Mathf.LerpAngle(_currentHorizontalAngle, _horizontalAngle, 
                Time.deltaTime * AngleAcceleration);
            _currentVerticalAngle = Mathf.LerpAngle(_currentVerticalAngle, _verticalAngle, 
                Time.deltaTime * AngleAcceleration);
            _currentDistance = Mathf.MoveTowards(_currentDistance, _cameraDistance, 
                Time.deltaTime * DistanceAcceleration);
            // The position is determined by the orientation and the distance, where distance has an exponential effect.
            Vector3 relativePosition = Quaternion.Euler(0, _currentHorizontalAngle, _verticalAngle) 
                                       * new Vector3(Mathf.Pow(DistanceScaling, _currentDistance), 0, 0);
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
            Vector3 targetPosition = _target.transform.position;
            // End point of transition
            Vector3 newPosition = targetPosition + _playerOffset + relativePosition;
            if (Vector3.Distance(Position, newPosition) <= _endTransitionDistance)
            {
                _inTransition = false;
                _inputSystem.ToggleActionMap(_controls, true);
                return;
            }
            //The lower the offset, the more transition slows down at the end
            Vector3 newPositionOffset = Vector3.Normalize(newPosition - Position) * NewPositionOffsetMult;
            Position = Vector3.Lerp(Position, newPosition + newPositionOffset, _transitionSpeed * Time.deltaTime);
            Position += targetPosition - _prevTargetPosition;
            _prevTargetPosition = targetPosition;
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
            Vector3 targetPosition = newTarget.transform.position;
            float distance = Vector3.Distance(transform.position, targetPosition);
            // Larger distance - larger speed
            _transitionSpeed = Math.Clamp(distance * TransitionAcceleration, MinTransitionSpeed, MaxTransitionSpeed);
            // Smoothes movement at the end
            _endTransitionDistance = 0.05f / _transitionSpeed;
            _prevTargetPosition = targetPosition;
            _inputSystem.ToggleActionMap(_controls, false);
        }

        /// <summary>
        /// Update the target the camera is meant to follow
        /// </summary>
        /// <param name="newTarget">The target for the camera to follow</param>
        public void SetTarget(GameObject newTarget)
        {
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
