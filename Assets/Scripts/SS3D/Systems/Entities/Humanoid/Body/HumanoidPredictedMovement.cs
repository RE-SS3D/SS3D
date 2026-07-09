using FishNet;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inputs;
using SS3D.Systems.Screens;
using UnityEngine;
using UnityEngine.InputSystem;
using Actor = SS3D.Core.Behaviours.Actor;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Server-authoritative movement using FishNet prediction/reconcile.
    /// Integrates with HumanoidBodyStateMachine capabilities (milestone 0.0.8).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(HumanoidBodyStateMachine))]
    public class HumanoidPredictedMovement : NetworkActor
    {
        public struct MoveData : IReplicateData
        {
            public float Horizontal;
            public float Vertical;
            public bool IsRunning;
            public float AimYaw;

            private uint _tick;
            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;
        }

        public struct ReconcileData : IReconcileData
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public ReconcileData(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
                _tick = 0;
            }

            private uint _tick;
            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;
        }

        [SerializeField] private HumanoidLivingController _livingController;
        [SerializeField] private HumanoidBodyStateMachine _bodyStateMachine;
        [SerializeField] private FeetController _feetController;
        [SerializeField] private float _movementSpeed = 5f;
        [SerializeField] private float _runMultiplier = 1.6f;

        private CharacterController _characterController;
        private Actor _camera;
        private Controls.MovementActions _movementControls;
        private InputSubSystem _inputSystem;
        private bool _subscribed;

        protected override void OnAwake()
        {
            base.OnAwake();
            _characterController = GetComponent<CharacterController>();
            _bodyStateMachine ??= GetComponent<HumanoidBodyStateMachine>();
            _livingController ??= GetComponent<HumanoidLivingController>();
            _feetController ??= GetComponent<FeetController>();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (IsOwner)
            {
                InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
                _characterController.enabled = true;
            }
            else if (IsServer)
            {
                InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
            }
            else
            {
                _characterController.enabled = false;
            }
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            if (InstanceFinder.TimeManager != null)
            {
                InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
            }
        }

        public override void OnOwnershipClient(FishNet.Connection.NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            if (IsOwner && !_subscribed)
            {
                SubscribeInput();
            }
            else if (!IsOwner && _subscribed)
            {
                UnsubscribeInput();
            }
        }

        private void SubscribeInput()
        {
            _inputSystem = SubSystems.Get<InputSubSystem>();
            _camera = SubSystems.Get<CameraSubSystem>().PlayerCamera;
            _movementControls = _inputSystem.Inputs.Movement;
            _inputSystem.ToggleActionMap(_movementControls, true);
            _subscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (_inputSystem == null) return;
            _inputSystem.ToggleActionMap(_movementControls, false);
            _subscribed = false;
        }

        private void TimeManager_OnTick()
        {
            if (IsOwner)
            {
                Reconciliation(default, false);
                CheckInput(out MoveData md);
                Move(md, false);
            }

            if (IsServer)
            {
                Move(default, true);
                ReconcileData rd = new(transform.position, transform.rotation);
                Reconciliation(rd, true);
            }
        }

        private void CheckInput(out MoveData md)
        {
            md = default;
            BodyCapabilities caps = _bodyStateMachine.Capabilities;
            if (!caps.CanMove)
            {
                return;
            }

            Vector2 input = _movementControls.Movement.ReadValue<Vector2>();
            if (input.sqrMagnitude < 0.01f)
            {
                return;
            }

            bool isRunning = caps.CanRun && Keyboard.current != null && Keyboard.current.leftShift.isPressed;
            float aimYaw = 0f;
            if (_bodyStateMachine.CombatMode == HumanoidCombatMode.Combat && _camera != null)
            {
                Vector3 mouseWorld = GetMouseWorldPosition();
                Vector3 lookDir = mouseWorld - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    aimYaw = Quaternion.LookRotation(lookDir).eulerAngles.y;
                    _bodyStateMachine.CmdSetAimYaw(aimYaw);
                }
            }

            md = new MoveData
            {
                Horizontal = input.x,
                Vertical = input.y,
                IsRunning = isRunning,
                AimYaw = aimYaw,
            };
        }

        [Replicate]
        private void Move(MoveData md, bool asServer, Channel channel = Channel.Unreliable, bool replaying = false)
        {
            BodyCapabilities caps = _bodyStateMachine.Capabilities;
            if (!caps.CanMove)
            {
                return;
            }

            _characterController.Move(Physics.gravity * (float)InstanceFinder.TimeManager.TickDelta);

            if (md.Horizontal == 0f && md.Vertical == 0f)
            {
                _bodyStateMachine.SetLocomotionSpeed(0f);
                _bodyStateMachine.SetLocomotionMode(LocomotionMode.Idle);
                return;
            }

            Vector3 moveDirection = GetCameraRelativeDirection(md.Horizontal, md.Vertical);
            float speedFactor = _feetController != null ? _feetController.FeetHealthFactor : 1f;
            float speed = _movementSpeed * speedFactor * (md.IsRunning ? _runMultiplier : 1f);

            _characterController.Move(moveDirection * speed * (float)InstanceFinder.TimeManager.TickDelta);

            if (caps.CanRotate)
            {
                if (_bodyStateMachine.CombatMode == HumanoidCombatMode.Combat)
                {
                    transform.rotation = Quaternion.Euler(0f, md.AimYaw, 0f);
                }
                else
                {
                    transform.rotation = Quaternion.LookRotation(moveDirection);
                }
            }

            float animSpeed = md.IsRunning ? 1f : 0.3f;
            _bodyStateMachine.SetLocomotionSpeed(animSpeed);
            _bodyStateMachine.SetLocomotionMode(md.IsRunning ? LocomotionMode.Run : LocomotionMode.Walk);
        }

        [Reconcile]
        private void Reconciliation(ReconcileData rd, bool asServer, Channel channel = Channel.Unreliable)
        {
            transform.position = rd.Position;
            transform.rotation = rd.Rotation;
        }

        private Vector3 GetCameraRelativeDirection(float horizontal, float vertical)
        {
            if (_camera == null)
            {
                return new Vector3(horizontal, 0f, vertical).normalized;
            }

            return (
                vertical * Vector3.Cross(_camera.Right, Vector3.up).normalized +
                horizontal * Vector3.Cross(Vector3.up, _camera.Forward).normalized
            ).normalized;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (_camera == null || Mouse.current == null)
            {
                return transform.position + transform.forward;
            }

            Ray ray = _camera.GetComponent<Camera>().ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                return hit.point;
            }

            return ray.origin + ray.direction * 10f;
        }
    }
}
