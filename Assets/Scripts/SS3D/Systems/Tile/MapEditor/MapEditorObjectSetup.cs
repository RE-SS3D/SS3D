using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Persistence;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using System;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Object setup in the editor (Documents/architecture/2026-11_object-setup-persistence.md): who has the editor open,
    /// the in-world setup tracker, and the Dropper's copied setup.
    /// </summary>
    public sealed partial class MapEditorSubSystem
    {
        private MapEditorSetupTracker _setupTracker;
        private readonly MapEditorSetupCopy _setupCopy = new();

        /// <summary>Server only; null on a pure client.</summary>
        public MapEditorSetupTracker SetupTracker
        {
            get
            {
                if (NetworkObject == null || !IsServer)
                    return null;

                if (_setupTracker == null)
                {
                    _setupTracker = new MapEditorSetupTracker(_commandService);
                    _setupTracker.Recorded += clientId =>
                    {
                        if (ServerManager.Clients.TryGetValue(clientId, out NetworkConnection conn))
                            TargetSyncUndoState(conn, _commandService.UndoDepth, _commandService.RedoDepth, true);
                    };
                }

                return _setupTracker;
            }
        }

        /// <summary>Whether <paramref name="conn"/> has the editor open: the server knows everyone's, a client only its own.</summary>
        public bool IsEditing(NetworkConnection conn)
        {
            if (conn == null || !conn.IsValid || NetworkObject == null)
                return false;

            return IsServer ? SetupTracker.IsEditing(conn.ClientId) : _active && conn == LocalConnection;
        }

        /// <summary>Whether the player owning <paramref name="actor"/> (a body, a hand) has the editor open.</summary>
        public static bool IsEditorUser(GameObject actor)
        {
            NetworkObject owner = actor != null ? actor.GetComponentInParent<NetworkObject>() : null;
            return owner != null && SubSystems.TryGet(out MapEditorSubSystem editor) && editor.IsEditing(owner.Owner);
        }

        /// <summary>
        /// Server: wraps each step of a running interaction (<see cref="ServerInteractionSteps"/>), so a valve turned or a
        /// locker filled by someone with the editor open becomes their undo step. Interactions start a frame after their
        /// RPC, so this is where the change actually happens.
        /// </summary>
        private bool TrackInteractionStep(InteractionInstance instance, Func<bool> step)
        {
            NetworkConnection owner = instance.Owner;
            MapEditorSetupTracker tracker = SetupTracker;
            if (tracker == null || owner == null || !owner.IsValid || !tracker.IsEditing(owner.ClientId))
                return step();

            GameObject target = (instance.Event.Target as IGameObjectProvider)?.GameObject;
            GameObject source = (instance.Event.Source as IGameObjectProvider)?.GameObject;
            return tracker.RunStep(owner.ClientId, new[] { target, source }, step);
        }

        /// <summary>Client: tells the server the editor opened or closed, so it knows whose in-world changes to track.</summary>
        private void ReportEditorOpen(bool open)
        {
            if (NetworkObject != null && NetworkObject.IsSpawned && IsClient)
                RpcSetEditorOpen(open, LocalConnection);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcSetEditorOpen(bool open, NetworkConnection conn = null)
        {
            if (conn != null)
                SetEditing(conn.ClientId, open && MapEditorPermissions.TryAuthorize(conn));
        }

        /// <summary>Server: records who has the editor open; interaction steps are only wrapped while someone does.</summary>
        private void SetEditing(int clientId, bool open)
        {
            MapEditorSetupTracker tracker = SetupTracker;
            if (tracker == null)
                return;

            tracker.SetEditing(clientId, open);
            ServerInteractionSteps.Wrap = tracker.AnyEditing ? TrackInteractionStep : null;
        }

        /// <summary>Client: the Dropper copied a placed object; ask the server for its setup.</summary>
        private void RequestDropperSetup(string assetName, bool isItem, Vector3 position, Direction direction)
        {
            _setupCopy.Clear();
            RpcCaptureDropperSetup(assetName, isItem, position, direction, LocalConnection);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcCaptureDropperSetup(string assetName, bool isItem, Vector3 position, Direction direction,
            NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn) || !RpcInput.IsInStation(position))
                return;

            GameObject source = MapEditorSetupCopy.FindPlaced(_tileSystem?.CurrentMap, _tileSystem?.GetAsset(assetName), isItem, position, direction);
            SavedSetupEntry[] setup = ObjectSetup.Capture(source);
            if (setup.Length == 0)
                return;

            if (MapEditorSetupCopy.Chars(setup) > MapEditorSetupCopy.MaxCharsPerSubmit)
                TargetToastFor(conn, $"Copied {assetName}, but its contents are too big to copy.", 4f);
            else
                TargetDropperSetup(conn, assetName, setup);
        }

        [TargetRpc]
        private void TargetDropperSetup(NetworkConnection conn, string assetName, SavedSetupEntry[] setup)
        {
            // A palette pick since the click wins.
            if (_viewModel.SelectedEntry?.AssetName != assetName)
                return;

            _setupCopy.Hold(assetName, setup);
            ShowLocalToast($"Copied {assetName} with its setup");
        }
    }
}
