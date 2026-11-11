using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using UnityEngine;
using SS3D.Core;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// HV-grid solar producer. Output is peak kW × sun intensity × aim cosine (design electricity §2).
    /// </summary>
    public class SolarPanel : BasicElectricDevice, IPowerProducer, IInteractionTarget
    {
        [SerializeField]
        private float _peakPowerKw = 2f;

        [SerializeField]
        private Transform _aimVisual;

        [SyncVar(OnChange = nameof(OnAimChanged))]
        private Direction _aim = Direction.North;

        private Quaternion _restRotationNoYaw;
        private bool _hasRestPose;

        public Direction Aim => _aim;

        public float PeakPowerKw
        {
            get => _peakPowerKw;
            set => _peakPowerKw = Mathf.Max(0f, value);
        }

        public float PowerProduction
        {
            get
            {
                float intensity = SolarCycle.GetSunIntensity();
                float aimFactor = SolarCycle.ComputeAimFactor(_aim, SolarCycle.GetSunAzimuthDegrees());
                return _peakPowerKw * intensity * aimFactor;
            }
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            SceneRegistry<SolarPanel>.Register(this);
        }

        protected override void OnDisabled()
        {
            SceneRegistry<SolarPanel>.Unregister(this);
            base.OnDisabled();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            CaptureRestPose();
            ApplyAimVisual(_aim);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            CaptureRestPose();
            ApplyAimVisual(_aim);
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new RotateSolarPanelInteraction() };
        }

        /// <summary>Server / EditMode: set absolute aim direction and refresh visuals.</summary>
        public void SetAim(Direction aim)
        {
            if (NetworkObject != null && NetworkObject.IsSpawned && !IsServer)
            {
                return;
            }

            _aim = aim;
            ApplyAimVisual(_aim);
        }

        /// <summary>Server: advance aim by one 45° step (manual Rotate interaction).</summary>
        public void RotateAimStep()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned && !IsServer)
            {
                return;
            }

            SetAim(TileHelper.GetNextDir(_aim));
        }

        private void OnAimChanged(Direction oldValue, Direction newValue, bool asServer)
        {
            if (asServer)
            {
                return;
            }

            ApplyAimVisual(newValue);
        }

        private Transform GetAimVisual()
        {
            return _aimVisual != null ? _aimVisual : transform;
        }

        /// <summary>
        /// FBX armature bones often have non-identity rest pitch/roll. Forcing world
        /// <c>Euler(0, yaw, 0)</c> zeros that rest and tumbles the rig. Keep rest pitch/roll
        /// and only replace world-up yaw to match tile <see cref="Direction"/>.
        /// </summary>
        private void CaptureRestPose()
        {
            if (_hasRestPose)
            {
                return;
            }

            Transform visual = GetAimVisual();
            Quaternion rest = visual.rotation;
            Vector3 flatForward = Vector3.ProjectOnPlane(rest * Vector3.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 1e-6f)
            {
                flatForward = Vector3.ProjectOnPlane(rest * Vector3.right, Vector3.up);
            }

            float restYaw = flatForward.sqrMagnitude > 1e-6f
                ? Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg
                : 0f;

            _restRotationNoYaw = Quaternion.Inverse(Quaternion.AngleAxis(restYaw, Vector3.up)) * rest;
            _hasRestPose = true;
        }

        private void ApplyAimVisual(Direction aim)
        {
            CaptureRestPose();
            Transform visual = GetAimVisual();
            float yaw = TileHelper.GetRotationAngle(aim);
            visual.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * _restRotationNoYaw;
        }
    }
}
