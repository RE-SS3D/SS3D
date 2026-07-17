using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inputs;
using SS3D.Systems.Screens;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Actor = SS3D.Core.Behaviours.Actor;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(AnimationOrchestrator))]
    [RequireComponent(typeof(HumanoidBodyStateMachine))]
    [RequireComponent(typeof(Animator))]
    public abstract class HumanoidController : NetworkActor
    {
        #region Fields
        public event Action<float> OnSpeedChangeEvent;
        /// <summary>Local-space planar locomotion (VelX, VelZ) and Turn (-1..1) for the 2D blend tree.</summary>
        public event Action<float, float, float> OnLocomotionVelocityChanged;

        [Header("Components")] 
        [SerializeField] protected Entity _entity;

        [Header("Movement Settings")]
        [SerializeField] protected float _movementSpeed;
        [SerializeField] protected float _lerpMultiplier;
        [SerializeField] protected float _rotationLerpMultiplier;
        /// <summary>Yaw turn rate while aiming in combat (lower than peaceful for less twitchy facing).</summary>
        [SerializeField] protected float _combatRotationLerpMultiplier = 3.5f;
        [SerializeField] protected float _turnVelocityScale = 4f;
        /// <summary>Combat gaits are slower than peaceful — scale world speed to match Mixamo cadence.</summary>
        [SerializeField] protected float _combatWalkSpeedFactor = 0.7f;
        /// <summary>Combat run clips are especially slow vs peaceful run; keep separate from walk.</summary>
        [SerializeField] protected float _combatRunSpeedFactor = 0.3f;

        [Header("Movement IK Targets")]
        [SerializeField] private Transform _movementTarget;

        [Header("Run/Walk")]
        private bool _isRunning;

        [Header("Debug Info")]
        protected Vector3 AbsoluteMovement;
        protected Vector2 Input;
        protected Vector2 SmoothedInput;
        public Vector3 TargetMovement;

        private Actor _camera;
        protected Controls.MovementActions MovementControls;
        protected Controls.HotkeysActions HotkeysControls;
        private InputSubSystem _inputSystem;
        private IInputHandle _gameplayHandle;
        private HumanoidBodyStateMachine _bodyStateMachine;
        private bool _inputSubscribed;
        private float _previousYaw;
        private const float _walkAnimatorValue = .3f;
        private const float _runAnimatorValue = 1f;
        #endregion

        #region Properties
        public virtual float WalkAnimatorValue => _walkAnimatorValue;
        public virtual float RunAnimatorValue => _runAnimatorValue;
        public bool IsRunning => _isRunning;
        protected HumanoidBodyStateMachine BodyStateMachine => _bodyStateMachine;
        #endregion

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (IsOwner)
            {
                SubscribeToInput();
            }
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);

            if (IsOwner)
            {
                SubscribeToInput();
            }
            else if (prevOwner.Equals(LocalConnection))
            {
                UnsubscribeFromInput();
            }
        }
// Must have, Unity doesn't invoke Awake() in NetworkActor and therefore doesn't call OnAwake() without it

        protected void Awake()
        {
            base.Awake();
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            
            Setup();
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

            if (IsOwner)
            {
                SubscribeToInput();
            }
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            if (IsOwner)
            {
                UnsubscribeFromInput();
            }

            TargetMovement = Vector3.zero;
        }

        private void Setup()
        {
            _camera = SubSystems.Get<CameraSubSystem>().PlayerCamera;
            _entity.OnMindChanged += HandleControllingPlayerChanged;
            _bodyStateMachine = GetComponent<HumanoidBodyStateMachine>();
            _previousYaw = transform.eulerAngles.y;
            EnsureInputReady();
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        private void SubscribeToInput()
        {
            EnsureInputReady();
            if (_inputSystem == null || _inputSubscribed)
            {
                return;
            }

            MovementControls.ToggleRun.performed += HandleToggleRun;

            // Gameplay context enables Movement, Camera, Interactions and Hotkeys; it is released
            // deterministically on unsubscribe, so no per-frame re-enabling is needed.
            _gameplayHandle = _inputSystem.PushContext(InputContext.Gameplay);
            _inputSubscribed = true;
        }

        private void EnsureInputReady()
        {
            if (_inputSystem != null)
            {
                return;
            }

            _inputSystem = SubSystems.Get<InputSubSystem>();
            if (_inputSystem == null)
            {
                return;
            }

            Controls controls = _inputSystem.Inputs;
            MovementControls = controls.Movement;
            HotkeysControls = controls.Hotkeys;
        }

        private void UnsubscribeFromInput()
        {
            if (_inputSystem == null || !_inputSubscribed)
            {
                return;
            }

            MovementControls.ToggleRun.performed -= HandleToggleRun;

            _gameplayHandle?.Dispose();
            _gameplayHandle = null;
            _inputSubscribed = false;
        }

        private void HandleControllingPlayerChanged(Mind mind)
        {
            OnSpeedChanged(0);
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
	        if (!enabled)
	        {
		        return;
	        }
            if (!IsOwner)
            {
                return;
            }

            EnsureInputReady();
            if (IsOwner)
            {
                SubscribeToInput();
            }

            if (_bodyStateMachine != null && !_bodyStateMachine.Capabilities.CanMove)
            {
                OnSpeedChanged(0);
                return;
            }
            
            ProcessCharacterMovement();
        }

        /// <summary>
        /// Executes the movement code and updates the IK targets
        /// </summary>
        protected abstract void ProcessCharacterMovement();
    
        /// <summary>
        /// Gets the mouse position and updates the mouse IK targets while maintaining the player height
        /// </summary>
        private void UpdateMousePositionTransforms()
        {
            // Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            // Vector3 mousePos = ray.origin - ray.direction * (ray.origin.y / ray.direction.y);
            // mousePos = new Vector3(mousePos.x, transform.position.y, mousePos.z);
            
            // _mouseDirectionTransform.LookAt(mousePos);
            // _mousePositionTransform.position = mousePos;
        }

        /// <summary>
        /// Moves the movement targets with the given input
        /// </summary>
        /// <param name="movementInput"></param>
         protected void MoveMovementTarget(Vector2 movementInput, float multiplier = 1)
         {
             if (_camera == null)
             {
                 _camera = SubSystems.Get<CameraSubSystem>().PlayerCamera;
             }

             Vector3 forwardBasis = _camera != null
                 ? Vector3.Cross(_camera.Right, Vector3.up).normalized
                 : Vector3.forward;
             Vector3 rightBasis = _camera != null
                 ? Vector3.Cross(Vector3.up, _camera.Forward).normalized
                 : Vector3.right;

             //makes the movement align to the camera view
             Vector3 newTargetMovement =
                 movementInput.y * forwardBasis +
                 movementInput.x * rightBasis;

             float lerpRate = _lerpMultiplier * multiplier;
             // Snap idle→walk; seed idle→run at walk speed so motion accelerates through the gait blend.
             if (TargetMovement.sqrMagnitude < 0.0001f && newTargetMovement.sqrMagnitude > 0.0001f)
             {
                 float targetMag = newTargetMovement.magnitude;
                 if (targetMag <= WalkAnimatorValue + 0.05f)
                 {
                     TargetMovement = newTargetMovement;
                 }
                 else
                 {
                     TargetMovement = newTargetMovement.normalized * WalkAnimatorValue;
                 }
             }
             else
             {
                 TargetMovement = Vector3.Lerp(TargetMovement, newTargetMovement, Time.deltaTime * lerpRate);
             }

             Vector3 resultingMovement = TargetMovement + Position;
            AbsoluteMovement = resultingMovement;
             _movementTarget.position = AbsoluteMovement;
         }

        /// <summary>
        /// Rotates the player to the target movement
        /// </summary>
        protected void RotatePlayerToMovement()
        {
            Quaternion lookRotation = Quaternion.LookRotation(TargetMovement);

            transform.rotation = Quaternion.Slerp(Rotation, lookRotation, Time.deltaTime * _rotationLerpMultiplier);
        }

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        protected abstract void MovePlayer();
        
        /// <summary>
        /// Process the player movement input, smoothing it
        /// </summary>
        /// <returns></returns>
        protected void ProcessPlayerInput()
        {
            EnsureInputReady();

            if (_camera == null)
            {
                _camera = SubSystems.Get<CameraSubSystem>().PlayerCamera;
            }

            float x = MovementControls.Movement.ReadValue<Vector2>().x;
            float y = MovementControls.Movement.ReadValue<Vector2>().y;

            float inputFilteredSpeed = FilterSpeed();

            Input = Vector2.ClampMagnitude(new Vector2(x, y), inputFilteredSpeed);
            SmoothedInput = Vector2.Lerp(SmoothedInput, Input, Time.deltaTime * (_lerpMultiplier / 10));

            OnSpeedChanged(Input.magnitude != 0 ? inputFilteredSpeed : 0);
        }

        /// <summary>
        /// Publishes local-space VelX/VelZ for the FreeformCartesian2D locomotion blend.
        /// Peaceful mode faces the move direction, so only forward gait is used.
        /// Combat mode faces the mouse aim point and uses true strafe axes.
        /// </summary>
        protected void PublishLocomotionVelocity(Vector3 worldMoveDirection, float gaitSpeed)
        {
            float velX = 0f;
            float velZ = 0f;

            if (gaitSpeed > 0.01f)
            {
                if (IsCombatMode())
                {
                    Vector3 planar = worldMoveDirection;
                    planar.y = 0f;
                    if (planar.sqrMagnitude > 0.0001f)
                    {
                        Vector3 local = transform.InverseTransformDirection(planar.normalized);
                        velX = local.x * gaitSpeed;
                        velZ = local.z * gaitSpeed;
                    }
                }
                else
                {
                    // Body rotates to face movement — keep blend on the forward axis only so
                    // turn/strafe clips do not pop in while facing catches up.
                    velZ = gaitSpeed;
                }
            }

            float yaw = transform.eulerAngles.y;
            float yawDelta = Mathf.DeltaAngle(_previousYaw, yaw);
            _previousYaw = yaw;
            float turn = Mathf.Clamp(yawDelta / Mathf.Max(0.01f, _turnVelocityScale), -1f, 1f);

            OnLocomotionVelocityChanged?.Invoke(velX, velZ, turn);
        }

        protected bool IsCombatMode()
        {
            return _bodyStateMachine != null
                && _bodyStateMachine.CombatMode.IsCombat();
        }

        /// <summary>
        /// Combat facing: body yaw toward mouse, head/torso pitch via look-at IK.
        /// </summary>
        protected void RotatePlayerToCombatAim()
        {
            if (!TryGetCombatAim(out float yaw, out float pitch, out Vector3 aimPoint))
            {
                return;
            }

            _bodyStateMachine?.CmdSetAim(yaw, pitch);
            GetComponent<HumanoidIkController>()?.SetCombatAimPoint(aimPoint);

            Quaternion lookRotation = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = Quaternion.Slerp(Rotation, lookRotation, Time.deltaTime * _combatRotationLerpMultiplier);
        }

        /// <summary>
        /// Smooth combat yaw used by predicted movement (same rate as <see cref="RotatePlayerToCombatAim"/>).
        /// </summary>
        public void ApplyCombatAimYaw(float aimYaw, float deltaTime)
        {
            Quaternion lookRotation = Quaternion.Euler(0f, aimYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, deltaTime * _combatRotationLerpMultiplier);
        }

        /// <summary>
        /// Resolves combat aim from the camera mouse ray (3D hit or fallback distance).
        /// Body yaw uses the planar component; pitch is elevation to the aim point.
        /// </summary>
        public bool TryGetCombatAim(out float yaw, out float pitch, out Vector3 aimPoint)
        {
            yaw = 0f;
            pitch = 0f;
            aimPoint = Position;

            if (_camera == null)
            {
                _camera = SubSystems.Get<CameraSubSystem>().PlayerCamera;
            }

            if (_camera == null || Mouse.current == null)
            {
                return false;
            }

            Camera cam = _camera.GetComponent<Camera>();
            if (cam == null)
            {
                return false;
            }

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            const float maxDistance = 100f;
            const float fallbackDistance = 20f;

            if (!TryResolveAimPoint(ray, maxDistance, fallbackDistance, out aimPoint))
            {
                return false;
            }

            Vector3 eye = Position + Vector3.up * 1.5f;
            Vector3 toAim = aimPoint - eye;
            if (toAim.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            Vector3 flat = toAim;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f)
            {
                flat = Vector3.ProjectOnPlane(ray.direction, Vector3.up);
                if (flat.sqrMagnitude < 0.01f)
                {
                    return false;
                }
            }

            yaw = Quaternion.LookRotation(flat).eulerAngles.y;
            float flatMag = flat.magnitude;
            pitch = Mathf.Clamp(Mathf.Atan2(toAim.y, flatMag) * Mathf.Rad2Deg, -60f, 60f);
            return true;
        }

        private bool TryResolveAimPoint(Ray ray, float maxDistance, float fallbackDistance, out Vector3 aimPoint)
        {
            aimPoint = ray.GetPoint(fallbackDistance);
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return true;
            }

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform != null && hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                aimPoint = hit.point;
                return true;
            }

            return true;
        }

        /// <summary>
        /// Used by <see cref="HumanoidPredictedMovement"/> when it owns locomotion ticks.
        /// </summary>
        public void PublishPredictedLocomotionVelocity(float velX, float velZ, float turn = 0f)
        {
            OnLocomotionVelocityChanged?.Invoke(velX, velZ, turn);
            OnSpeedChanged(new Vector2(velX, velZ).magnitude);
        }

        protected virtual float FilterSpeed()
        {
            return _isRunning ? RunAnimatorValue : WalkAnimatorValue;
        }

        /// <summary>
        /// Toggles your movement between run/walk
        /// </summary>
        protected void HandleToggleRun(InputAction.CallbackContext context)
        {
            _isRunning = !_isRunning;
        }

        protected void OnSpeedChanged(float speed)
        {
            OnSpeedChangeEvent?.Invoke(speed);
        }
    }

}
