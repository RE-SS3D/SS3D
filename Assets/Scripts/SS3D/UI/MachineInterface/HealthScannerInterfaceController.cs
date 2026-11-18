using FishNet.Connection;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Examine;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// The handheld health scanner (virology.md §5, live scan): used on a character, it reads their
    /// health state on the server and opens the VSU-5R vitals screen for the user. While the screen is
    /// open the reading follows the patient live, as long as they stay within <see cref="LiveRange"/> of
    /// the scanner; past that it freezes on the last reading until the next scan.
    /// </summary>
    public sealed class HealthScannerInterfaceController : MachineInterfaceBehaviour, IInteractionSourceExtension
    {
        private static readonly HealthScanInteraction ScanInteraction = new();

        [SerializeField]
        private string _modelLabel = HealthScanViewModel.DefaultModelLabel;

        [SerializeField]
        private string _deviceTitle = HealthScanViewModel.DefaultDeviceTitle;

        /// <summary>How far the patient may be from the scanner and still update the screen (metres).</summary>
        public const float LiveRange = 3f;

        /// <summary>Seconds between live updates: health ticks at 1 Hz, and hits land between ticks.</summary>
        private const float LiveInterval = 0.5f;

        private readonly Dictionary<NetworkConnection, HealthScanSnapshot> _scans = new();
        private readonly Dictionary<NetworkConnection, HumanHealthController> _patients = new();
        private float _nextLiveUpdate;

        public override string InterfaceId => MachineInterfaceIds.HealthScanner;

        /// <summary>The scanner opens by being used on a patient, never by clicking the scanner itself.</summary>
        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return System.Array.Empty<IInteraction>();
        }

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions, InteractionEvent context)
        {
            foreach (IInteractionTarget target in targets)
            {
                if (ScanInteraction.CanInteract(context.WithTarget(target)))
                {
                    interactions.Add(new InteractionEntry(target, ScanInteraction));
                }
            }
        }

        /// <summary>Reads <paramref name="patient"/> and shows the result to whoever used the scanner.</summary>
        [Server]
        public void ServerScan(InteractionEvent interactionEvent, HumanHealthController patient)
        {
            if (patient == null || !TryResolveViewerConnection(interactionEvent.Source, out NetworkConnection conn))
            {
                return;
            }

            _patients[conn] = patient;
            _scans[conn] = BuildSnapshot(patient, true);
            ServerHandleOpenRequest(interactionEvent);
        }

        public override void OnStopServer()
        {
            _scans.Clear();
            _patients.Clear();
            base.OnStopServer();
        }

        protected override bool ApplyControl(byte controlId, bool value) => false;

        /// <summary>Re-reads each viewer's patient and pushes it, every <see cref="LiveInterval"/>.</summary>
        protected override void OnInterfaceTick()
        {
            if (Time.time < _nextLiveUpdate)
            {
                return;
            }

            _nextLiveUpdate = Time.time + LiveInterval;
            var viewers = new List<NetworkConnection>(_patients.Keys);
            foreach (NetworkConnection conn in viewers)
            {
                HumanHealthController patient = _patients[conn];
                if (!conn.IsValid || patient == null)
                {
                    _patients.Remove(conn);
                    continue;
                }

                if (Vector3.Distance(patient.transform.position, transform.position) <= LiveRange)
                {
                    _scans[conn] = BuildSnapshot(patient, true);
                }
                else if (_scans.TryGetValue(conn, out HealthScanSnapshot last) && last.Live)
                {
                    _scans[conn] = WithLive(last, false);
                }
            }

            base.OnInterfaceTick();
        }

        private static HealthScanSnapshot WithLive(HealthScanSnapshot snapshot, bool live)
        {
            snapshot.Live = live;
            return snapshot;
        }

        protected override void SendOpenToViewer(NetworkConnection conn)
        {
            if (_scans.TryGetValue(conn, out HealthScanSnapshot snapshot))
            {
                TargetOpenInterface(conn, snapshot);
            }
        }

        protected override void SendRefreshToViewer(NetworkConnection conn)
        {
            if (_scans.TryGetValue(conn, out HealthScanSnapshot snapshot))
            {
                TargetRefreshInterface(conn, snapshot);
            }
        }

        [TargetRpc(RunLocally = true)]
        private void TargetOpenInterface(NetworkConnection conn, HealthScanSnapshot snapshot)
        {
            DispatchClientOpen(conn, snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, HealthScanSnapshot snapshot)
        {
            DispatchClientRefresh(conn, snapshot);
        }

        private HealthScanSnapshot BuildSnapshot(HumanHealthController patient, bool live)
        {
            float[] organs = new float[HealthScanSnapshotBuilder.OrganCount];
            for (int i = 0; i < organs.Length; i++)
            {
                organs[i] = patient.GetStoredOrganFunction((OrganType)i);
            }

            HealthScanSnapshot snapshot = HealthScanSnapshotBuilder.Build(
                patient.Snapshot,
                patient.DebugDetail,
                organs,
                ResolveSubjectName(patient));
            snapshot.MachineObjectId = NetworkObject != null ? NetworkObject.ObjectId : 0;
            snapshot.InterfaceId = InterfaceId;
            snapshot.ModelLabel = _modelLabel;
            snapshot.DeviceTitle = _deviceTitle;
            snapshot.Live = live;
            return snapshot;
        }

        /// <summary>The name the patient presents (their worn ID), as examine names them.</summary>
        private static string ResolveSubjectName(HumanHealthController patient)
        {
            Entity entity = patient.GetComponentInParent<Entity>();
            HumanInventory inventory = entity != null
                ? entity.GetComponentInChildren<HumanInventory>()
                : patient.GetComponent<HumanInventory>();
            return inventory != null ? PresentedIdentity.DisplayName(inventory) : string.Empty;
        }
    }
}
