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
    /// health state on the server and opens the VSU-5R vitals screen for the user. The reading is
    /// fixed at the moment of the scan; scan again for a fresh one.
    /// </summary>
    public sealed class HealthScannerInterfaceController : MachineInterfaceBehaviour, IInteractionSourceExtension
    {
        private static readonly HealthScanInteraction ScanInteraction = new();

        [SerializeField]
        private string _modelLabel = HealthScanViewModel.DefaultModelLabel;

        [SerializeField]
        private string _deviceTitle = HealthScanViewModel.DefaultDeviceTitle;

        private readonly Dictionary<NetworkConnection, HealthScanSnapshot> _scans = new();

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

            _scans[conn] = BuildSnapshot(patient);
            ServerHandleOpenRequest(interactionEvent);
        }

        public override void OnStopServer()
        {
            _scans.Clear();
            base.OnStopServer();
        }

        protected override bool ApplyControl(byte controlId, bool value) => false;

        /// <summary>A scan is a still reading: no refresh while the screen is open.</summary>
        protected override void OnInterfaceTick()
        {
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

        private HealthScanSnapshot BuildSnapshot(HumanHealthController patient)
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
