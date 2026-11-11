using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Systems.Tile;
using UnityEngine;
using SS3D.Core;
using System.Collections.Generic;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Diegetic auto-aimer for nearby <see cref="SolarPanel"/>s. Toggle on/off in-world — no machine UI.
    /// </summary>
    public class SolarTrackingBeacon : NetworkActor
    {
        [SerializeField]
        private float _radius = 8f;

        [SerializeField]
        private float _updateInterval = 0.2f;

        private float _nextUpdateTime;
        private bool _enabled;

        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(0f, value);
        }

        public bool IsTrackingEnabled => _enabled;

        // OnToggle is raised on the server only; subscribing in OnStartClient never ran on a dedicated server.
        public override void OnStartServer()
        {
            base.OnStartServer();
            GenericToggleInteractionTarget toggle = GetComponent<GenericToggleInteractionTarget>();
            if (toggle != null)
            {
                toggle.OnToggle += HandleToggle;
                _enabled = toggle.GetState();
            }
        }

        public override void OnStopServer()
        {
            if (TryGetComponent(out GenericToggleInteractionTarget toggle))
                toggle.OnToggle -= HandleToggle;

            base.OnStopServer();
        }

        private void Update()
        {
            if (!IsServer || !_enabled)
            {
                return;
            }

            if (Time.time < _nextUpdateTime)
            {
                return;
            }

            _nextUpdateTime = Time.time + _updateInterval;
            AimNearbyPanels();
        }

        /// <summary>
        /// Aims all <see cref="SolarPanel"/>s within <see cref="Radius"/> at the current sun azimuth.
        /// Public for EditMode coverage without a live toggle.
        /// </summary>
        public int AimNearbyPanels()
        {
            Direction aim = SolarCycle.GetNearestAimDirection(SolarCycle.GetSunAzimuthDegrees());
            float radiusSq = _radius * _radius;
            Vector3 origin = transform.position;
            int aimed = 0;

            IReadOnlyList<SolarPanel> panels = SceneRegistry<SolarPanel>.All;
            for (int i = 0; i < panels.Count; i++)
            {
                SolarPanel panel = panels[i];
                if (panel == null)
                {
                    continue;
                }

                if ((panel.transform.position - origin).sqrMagnitude > radiusSq)
                {
                    continue;
                }

                panel.SetAim(aim);
                aimed++;
            }

            return aimed;
        }

        [Server]
        private void HandleToggle(bool isEnabled)
        {
            _enabled = isEnabled;
            if (_enabled)
            {
                AimNearbyPanels();
            }
        }
    }
}
