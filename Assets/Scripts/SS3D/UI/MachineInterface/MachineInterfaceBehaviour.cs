using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Transporting;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    public abstract class MachineInterfaceBehaviour : InteractionTargetNetworkBehaviour, IMachineInterfaceProvider, IMachineInterfaceClientBridge
    {
        private readonly HashSet<NetworkConnection> _viewers = new();

        public abstract string InterfaceId { get; }

        protected NetworkConnection LastControlConnection { get; private set; }

        NetworkObject IMachineInterfaceProvider.NetworkObject => NetworkObject;

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new OpenMachineInterfaceInteraction() };
        }

        public void SetControl(byte controlId, bool value)
        {
            CmdSetControl(controlId, value);
        }

        public void SetNumericControl(byte controlId, float delta)
        {
            CmdSetNumericControl(controlId, delta);
        }

        public void SetActionControl(byte controlId, int value)
        {
            CmdSetActionControl(controlId, value);
        }

        public void RequestClose()
        {
            CmdCloseInterface();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
        }

        public override void OnStopServer()
        {
            ServerManager.OnRemoteConnectionState -= HandleRemoteConnectionState;

            foreach (NetworkConnection viewer in _viewers)
            {
                if (viewer.IsValid)
                {
                    TargetCloseInterface(viewer);
                }
            }

            _viewers.Clear();
            ReleaseTickSubscription();
            base.OnStopServer();
        }

        [Server]
        public void ServerHandleOpenRequest(InteractionEvent interactionEvent)
        {
            if (!TryResolveViewerConnection(interactionEvent.Source, out NetworkConnection conn))
            {
                Debug.LogWarning("Could not resolve viewer connection for machine interface open request.", this);
                return;
            }

            if (!ValidateViewer(conn, interactionEvent))
            {
                return;
            }

            OpenInterfaceForViewer(conn);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdSetControl(byte controlId, bool value, NetworkConnection conn = null)
        {
            if (conn == null || !conn.IsValid || !_viewers.Contains(conn))
            {
                return;
            }

            LastControlConnection = conn;
            ApplyControl(controlId, value);
            LastControlConnection = null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdSetNumericControl(byte controlId, float delta, NetworkConnection conn = null)
        {
            if (conn == null || !conn.IsValid || !_viewers.Contains(conn))
            {
                return;
            }

            LastControlConnection = conn;
            ApplyNumericControl(controlId, delta);
            LastControlConnection = null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdSetActionControl(byte controlId, int value, NetworkConnection conn = null)
        {
            if (conn == null || !conn.IsValid || !_viewers.Contains(conn))
            {
                return;
            }

            LastControlConnection = conn;
            ApplyActionControl(controlId, value);
            LastControlConnection = null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdCloseInterface(NetworkConnection conn = null)
        {
            if (conn == null || !conn.IsValid)
            {
                return;
            }

            _viewers.Remove(conn);
            TargetCloseInterface(conn);

            if (_viewers.Count == 0)
            {
                ReleaseTickSubscription();
            }
        }

        protected abstract bool ApplyControl(byte controlId, bool value);

        protected virtual bool ApplyNumericControl(byte controlId, float delta)
        {
            return false;
        }

        protected virtual bool ApplyActionControl(byte controlId, int value)
        {
            return false;
        }

        [Server]
        protected void RefreshAllViewers()
        {
            if (_viewers.Count == 0)
            {
                return;
            }

            foreach (NetworkConnection viewer in _viewers)
            {
                if (viewer.IsValid)
                {
                    SendRefreshToViewer(viewer);
                }
            }
        }

        [Server]
        protected virtual void OnInterfaceTick()
        {
            if (_viewers.Count == 0)
            {
                return;
            }

            RefreshAllViewers();
        }

        protected override void OnDestroyed()
        {
            if (IsServer && ServerManager != null)
            {
                ServerManager.OnRemoteConnectionState -= HandleRemoteConnectionState;
            }

            ReleaseTickSubscription();
            base.OnDestroyed();
        }

        [Server]
        protected void OpenInterfaceForViewer(NetworkConnection conn)
        {
            _viewers.Add(conn);
            EnsureTickSubscription();
            SendOpenToViewer(conn);
        }

        protected abstract void SendOpenToViewer(NetworkConnection conn);

        protected abstract void SendRefreshToViewer(NetworkConnection conn);

        protected void DispatchClientOpen<TSnapshot>(TSnapshot snapshot)
            where TSnapshot : struct
        {
            if (!SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                return;
            }

            MachineInterfaceNetworkRegistry.DispatchOpen(subsystem, snapshot, this);
        }

        protected void DispatchClientRefresh<TSnapshot>(TSnapshot snapshot)
            where TSnapshot : struct
        {
            if (!SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                return;
            }

            MachineInterfaceNetworkRegistry.DispatchRefresh(subsystem, snapshot);
        }

        private static bool TryResolveViewerConnection(IInteractionSource source, out NetworkConnection conn)
        {
            conn = null;

            if (source is Hand hand && hand.HandsController != null && hand.HandsController.Owner.IsValid)
            {
                conn = hand.HandsController.Owner;
                return true;
            }

            if (source is not Component component)
            {
                return false;
            }

            Hands hands = component.GetComponentInParent<Hands>();
            if (hands != null && hands.Owner.IsValid)
            {
                conn = hands.Owner;
                return true;
            }

            InteractionController interactionController = component.GetComponentInParent<InteractionController>();
            if (interactionController != null && interactionController.Owner.IsValid)
            {
                conn = interactionController.Owner;
                return true;
            }

            Transform current = component.transform;
            while (current != null)
            {
                if (current.TryGetComponent(out NetworkObject networkObject) && networkObject.Owner.IsValid)
                {
                    conn = networkObject.Owner;
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private void HandleRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != RemoteConnectionState.Stopped)
            {
                return;
            }

            if (_viewers.Remove(conn) && _viewers.Count == 0)
            {
                ReleaseTickSubscription();
            }
        }

        [TargetRpc(RunLocally = true)]
        private void TargetCloseInterface(NetworkConnection conn)
        {
            if (!SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                return;
            }

            subsystem.CloseFromNetwork(this);
        }

        private bool ValidateViewer(NetworkConnection conn, InteractionEvent interactionEvent)
        {
            if (conn == null || !conn.IsValid)
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        private void EnsureTickSubscription()
        {
            if (!IsServer || _viewers.Count != 1)
            {
                return;
            }

            if (SubSystems.TryGet(out global::SS3D.Systems.Electricity.ElectricitySubSystem electricitySubSystem))
            {
                electricitySubSystem.OnTick += OnInterfaceTick;
            }
        }

        private void ReleaseTickSubscription()
        {
            if (!IsServer)
            {
                return;
            }

            if (SubSystems.TryGet(out global::SS3D.Systems.Electricity.ElectricitySubSystem electricitySubSystem))
            {
                electricitySubSystem.OnTick -= OnInterfaceTick;
            }
        }
    }
}
