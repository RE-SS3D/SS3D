using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Animations;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health;
using SS3D.Systems.Inputs;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Entities
{
    public sealed class PositionController : NetworkActor
    {
        public event Action<PositionType, float> OnChangedPosition;

        public event Action<MovementType> OnChangedMovement;

        public event Action<bool> OnDance;

        private const float MinFeetHealthFactorToStand = 0.5f;

        private const float RagdollRecoverTime = 1f;

        private const float RagdollBoneResetTime = 0.7f;

        private const float DefaultTransitionDuration = 0.2f;

        [SerializeField]
        private FeetController _feetController;

        [SerializeField]
        private AnimationClip _getUpFromFront;

        [SerializeField]
        private AnimationClip _getUpFromBack;

        [SerializeField]
        private Ragdoll _ragdoll;

        [SyncVar(OnChange = nameof(SyncPosition))]
        private PositionType _positionType;

        [SyncVar(OnChange = nameof(SyncMovement))]
        private MovementType _movement;

        [SyncVar]
        private MovementType _previousMovement;

        [SyncVar]
        private PositionType _previousPosition;

        [SyncVar(OnChange = nameof(SyncDance))]
        private bool _isDancing;

        [SyncVar]
        private bool _standingAbility = true;

        public PositionType PositionType => _positionType;

        public PositionType PreviousPositionType => _positionType;

        public MovementType Movement => _movement;

        public bool CanGrab => _standingAbility && _movement == MovementType.Normal && _positionType != PositionType.Sitting;

        public bool CanSit => _standingAbility;

        public bool CanRotateWhileAiming => Movement == MovementType.Aiming && PositionType != PositionType.Sitting && PositionType != PositionType.Proning;

        public AnimationClip GetRecoverFromRagdollClip()
        {
            return GetComponent<Ragdoll>().IsFacingDown ? _getUpFromFront : _getUpFromBack;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _feetController.OnFeetHealthChanged += OnFeetHealthChanged;
            _ragdoll.OnRagdoll += HandleRagdoll;
            _standingAbility = true;
            _positionType = PositionType.Standing;
            _movement = MovementType.Normal;
            _previousPosition = PositionType.Standing;
            _previousMovement = MovementType.Normal;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner)
            {
                return;
            }

            Subsystems.Get<InputSubSystem>().Inputs.Movement.ChangePosition.performed += HandleChangePosition;
            Subsystems.Get<InputSubSystem>().Inputs.Movement.Dance.performed += HandleDance;
            GetComponent<AimController>().OnAim += HandleAimChange;
        }

        [Client]
        public void TrySit()
        {
            if (_positionType == PositionType.Ragdoll)
            {
                return;
            }

            RpcChangePosition(PositionType.Sitting);
        }

        [Client]
        public void Prone()
        {
            if (_positionType == PositionType.Ragdoll)
            {
                return;
            }

            RpcChangePosition(PositionType.Proning);
        }

        [Client]
        public void TryCrouch()
        {
            if (_positionType == PositionType.Ragdoll)
            {
                return;
            }

            RpcChangePosition(_standingAbility ? PositionType.Crouching : PositionType.Proning);
        }

        [Client]
        public void TryToStandUp()
        {
            if (_positionType == PositionType.Ragdoll)
            {
                return;
            }

            RpcChangePosition(_standingAbility ? PositionType.Standing : PositionType.Proning);
        }

        [Client]
        public void TryToGetToPreviousPosition()
        {
            RpcRestorePreviousPosition();
        }

        [Client]
        public void ChangeGrab(bool grab)
        {
            RpcChangeMovement(grab ? MovementType.Dragging : MovementType.Normal);
        }

        private void HandleDance(InputAction.CallbackContext obj)
        {
            RpcDance();
        }

        [ServerRpc]
        private void RpcDance()
        {
            if (_standingAbility && _positionType == PositionType.Standing)
            {
                _isDancing = !_isDancing;
            }
        }

        private void SyncPosition(PositionType oldPosition, PositionType newPosition, bool asServer)
        {
            float duration = DefaultTransitionDuration;

            if (newPosition == PositionType.ResetBones)
            {
                duration = RagdollBoneResetTime;
            }
            else if (newPosition == PositionType.RagdollRecover)
            {
                duration = RagdollRecoverTime;
            }

            OnChangedPosition?.Invoke(newPosition, duration);
        }

        private void SyncMovement(MovementType oldPosition, MovementType newPosition, bool asServer)
        {
            OnChangedMovement?.Invoke(newPosition);
        }

        private void SyncDance(bool wasDancing, bool isDancing, bool asServer)
        {
            OnDance?.Invoke(isDancing);
        }

        [Server]
        private void HandleRagdoll(bool isOn)
        {
            if (isOn)
            {
                ChangePosition(PositionType.Ragdoll);
            }
            else if (_positionType == PositionType.Ragdoll)
            {
                RpcChangePosition(PositionType.ResetBones);
                Invoke(nameof(RagdollRecover), RagdollBoneResetTime);
                Invoke(nameof(TryToStandUp), RagdollRecoverTime + RagdollBoneResetTime);
            }
        }

        private void RagdollRecover()
        {
            RpcChangePosition(PositionType.RagdollRecover);
        }

        [ServerRpc]
        private void RpcChangeMovement(MovementType movement)
        {
            _previousMovement = _movement;
            _movement = movement;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcRestorePreviousPosition()
        {
            ChangePosition(_previousPosition);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcChangePosition(PositionType position)
        {
            ChangePosition(position);
        }

        [Server]
        private void ChangePosition(PositionType position)
        {
            _previousPosition = _positionType;
            _positionType = position;
        }

        /// <summary>
        /// Cycle through standing, crouching and crawling position
        /// </summary>
        [Client]
        private void HandleChangePosition(InputAction.CallbackContext obj)
        {
            switch (_positionType)
            {
                case PositionType.Proning:
                case PositionType.Sitting:
                {
                    TryToStandUp();
                    break;
                }

                case PositionType.Standing:
                {
                    TryCrouch();
                    break;
                }

                case PositionType.Crouching:
                {
                    Prone();
                    break;
                }
            }
        }

        [Client]
        private void HandleAimChange(bool isAiming, bool toThrow)
        {
            RpcChangeMovement(isAiming ? MovementType.Aiming : MovementType.Normal);
        }

        [Server]
        private void OnFeetHealthChanged(float feetHealth)
        {
            _standingAbility = feetHealth >= MinFeetHealthFactorToStand;

            if (!_standingAbility)
            {
                _ragdoll.KnockDown(2f);
            }
        }
    }
}
