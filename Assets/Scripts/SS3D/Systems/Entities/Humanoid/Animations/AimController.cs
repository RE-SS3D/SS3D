using Coimbra;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Intents;
using SS3D.Interactions;
using SS3D.Systems.Camera;
using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Controller handling the logic to decide when a player should be aiming or not, also sync the aiming between clients.
    /// </summary>
    public class AimController : NetworkActor, IRecoilGiver
    {
        public event Action<bool, bool> OnAim;

        private const float AngularSpeed = 20f;

        private const float SphereRadius = 3f;

        [SerializeField]
        private Hands _hands;

        [SerializeField]
        private HumanoidMovementController _movementController;

        [SerializeField]
        private Rig _bodyAimRig;

        [SerializeField]
        private PositionController _positionController;

        [SerializeField]
        private Transform _aimTarget;

        private CameraActor _camera;

        private IntentController _intentController;

        [SyncVar(OnChange = nameof(SyncAimingToThrow))]
        private bool _isAimingToThrow;

        [SyncVar(OnChange = nameof(SyncAimingToShoot))]
        private bool _isAimingToShoot;

        private InputAction _primaryInteractionAction;

        private InputAction _shootingAction;

        private Vector3 _aimingTargetDesiredPosition = Vector3.zero;

        private Transform _aimingItem;

        /// <summary>
        /// Is the player aiming to throw ?
        /// </summary>
        public bool IsAimingToThrow => _isAimingToThrow;

        /// <summary>
        /// Is the player aiming to shoot with a gun ?
        /// </summary>
        public bool IsAimingToShoot => _isAimingToShoot;

        public void AddRecoil(float angle)
        {
            UpdateAimTargetPosition(angle, _aimingItem.position + (SphereRadius * Vector3.up));
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            _camera = Subsystems.Get<CameraSystem>().PlayerCamera;
            _intentController = GetComponent<IntentController>();

            if (!GetComponent<NetworkObject>().IsOwner)
            {
                enabled = false;
            }

            _primaryInteractionAction = Subsystems.Get<InputSystem>().Inputs.Interactions.RunPrimary;
            _shootingAction = Subsystems.Get<InputSystem>().Inputs.GunFire.Fire;

            Subsystems.Get<InputSystem>().Inputs.Interactions.AimThrow.performed += AimThrowOnPerformed;
            Subsystems.Get<InputSystem>().Inputs.Interactions.AimGun.performed += AimGunOnPerformed;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _hands.OnHandContentChanged += HandleHandContentChanged;
        }

        protected void Update()
        {
            if (_positionController.Movement == MovementType.Aiming)
            {
                _aimingItem = GetComponent<Hands>().SelectedHand.ItemHeld.transform;

                // Cast a ray from the mouse position into the scene
                Ray ray = _camera.Camera.ScreenPointToRay(Input.mousePosition);

                // Check if the ray hits any collider
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    _aimingTargetDesiredPosition = _aimingItem.position + ((hit.point - _aimingItem.position).normalized * SphereRadius);
                }

                UpdateAimTargetPosition((AngularSpeed / SphereRadius) * Mathf.Rad2Deg * Time.deltaTime, _aimingTargetDesiredPosition);
            }
        }

        [Server]
        private void HandleHandContentChanged(Hand hand, Item olditem, Item newitem, ContainerChangeType type)
        {
            // Stop aiming if the item in hand is removed.
            if (_isAimingToShoot && hand == _hands.SelectedHand && type == ContainerChangeType.Remove)
            {
                _isAimingToShoot = false;
            }

            if (_isAimingToThrow && hand == _hands.SelectedHand && type == ContainerChangeType.Remove)
            {
                _isAimingToThrow = false;
            }
        }

        [ServerOrClient]
        private void SyncAimingToThrow(bool wasAiming, bool isAiming, bool asServer)
        {
            _bodyAimRig.weight = isAiming ? 0.3f : 0f;
            OnAim?.Invoke(isAiming, true);
        }

        [ServerOrClient]
        private void SyncAimingToShoot(bool wasAiming, bool isAiming, bool asServer)
        {
            _bodyAimRig.weight = isAiming ? 0.3f : 0f;
            OnAim?.Invoke(isAiming, false);

            Debug.Log($"Aiming to shoot : {isAiming}");
            Subsystems.Get<InputSystem>().ToggleAction(_primaryInteractionAction, !isAiming);
            Subsystems.Get<InputSystem>().ToggleAction(_shootingAction, isAiming);
        }

        [Client]
        private void AimThrowOnPerformed(InputAction.CallbackContext obj)
        {
            if (!_hands.SelectedHand.Full)
            {
                return;
            }

            RpcAimToThrow(!IsAimingToThrow);
        }

        [Client]
        private void AimGunOnPerformed(InputAction.CallbackContext obj)
        {
            // To aim, the intent must be harmful, and a gun must be in hand.
            bool canAim = _intentController.Intent == IntentType.Harm && _hands.SelectedHand.Full && _hands.SelectedHand.ItemHeld.GameObject.HasComponent<Gun>();

            if (canAim)
            {
                RpcAimToShoot(!IsAimingToShoot);
            }
        }

        [ServerRpc]
        private void RpcAimToThrow(bool isAimingToThrow)
        {
            _isAimingToThrow = isAimingToThrow;
        }

        [ServerRpc]
        private void RpcAimToShoot(bool isAimingToShoot)
        {
            _isAimingToShoot = isAimingToShoot;
        }

        [Client]
        private void UpdateAimTargetPosition(float angle, Vector3 desiredPosition)
        {
            // Get current position relative to sphere center
            Vector3 fromCenter = _aimTarget.position - _aimingItem.position;
            fromCenter = fromCenter.normalized * SphereRadius; // Ensure it stays on the sphere

            // Get target position relative to sphere center
            Vector3 toTarget = desiredPosition - _aimingItem.position;
            toTarget = toTarget.normalized * SphereRadius; // Ensure it stays on the sphere

            // Compute the rotation axis (perpendicular to both vectors)
            Vector3 rotationAxis = Vector3.Cross(fromCenter, toTarget).normalized;

            // Compute the angle between the current position and the target
            float angleToTarget = Vector3.Angle(fromCenter, toTarget);

            if (angleToTarget > 0.01f)
            {
                // Rotate towards the target along the sphere
                float angleStep = Mathf.Min(angle, angleToTarget); // Avoid overshooting
                Quaternion rotation = Quaternion.AngleAxis(angleStep, rotationAxis);
                fromCenter = rotation * fromCenter;

                // Update position
                _aimTarget.position = _aimingItem.position + fromCenter;
            }
        }
    }
}
