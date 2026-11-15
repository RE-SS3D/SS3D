using System;
using System.Collections.Generic;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Electricity;
using SS3D.Systems.Rounds;
using SS3D.Systems.Tile.MapEditor.SystemsView;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// The server's Phase 5 jobs for the systems views (Documents/architecture/2026-11_map-editor-systems-views.md Q6, Q2),
    /// kept out of <see cref="MapEditorSubSystem"/>, which owns only the RPCs (<c>MapEditorSystemsView.cs</c>):
    /// <list type="bullet">
    /// <item>the admin pause and step (<see cref="Pause"/>): the gate, who is told the state (every open editor, with whether
    /// it may pause), and the pause lifting when its admin's editor closes, they leave, the round stops, the server stops,
    /// a station template restore or world suspension runs, or something else unpaused a simulation under it;</item>
    /// <item>Room air's resync: every atmos chunk once to the client that turned it on, a few each poll.</item>
    /// </list>
    /// </summary>
    public sealed class MapEditorSystemsViewServer
    {
        public const string RefusedMessage = "Only admins can pause or step the simulation.";
        private const float ResyncSeconds = 0.25f;

        private readonly Func<NetworkConnection, bool> _isEditing;
        private readonly Func<NetworkConnection, bool> _mayControl;
        private readonly Func<IEnumerable<NetworkConnection>> _clients;
        private readonly Action<NetworkConnection, bool, bool> _sendState;
        private readonly Action<NetworkConnection, string> _toast;
        private float _nextResync;

        /// <param name="isEditing">Whether a connection has the editor open.</param>
        /// <param name="mayControl">The admin gate (<see cref="MapEditorPermissions.MayControlSimulation(NetworkConnection)"/>); asked only of editor users.</param>
        /// <param name="clients">Every connection on the server.</param>
        /// <param name="sendState">Tells one client: paused, and whether it may pause and step.</param>
        /// <param name="toast">Tells one client why it was refused.</param>
        /// <param name="simulations">What to pause and step; the server's atmos and electricity when null.</param>
        public MapEditorSystemsViewServer(Func<NetworkConnection, bool> isEditing, Func<NetworkConnection, bool> mayControl,
            Func<IEnumerable<NetworkConnection>> clients, Action<NetworkConnection, bool, bool> sendState,
            Action<NetworkConnection, string> toast, Func<IEnumerable<ISystemsViewSimulation>> simulations = null)
        {
            _isEditing = isEditing;
            _mayControl = mayControl;
            _clients = clients;
            _sendState = sendState;
            _toast = toast;
            Pause = new SystemsViewSimulationControl(simulations ?? Simulations);
        }

        /// <summary>The admin pause and step.</summary>
        public SystemsViewSimulationControl Pause { get; }

        /// <summary>The server's atmospherics and electricity, as far as their subsystems exist.</summary>
        public static IEnumerable<ISystemsViewSimulation> Simulations()
        {
            if (SubSystems.TryGet(out AtmosSubSystem atmos))
                yield return atmos;
            if (SubSystems.TryGet(out ElectricitySubSystem electricity))
                yield return electricity;
        }

        /// <summary>An editor user's Pause, Resume or Step. A refusal is toasted, and the client is told the state again.</summary>
        public SystemsViewSimulationResult Request(NetworkConnection conn, SystemsViewSimulationAction action)
        {
            if (conn == null)
                return SystemsViewSimulationResult.Refused;

            SystemsViewSimulationResult result = Pause.Apply(conn.ClientId, MayControl(conn), action);
            if (result == SystemsViewSimulationResult.Refused)
            {
                _toast(conn, RefusedMessage);
                SendState(conn);
            }
            else if (result is SystemsViewSimulationResult.Paused or SystemsViewSimulationResult.Resumed)
                Broadcast();

            return result;
        }

        /// <summary>The editor opened (the client is told the state and its rights), or closed or the client left (its Room air ends, and a pause it holds lifts).</summary>
        public void EditorOpenChanged(NetworkConnection conn, bool open)
        {
            if (conn == null)
                return;

            if (open)
            {
                SendState(conn);
                return;
            }

            SetRoomAir(conn, false);
            if (Pause.Release(conn.ClientId))
                Broadcast();
        }

        /// <summary>A round that stops running resumes the simulations, whoever paused them.</summary>
        public void RoundStateChanged(RoundState state)
        {
            if (state != RoundState.Ongoing && Pause.ResumeAll())
                Broadcast();
        }

        /// <summary>The server stops: nothing is left paused into the next session (the channels outlive it).</summary>
        public void ServerStopped() => Pause.ResumeAll();

        /// <summary>
        /// Every server frame: the pause lifts if a simulation runs again under it or a world suspension (a map load, clear
        /// or import) starts, and the editor users are told; Room air's resync sends its next chunks every 0.25 s.
        /// </summary>
        public void Update(float now)
        {
            if (Pause.IsPaused && (WorldSimSuspension.IsSuspended ? Pause.ResumeAll() : Pause.Reconcile()))
                Broadcast();

            if (now < _nextResync)
                return;

            _nextResync = now + ResyncSeconds;
            if (SubSystems.TryGet(out AtmosSubSystem atmos))
                atmos.SendChunkResync();
        }

        /// <summary>Room air on (already gated by the hub: editor open, authorized, Atmos view) or off for one client.</summary>
        public void SetRoomAir(NetworkConnection conn, bool on)
        {
            if (conn == null || !SubSystems.TryGet(out AtmosSubSystem atmos))
                return;

            if (on)
                atmos.BeginChunkResync(conn);
            else
                atmos.EndChunkResync(conn.ClientId);
        }

        private bool MayControl(NetworkConnection conn) => _isEditing(conn) && _mayControl(conn);

        private void SendState(NetworkConnection conn) => _sendState(conn, Pause.IsPaused, MayControl(conn));

        private void Broadcast()
        {
            foreach (NetworkConnection conn in _clients())
            {
                if (_isEditing(conn))
                    SendState(conn);
            }
        }
    }
}
