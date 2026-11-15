using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>What an admin asks of the server's simulations from the systems views (creative-mode §4 "Live, and pausable").</summary>
    public enum SystemsViewSimulationAction : byte
    {
        Pause = 1,
        Resume = 2,
        Step = 3,
    }

    /// <summary>What <see cref="SystemsViewSimulationControl.Apply"/> did.</summary>
    public enum SystemsViewSimulationResult
    {
        /// <summary>The caller may not pause or step; nothing changed.</summary>
        Refused,
        /// <summary>Already as asked, or a step while running; nothing changed.</summary>
        Unchanged,
        Paused,
        Resumed,
        Stepped,
    }

    /// <summary>
    /// One server simulation the systems views can pause and step: atmospherics (<c>AtmosSubSystem</c>) and electricity
    /// (<c>ElectricitySubSystem</c>), each over its own server channel. Clients' own electricity channels are never touched
    /// (tech-debt P6.1): their visuals come from the server's SyncVars.
    /// </summary>
    public interface ISystemsViewSimulation
    {
        /// <summary>The server channel's pause. Time doesn't build up while it is set.</summary>
        bool SimulationPaused { get; set; }

        /// <summary>Server: runs exactly one tick of this simulation now, paused or not.</summary>
        void StepOnce();
    }

    /// <summary>
    /// Server: the admin pause and step of Documents/architecture/2026-11_map-editor-systems-views.md Q6, pure C#. The hub
    /// gates every request (<c>MapEditorPermissions.MayControlSimulation</c>: an admin or the host with the editor open,
    /// never a sandbox player) and passes the verdict in; this class remembers who paused, so the simulations resume when
    /// that admin closes the editor or leaves (<see cref="Release"/>) and when the round ends (<see cref="ResumeAll"/>).
    /// It holds only the simulations it paused itself: one already paused by something else (the F1 atmos debug pause)
    /// stays paused when this pause lifts. When a simulation runs again under it (F1's resume, a station template restore
    /// that rebuilds atmos), <see cref="Reconcile"/> lifts the whole pause, so the editor never says paused while one runs.
    /// </summary>
    public sealed class SystemsViewSimulationControl
    {
        public const int Nobody = -1;

        private readonly Func<IEnumerable<ISystemsViewSimulation>> _simulations;
        // The simulations this pause set Paused on; only these are resumed. Kept by reference, so one whose subsystem is
        // going away still resumes.
        private readonly List<ISystemsViewSimulation> _held = new();

        /// <param name="simulations">The simulations to pause and step, read at each call (subsystems come and go).</param>
        public SystemsViewSimulationControl(Func<IEnumerable<ISystemsViewSimulation>> simulations) =>
            _simulations = simulations ?? throw new ArgumentNullException(nameof(simulations));

        /// <summary>The client whose pause holds the simulations, or <see cref="Nobody"/>.</summary>
        public int PausedBy { get; private set; } = Nobody;

        public bool IsPaused => PausedBy != Nobody;

        /// <summary>
        /// Pauses, resumes or steps for <paramref name="clientId"/>. <paramref name="mayControl"/> is the hub's admin gate;
        /// false refuses every action. A pause while paused hands the pause to the new caller (its editor now holds it). A step
        /// runs one tick of every simulation and only while paused: running simulations need no step.
        /// </summary>
        public SystemsViewSimulationResult Apply(int clientId, bool mayControl, SystemsViewSimulationAction action)
        {
            if (!mayControl || clientId == Nobody)
                return SystemsViewSimulationResult.Refused;

            switch (action)
            {
                case SystemsViewSimulationAction.Pause:
                    bool wasPaused = IsPaused;
                    PausedBy = clientId;
                    foreach (ISystemsViewSimulation simulation in _simulations())
                    {
                        if (simulation == null || simulation.SimulationPaused)
                            continue;
                        simulation.SimulationPaused = true;
                        _held.Add(simulation);
                    }

                    return wasPaused ? SystemsViewSimulationResult.Unchanged : SystemsViewSimulationResult.Paused;
                case SystemsViewSimulationAction.Resume:
                    return Resume() ? SystemsViewSimulationResult.Resumed : SystemsViewSimulationResult.Unchanged;
                case SystemsViewSimulationAction.Step:
                    if (!IsPaused)
                        return SystemsViewSimulationResult.Unchanged;
                    foreach (ISystemsViewSimulation simulation in _simulations())
                        simulation?.StepOnce();
                    return SystemsViewSimulationResult.Stepped;
                default:
                    return SystemsViewSimulationResult.Unchanged;
            }
        }

        /// <summary>
        /// <paramref name="clientId"/> closed the editor or left: if its pause holds the simulations, they resume. Returns
        /// whether they did.
        /// </summary>
        public bool Release(int clientId) => IsPaused && PausedBy == clientId && Resume();

        /// <summary>The round ended or the server stops: whoever paused, the simulations resume. Returns whether they did.</summary>
        public bool ResumeAll() => IsPaused && Resume();

        /// <summary>
        /// Every server frame: while paused, a simulation that runs again under the pause (something else unpaused it)
        /// lifts the pause on the rest, so nothing is left paused alone and the editor stops saying paused. Returns whether
        /// the pause lifted, so the hub tells the editor users.
        /// </summary>
        public bool Reconcile()
        {
            if (!IsPaused)
                return false;

            foreach (ISystemsViewSimulation simulation in _held)
            {
                if (!simulation.SimulationPaused)
                    return Resume();
            }

            foreach (ISystemsViewSimulation simulation in _simulations())
            {
                if (simulation != null && !simulation.SimulationPaused)
                    return Resume();
            }

            return false;
        }

        private bool Resume()
        {
            if (!IsPaused)
                return false;

            PausedBy = Nobody;
            foreach (ISystemsViewSimulation simulation in _held)
                simulation.SimulationPaused = false;
            _held.Clear();
            return true;
        }
    }
}
