using FishNet;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using SS3D.Core;
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
        /// <summary>World units/sec at full run (Speed animator param 1.0).</summary>
        [SerializeField] private float _movementSpeed = 5f;
        /// <summary>Matches HumanoidController walk animator value (0.3) so walk/run stay in sync.</summary>
        [SerializeField] private float _walkSpeedFactor = 0.3f;

        private CharacterController _characterController;
        private Actor _camera;
        private Controls.MovementActions _movementControls;
        private InputSubSystem _inputSystem;
        private bool _subscribed;
        private bool _tickSubscribed;
        private bool _networkStarted;

        protected override void OnAwake()
        {
            base.OnAwake();
            _characterController = GetComponent<CharacterController>();
            if (_bodyStateMachine == null)
            {
                _bodyStateMachine = GetComponent<HumanoidBodyStateMachine>();
            }

            if (_livingController == null)
            {
                _livingController = GetComponent<HumanoidLivingController>();
            }

            if (_feetController == null)
            {
                _feetController = GetComponent<FeetController>();
            }
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _networkStarted = true;
            TrySubscribeTick();
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            TrySubscribeTick();
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            UnsubscribeTick();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            UnsubscribeTick();
            if (_subscribed)
            {
                UnsubscribeInput();
            }
        }

        public override void OnOwnershipClient(FishNet.Connection.NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            if (!enabled)
            {
                return;
            }

            if (IsOwner && !_subscribed)
            {
                SubscribeInput();
            }
            else if (!IsOwner && _subscribed)
            {
                UnsubscribeInput();
            }

            TrySubscribeTick();
        }

        private void TrySubscribeTick()
        {
            if (!_networkStarted || !enabled || _tickSubscribed || InstanceFinder.TimeManager == null)
            {
                return;
            }

            if (IsOwner)
            {
                InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
                _characterController.enabled = true;
                _tickSubscribed = true;
            }
            else if (IsServer)
            {
                InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
                _tickSubscribed = true;
            }
            else
            {
                _characterController.enabled = false;
            }
        }

        private void UnsubscribeTick()
        {
            if (!_tickSubscribed)
            {
                return;
            }

            if (InstanceFinder.TimeManager != null)
            {
                InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
            }

            _tickSubscribed = false;
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
            if (!enabled)
            {
                return;
            }

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
            if (!caps.CanMove || !_subscribed)
            {
                return;
            }

            Vector2 input = _movementControls.Movement.ReadValue<Vector2>();
            if (input.sqrMagnitude < 0.01f)
            {
                return;
            }

            bool isRunning = caps.CanRun && _livingController != null && _livingController.IsRunning;
            float aimYaw = 0f;
            if (_bodyStateMachine.CombatMode.IsCombat() && _camera != null)
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

            float tickDelta = (float)InstanceFinder.TimeManager.TickDelta;
            _characterController.Move(tickDelta * Physics.gravity);

            if (md.Horizontal == 0f && md.Vertical == 0f)
            {
                _bodyStateMachine.SetLocomotionSpeed(0f);
                _bodyStateMachine.SetLocomotionMode(LocomotionMode.Idle);
                _livingController?.PublishPredictedLocomotionVelocity(0f, 0f);
                return;
            }

            Vector3 moveDirection = GetCameraRelativeDirection(md.Horizontal, md.Vertical);
            float speedFactor = _feetController != null ? _feetController.FeetHealthFactor : 1f;
            // Same mapping as HumanoidController: walk clamps to ~0.3 of run speed so feet match Speed blend.
            float gaitFactor = md.IsRunning ? 1f : _walkSpeedFactor;
            float speed = _movementSpeed * speedFactor * gaitFactor;
            float animSpeed = md.IsRunning ? 1f : _walkSpeedFactor;

            _characterController.Move(moveDirection * (tickDelta * speed));

            if (caps.CanRotate)
            {
                if (_bodyStateMachine.CombatMode.IsCombat())
                {
                    transform.rotation = Quaternion.Euler(0f, md.AimYaw, 0f);
                }
                else
                {
                    transform.rotation = Quaternion.LookRotation(moveDirection);
                }
            }

            _bodyStateMachine.SetLocomotionSpeed(animSpeed);
            _bodyStateMachine.SetLocomotionMode(md.IsRunning ? LocomotionMode.Run : LocomotionMode.Walk);
            if (_livingController != null)
            {
                // Local-space blend axes relative to facing after rotation applied this tick.
                Vector3 local = transform.InverseTransformDirection(moveDirection);
                float velX = local.x * animSpeed;
                float velZ = local.z * animSpeed;
                _livingController.PublishPredictedLocomotionVelocity(velX, velZ);
            }
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
