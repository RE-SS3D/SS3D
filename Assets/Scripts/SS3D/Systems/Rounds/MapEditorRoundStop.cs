using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Comms;
using SS3D.Systems.Persistence;
using SS3D.Systems.Tile.MapEditor;

namespace SS3D.Systems.Rounds
{
    public enum MapStopChoice { Cancel, SaveAndStop, DiscardAndStop }

    /// <summary>
    /// Map edits happen during an Ongoing round, and stopping the round unloads the station
    /// (<see cref="UnloadStation"/>). This keeps unsaved edits from vanishing with it: an admin's
    /// Stop asks Save and stop / Discard / Cancel, and any other round end autosaves them to a
    /// separate "-autosave" template.
    /// </summary>
    public sealed partial class RoundSubSystem
    {
        public const string AutosaveSuffix = "-autosave";

        private sealed class PendingMapStop
        {
            public int Token;
            public long Revision;
            public string EditingMap;
        }

        private readonly Dictionary<NetworkConnection, PendingMapStop> _pendingMapStops = new();
        private int _nextMapStopToken;
        private bool _mapDiscardApproved;

        /// <summary>Client side: the pending Stop prompt for this admin (0 = none).</summary>
        public int MapStopToken { get; private set; }
        public string MapStopEditingMap { get; private set; }
        public string MapStopError { get; private set; }

        [Server]
        protected override void RequestPlayerRoundStop(NetworkConnection sender)
        {
            // With map persistence off there is nothing to save, so no prompt.
            if (IsOngoing && MapEditorPermissions.PersistenceAllowed
                && SubSystems.TryGet(out MapEditorSubSystem editor) && editor.HasUnsavedChanges)
            {
                PendingMapStop pending = new()
                {
                    Token = ++_nextMapStopToken,
                    Revision = editor.EditRevision,
                    EditingMap = editor.EditingTemplateName,
                };
                ShowMapStop(sender, pending, "");
                return;
            }
            RequestRoundEnd(RoundEndRequest.Admin);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdResolveMapStop(int token, MapStopChoice choice, string saveName, NetworkConnection sender = null)
        {
            if (!IsAdministrator(sender) || !_pendingMapStops.TryGetValue(sender, out PendingMapStop pending)
                || pending.Token != token) return;
            _pendingMapStops.Remove(sender);
            TargetMapStopPrompt(sender, 0, "", "");
            if (choice == MapStopChoice.Cancel || !IsOngoing || !SubSystems.TryGet(out MapEditorSubSystem editor)) return;
            if (pending.Revision != editor.EditRevision || pending.EditingMap != editor.EditingTemplateName)
            {
                // Someone edited, saved or loaded since the prompt opened: confirm against the current state.
                RequestPlayerRoundStop(sender);
                return;
            }
            if (choice == MapStopChoice.SaveAndStop)
            {
                // A known template is an explicit overwrite; a new map needs a fresh name.
                bool known = !string.IsNullOrEmpty(pending.EditingMap);
                string name = known ? pending.EditingMap : saveName?.Trim();
                PersistenceSubSystem persistence = SubSystems.Get<PersistenceSubSystem>();
                if (!known && (!ValidNewTemplateName(name) || persistence.ListStationTemplates().Contains(name)))
                {
                    ShowMapStop(sender, pending, "Choose a new map name (letters, numbers, spaces, - or _). Existing maps cannot be overwritten here.");
                    return;
                }
                bool saved;
                try { saved = persistence.SaveStationTemplate(name, overwrite: known); }
                catch (Exception exception)
                {
                    Log.Error(this, "Save before stop failed: {error}", Logs.ServerOnly, exception.Message);
                    saved = false;
                }
                if (!saved)
                {
                    ShowMapStop(sender, pending, "Save failed. The round is still running; retry or cancel.");
                    return;
                }
            }
            else if (choice == MapStopChoice.DiscardAndStop) _mapDiscardApproved = true;
            else return;

            RequestRoundEnd(RoundEndRequest.Admin);
        }

        private static bool ValidNewTemplateName(string name) => !string.IsNullOrWhiteSpace(name)
            && name.Length <= 80 && name.All(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_');

        private void ShowMapStop(NetworkConnection sender, PendingMapStop pending, string error)
        {
            _pendingMapStops[sender] = pending;
            TargetMapStopPrompt(sender, pending.Token, pending.EditingMap ?? "", error);
        }

        [TargetRpc]
        private void TargetMapStopPrompt(NetworkConnection sender, int token, string editingMap, string error)
        {
            MapStopToken = token;
            MapStopEditingMap = editingMap;
            MapStopError = error;
        }

        /// <summary>
        /// Runs right before the station is unloaded. Unsaved edits are kept in an autosave
        /// template unless an admin chose Discard; the original template is never overwritten.
        /// </summary>
        [Server]
        private void HandleUnsavedMapEditsBeforeUnload()
        {
            bool discard = _mapDiscardApproved;
            _mapDiscardApproved = false;
            ClearMapStopPrompts();
            if (!SubSystems.TryGet(out MapEditorSubSystem editor)) return;
            if (!discard && editor.HasUnsavedChanges && MapEditorPermissions.PersistenceAllowed) AutosaveMapEdits(editor);
            editor.ResetAuthoringState();
        }

        [Server]
        private void AutosaveMapEdits(MapEditorSubSystem editor)
        {
            string baseName = string.IsNullOrEmpty(editor.EditingTemplateName) ? "Untitled Map" : editor.EditingTemplateName;
            string name = baseName.EndsWith(AutosaveSuffix, StringComparison.Ordinal) ? baseName : baseName + AutosaveSuffix;
            bool saved;
            try { saved = SubSystems.Get<PersistenceSubSystem>().SaveStationTemplate(name, overwrite: true); }
            catch (Exception exception)
            {
                Log.Error(this, "Map edit autosave failed: {error}", Logs.ServerOnly, exception.Message);
                saved = false;
            }
            string notice = saved
                ? $"The round ended with unsaved map edits. They were saved as \"{name}\"."
                : "The round ended with unsaved map edits, and the autosave failed. Those edits are lost.";
            Log.Information(this, notice, Logs.ServerOnly);
            if (SubSystems.TryGet(out CommsSubSystem comms)) comms.PostOocSystem(OocChannel.Admin, notice);
        }

        [Server]
        private void ClearMapStopPrompts()
        {
            foreach (NetworkConnection connection in _pendingMapStops.Keys)
                if (connection.IsActive) TargetMapStopPrompt(connection, 0, "", "");
            _pendingMapStops.Clear();
        }

        public override void OnStopServer()
        {
            _pendingMapStops.Clear();
            _mapDiscardApproved = false;
            MapEditorPermissions.PersistenceAllowed = true;
            StopAutoStartLoop();
            StopServerLifetime();
            UnregisterBugReportBroadcasts();
            base.OnStopServer();
        }
    }
}
