using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Rounds;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Authorizers;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.MapEditor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Map editor systems views pause, step and Room air (Documents/architecture/systems/map-editor-systems-views.md, Pause and step, Room air): the admin gate
    /// on pause and step, the pause that lifts when its admin closes the editor, leaves or the round ends, one step being
    /// exactly one tick, the bar that shows the controls only to admins, and Room air's chunk resync and pressure steps.
    /// </summary>
    public sealed class SystemsViewSimulationTests
    {
        private const int Admin = 1;
        private const int OtherAdmin = 2;

        private Simulation _atmos;
        private Simulation _power;
        private SystemsViewSimulationControl _control;

        [SetUp]
        public void SetUp()
        {
            _atmos = new Simulation();
            _power = new Simulation();
            _control = new SystemsViewSimulationControl(() => new ISystemsViewSimulation[] { _atmos, _power });
            ResetClock();
        }

        [TearDown]
        public void TearDown() => ResetClock();

        private static void ResetClock()
        {
            typeof(SimulationClock).GetMethod("ResetForNewSession", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
            typeof(WorldSimSuspension).GetMethod("ResetForNewSession", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
        }

        [Test]
        public void Gate_IsAdminOrHost_WithTheEditorAuthorized()
        {
            Assert.IsTrue(MapEditorPermissions.MayControlSimulation(authorized: true, isAdministratorOrHost: true));
            Assert.IsFalse(MapEditorPermissions.MayControlSimulation(authorized: true, isAdministratorOrHost: false),
                "A player the editor's authorizer lets in (a Builder, a sandbox player under the open-build authorizer) is not an admin");
            Assert.IsFalse(MapEditorPermissions.MayControlSimulation(authorized: false, isAdministratorOrHost: true),
                "Refused the editor, refused the pause");
        }

        [Test]
        public void Gate_OnAConnection_AdminsAndTheHostOnly_EvenWhenEveryoneMayOpenTheEditor()
        {
            NetworkConnection host = new() { ClientId = 0 };
            NetworkConnection admin = new() { ClientId = 1 };
            NetworkConnection player = new() { ClientId = 2 };
            Func<NetworkConnection, bool> roles = MapEditorPermissions.HostOrAdministrator;
            MapEditorPermissions.HostOrAdministrator = conn => conn == host || conn == admin;
            // Lets every player in, as the open-build authorizer (-sandbox, mppm's editor_for_clients) does.
            MapEditorPermissions.SetAuthorizer(new Authorizer(_ => true));
            try
            {
                Assert.IsTrue(MapEditorPermissions.MayControlSimulation(host));
                Assert.IsTrue(MapEditorPermissions.MayControlSimulation(admin));
                Assert.IsFalse(MapEditorPermissions.MayControlSimulation(player), "In the editor, but not an admin");
                Assert.IsFalse(MapEditorPermissions.MayControlSimulation((NetworkConnection)null));

                MapEditorPermissions.SetAuthorizer(new Authorizer(conn => conn != admin));
                Assert.IsFalse(MapEditorPermissions.MayControlSimulation(admin), "An admin the authorizer refuses is refused");
            }
            finally
            {
                MapEditorPermissions.HostOrAdministrator = roles;
                MapEditorPermissions.SetAuthorizer(null);
            }
        }

        [Test]
        public void Hub_RefusesAndToastsANonAdmin_TellsEveryEditorOfAPause_AndReleasesOnDisconnect()
        {
            Hub hub = new(_atmos, _power);
            hub.Server.EditorOpenChanged(hub.Admin, open: true);
            hub.Server.EditorOpenChanged(hub.Player, open: true);
            CollectionAssert.AreEqual(new[] { "1:running:may", "2:running:no" }, hub.Sent, "Opening the editor tells the state and the right");

            hub.Sent.Clear();
            Assert.AreEqual(SystemsViewSimulationResult.Refused, hub.Server.Request(hub.Player, SystemsViewSimulationAction.Pause));
            CollectionAssert.AreEqual(new[] { "2:" + MapEditorSystemsViewServer.RefusedMessage }, hub.Toasts);
            CollectionAssert.AreEqual(new[] { "2:running:no" }, hub.Sent);
            Assert.IsFalse(_atmos.SimulationPaused);

            hub.Sent.Clear();
            Assert.AreEqual(SystemsViewSimulationResult.Paused, hub.Server.Request(hub.Admin, SystemsViewSimulationAction.Pause));
            CollectionAssert.AreEqual(new[] { "1:paused:may", "2:paused:no" }, hub.Sent, "Every editor user hears of the pause");

            hub.Sent.Clear();
            hub.Editing.Remove(hub.Admin);
            hub.Server.EditorOpenChanged(hub.Admin, open: false);
            Assert.IsFalse(_atmos.SimulationPaused || _power.SimulationPaused, "The pausing admin left: both resume");
            CollectionAssert.AreEqual(new[] { "2:running:no" }, hub.Sent);
        }

        [Test]
        public void Hub_ResumesWhenTheRoundStops_OrTheServerStops()
        {
            Hub hub = new(_atmos, _power);
            hub.Server.Request(hub.Admin, SystemsViewSimulationAction.Pause);
            hub.Server.RoundStateChanged(RoundState.Ongoing);
            Assert.IsTrue(_atmos.SimulationPaused, "A running round keeps the pause");
            hub.Server.RoundStateChanged(RoundState.Ending);
            Assert.IsFalse(_atmos.SimulationPaused || _power.SimulationPaused);
            Assert.AreEqual("2:running:no", hub.Sent.Last());

            hub.Server.Request(hub.Admin, SystemsViewSimulationAction.Pause);
            hub.Server.ServerStopped();
            Assert.IsFalse(_atmos.SimulationPaused || _power.SimulationPaused, "Nothing stays paused into the next session");
        }

        [Test]
        public void Hub_ARestoreOrWorldSuspension_LiftsThePause_AndSaysSo()
        {
            Hub hub = new(_atmos, _power);
            hub.Server.Request(hub.Admin, SystemsViewSimulationAction.Pause);
            hub.Sent.Clear();

            // A station template restore rebuilds atmos, which comes back unpaused (AtmosSubSystem.DisposeSimulation).
            _atmos.SimulationPaused = false;
            hub.Server.Update(0f);
            Assert.IsFalse(_power.SimulationPaused, "Electricity is never left paused alone");
            Assert.IsFalse(hub.Server.Pause.IsPaused);
            CollectionAssert.AreEqual(new[] { "1:running:may", "2:running:no" }, hub.Sent, "The editors stop saying PAUSED");

            hub.Server.Request(hub.Admin, SystemsViewSimulationAction.Pause);
            using (WorldSimSuspension.Begin("test"))
                hub.Server.Update(1f);
            Assert.IsFalse(hub.Server.Pause.IsPaused || _atmos.SimulationPaused || _power.SimulationPaused, "A map load lifts the pause");
        }

        [Test]
        public void Refused_ChangesNothing()
        {
            foreach (SystemsViewSimulationAction action in new[] { SystemsViewSimulationAction.Pause, SystemsViewSimulationAction.Step, SystemsViewSimulationAction.Resume })
                Assert.AreEqual(SystemsViewSimulationResult.Refused, _control.Apply(Admin, mayControl: false, action), action.ToString());

            Assert.IsFalse(_control.IsPaused);
            Assert.IsFalse(_atmos.SimulationPaused || _power.SimulationPaused);
            Assert.AreEqual(0, _atmos.Steps + _power.Steps);

            _control.Apply(Admin, mayControl: true, SystemsViewSimulationAction.Pause);
            Assert.AreEqual(SystemsViewSimulationResult.Refused, _control.Apply(OtherAdmin, mayControl: false, SystemsViewSimulationAction.Resume),
                "A non-admin can't lift an admin's pause either");
            Assert.IsTrue(_atmos.SimulationPaused && _power.SimulationPaused);
        }

        [Test]
        public void Pause_HoldsBothServerChannels_AndAStepRunsEachExactlyOnce()
        {
            Assert.AreEqual(SystemsViewSimulationResult.Unchanged, _control.Apply(Admin, true, SystemsViewSimulationAction.Step),
                "Running simulations need no step");
            Assert.AreEqual(0, _atmos.Steps);

            Assert.AreEqual(SystemsViewSimulationResult.Paused, _control.Apply(Admin, true, SystemsViewSimulationAction.Pause));
            Assert.IsTrue(_atmos.SimulationPaused && _power.SimulationPaused);
            Assert.AreEqual(Admin, _control.PausedBy);

            Assert.AreEqual(SystemsViewSimulationResult.Stepped, _control.Apply(Admin, true, SystemsViewSimulationAction.Step));
            Assert.AreEqual(1, _atmos.Steps);
            Assert.AreEqual(1, _power.Steps);
            Assert.IsTrue(_atmos.SimulationPaused, "A step leaves them paused");

            Assert.AreEqual(SystemsViewSimulationResult.Resumed, _control.Apply(Admin, true, SystemsViewSimulationAction.Resume));
            Assert.IsFalse(_atmos.SimulationPaused || _power.SimulationPaused);
            Assert.AreEqual(SystemsViewSimulationResult.Unchanged, _control.Apply(Admin, true, SystemsViewSimulationAction.Resume));
        }

        [Test]
        public void ClosingTheEditorOrLeaving_ResumesOnlyThePausingAdminsPause()
        {
            _control.Apply(Admin, true, SystemsViewSimulationAction.Pause);
            Assert.IsFalse(_control.Release(OtherAdmin), "Another editor closing leaves the pause");
            Assert.IsTrue(_atmos.SimulationPaused);

            // The hub releases on an editor close (RpcSetEditorOpen) and on a disconnect (OnRemoteConnectionState) alike.
            Assert.IsTrue(_control.Release(Admin));
            Assert.IsFalse(_control.IsPaused);
            Assert.IsFalse(_atmos.SimulationPaused || _power.SimulationPaused);

            _control.Apply(Admin, true, SystemsViewSimulationAction.Pause);
            _control.Apply(OtherAdmin, true, SystemsViewSimulationAction.Pause);
            Assert.AreEqual(OtherAdmin, _control.PausedBy, "Pausing again hands the pause to the newer admin's editor");
            Assert.IsFalse(_control.Release(Admin));
            Assert.IsTrue(_control.Release(OtherAdmin));
        }

        [Test]
        public void RoundEnd_ResumesWhoeverPaused_ButNeverAPauseItDidntMake()
        {
            _control.Apply(Admin, true, SystemsViewSimulationAction.Pause);
            Assert.IsTrue(_control.ResumeAll());
            Assert.IsFalse(_atmos.SimulationPaused || _power.SimulationPaused);

            // The F1 atmos debug pause is the debug panel's, not the editor's.
            _atmos.SimulationPaused = true;
            Assert.IsFalse(_control.ResumeAll());
            Assert.IsFalse(_control.Release(Admin));
            Assert.IsTrue(_atmos.SimulationPaused);
        }

        [Test]
        public void AnF1PauseFirst_StaysWhenTheAdminPauseLifts()
        {
            _atmos.SimulationPaused = true;
            Assert.AreEqual(SystemsViewSimulationResult.Paused, _control.Apply(Admin, true, SystemsViewSimulationAction.Pause));
            Assert.IsFalse(_control.Reconcile(), "Both paused: nothing to lift");
            Assert.AreEqual(SystemsViewSimulationResult.Resumed, _control.Apply(Admin, true, SystemsViewSimulationAction.Resume));
            Assert.IsTrue(_atmos.SimulationPaused, "The F1 pause is the debug panel's, not the editor's");
            Assert.IsFalse(_power.SimulationPaused);
        }

        [Test]
        public void AnF1ResumeUnderTheAdminPause_LiftsTheWholePause()
        {
            _control.Apply(Admin, true, SystemsViewSimulationAction.Pause);
            Assert.IsFalse(_control.Reconcile());
            _atmos.SimulationPaused = false;
            Assert.IsTrue(_control.Reconcile(), "Atmos runs again under the pause: the editors must stop saying PAUSED");
            Assert.IsFalse(_control.IsPaused);
            Assert.IsFalse(_power.SimulationPaused, "and electricity isn't left paused alone");
            Assert.IsFalse(_control.Reconcile());
        }

        [Test]
        public void ChannelStep_IsExactlyOneElectricityTick_WhilePaused()
        {
            SimulationChannel channel = SimulationClock.GetOrCreate("electricity", 0.2f, pausedByWorldSuspension: true);
            int fired = 0;
            channel.Subscribe(() => fired++);
            channel.Paused = true;

            SimulationClock.Advance(5d);
            Assert.AreEqual(0, fired, "Paused, time builds up nothing");
            long ticks = channel.TickCount;

            Assert.IsTrue(channel.Step());
            Assert.AreEqual(1, fired);
            Assert.AreEqual(ticks + 1, channel.TickCount, "The step counts as a tick, so rates read across it (an SMES's)");
            SimulationClock.Advance(5d);
            Assert.AreEqual(1, fired, "And it leaves the channel paused");

            using (WorldSimSuspension.Begin("test"))
                Assert.IsFalse(channel.Step(), "Nothing steps while a map load holds the world");
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void SimBar_ShowsPauseAndStepOnlyToAdmins_RoomAirOnlyOnAtmos_AndPausedToEveryone()
        {
            MapEditorViewModel vm = new();
            vm.EnsureLayerDefaults();
            // Buttons only react to submit events inside a live panel, so host the bar in a window.
            EditorWindow window = ScriptableObject.CreateInstance<EditorWindow>();
            window.Show();
            try
            {
                RunSimBar(vm, window.rootVisualElement);
            }
            finally { window.Close(); }
        }

        private static void RunSimBar(MapEditorViewModel vm, VisualElement column)
        {
            MapEditorSystemsSimBar bar = new();
            bar.Build(column);
            List<SystemsViewSimulationAction> asked = new();
            bar.SimulationRequested += asked.Add;

            bar.Refresh(vm);
            Assert.AreEqual(DisplayStyle.None, Display(column, "systems-sim"), "Hidden with no view on and nothing paused");

            vm.SetSystemsView(SystemsViewKind.Atmos);
            bar.Refresh(vm);
            Assert.AreEqual(DisplayStyle.Flex, Display(column, "systems-sim"));
            Assert.AreEqual(DisplayStyle.Flex, Display(column, "systems-room-air"));
            Assert.AreEqual(DisplayStyle.None, Display(column, "systems-sim-pause"), "Not an admin: no pause");
            Assert.AreEqual(DisplayStyle.None, Display(column, "systems-sim-step"));
            Assert.AreEqual(DisplayStyle.None, Display(column, "systems-sim-paused"));

            vm.SetSystemsSimulation(paused: true, mayControl: false);
            bar.Refresh(vm);
            Assert.AreEqual(DisplayStyle.Flex, Display(column, "systems-sim-paused"), "Everyone in the editor sees the pause");
            Assert.AreEqual(DisplayStyle.None, Display(column, "systems-sim-pause"));

            vm.SetSystemsView(SystemsViewKind.Power);
            vm.SetSystemsSimulation(paused: false, mayControl: true);
            bar.Refresh(vm);
            Assert.AreEqual(DisplayStyle.None, Display(column, "systems-room-air"), "Room air is the Atmos view's");
            Assert.AreEqual(DisplayStyle.Flex, Display(column, "systems-sim-pause"));
            Assert.AreEqual("Pause", column.Q<Button>("systems-sim-pause").text);
            Assert.IsFalse(column.Q<Button>("systems-sim-step").enabledSelf, "Step only while paused");

            vm.SetSystemsSimulation(paused: true, mayControl: true);
            bar.Refresh(vm);
            Assert.AreEqual("Resume", column.Q<Button>("systems-sim-pause").text);
            Assert.IsTrue(column.Q<Button>("systems-sim-step").enabledSelf);
            Click(column.Q<Button>("systems-sim-pause"));
            Click(column.Q<Button>("systems-sim-step"));
            CollectionAssert.AreEqual(new[] { SystemsViewSimulationAction.Resume, SystemsViewSimulationAction.Step }, asked);

            vm.SetSystemsView(SystemsViewKind.Off);
            bar.Refresh(vm);
            Assert.AreEqual(DisplayStyle.Flex, Display(column, "systems-sim"), "Paused, the bar stays up with no view on");
        }

        [Test]
        public void ChunkResync_SendsEveryChunkOnce_AFewAtATime_TakingTurns_UntilEnded()
        {
            AtmosChunkResync resync = new();
            Vector2Int[] chunks = Enumerable.Range(0, 5).Select(i => new Vector2Int(i, 0)).ToArray();
            resync.Begin(7, chunks);
            resync.Begin(3, chunks.Take(2));
            List<(int, Vector2Int)> sent = new();

            Assert.AreEqual(4, resync.Drain(4, (client, chunk) => sent.Add((client, chunk))));
            CollectionAssert.AreEqual(new[] { 3, 7, 3, 7 }, sent.Select(s => s.Item1), "Clients take turns");
            Assert.IsFalse(resync.IsPending(3), "A client whose station is all sent is done");

            resync.End(7);
            Assert.IsFalse(resync.HasWork);
            Assert.AreEqual(0, resync.Drain(4, (client, chunk) => sent.Add((client, chunk))), "Room air off: nothing more");

            resync.Begin(7, chunks);
            sent.Clear();
            while (resync.HasWork)
                resync.Drain(2, (client, chunk) => sent.Add((client, chunk)));
            CollectionAssert.AreEqual(chunks, sent.Select(s => s.Item2), "Turning it on again sends the whole station again, in order");

            resync.Begin(7, chunks);
            Assert.IsFalse(resync.HasWork, "Another 'on' after the station went out sends nothing: only off then on does");
            resync.End(7);
            resync.Begin(7, chunks);
            resync.Begin(7, chunks);
            Assert.AreEqual(5, resync.Drain(10, (client, chunk) => { }), "An 'on' while it runs doesn't queue the station twice");

            resync.Begin(3, chunks);
            resync.Restart();
            Assert.IsFalse(resync.HasWork, "A restore drops the old station's chunks");
            CollectionAssert.AreEquivalent(new[] { 3, 7 }, resync.TakeRestarts(), "and sends the new one to everyone who had Room air on");
        }

        [Test]
        public void RoomAir_TintsSimulatedFloorsByPressureStep_NotSpaceOrWalls()
        {
            Assert.AreEqual(0, AtmosRoomAirPalette.LevelOf(0f));
            Assert.AreEqual(0, AtmosRoomAirPalette.LevelOf(float.NaN));
            Assert.AreEqual(AtmosRoomAirPalette.Levels - 1, AtmosRoomAirPalette.LevelOf(1000f));
            int normal = AtmosRoomAirPalette.LevelOf(101.3f);
            Assert.AreEqual(normal, AtmosRoomAirPalette.LevelOf(102f), "A small drift is no redraw");
            Assert.Less(AtmosRoomAirPalette.LevelOf(60f), normal);
            Color green = AtmosRoomAirPalette.ColorOfLevel(normal);
            Assert.Greater(green.g, green.r, "Normal air reads green");
            Color red = AtmosRoomAirPalette.ColorOfLevel(0);
            Assert.Greater(red.r, red.g, "No air reads red");

            AtmosRenderContext.Snapshot snapshot = new()
            {
                AtlasBounds = new Vector4(10, 20, 4, 2),
                PressureData = new[] { 101.3f, 50f, 0f, 300f, 101.3f, 101.3f, 101.3f, 101.3f },
                MaskData = new[]
                {
                    AtmosCellVisualEncoding.MaskSimulated, AtmosCellVisualEncoding.MaskSimulated, AtmosCellVisualEncoding.MaskVacuum,
                    AtmosCellVisualEncoding.MaskSimulated, AtmosCellVisualEncoding.MaskBlocked, AtmosCellVisualEncoding.MaskEmpty,
                    AtmosCellVisualEncoding.MaskSimulated, AtmosCellVisualEncoding.MaskSimulated,
                },
                Valid = true,
            };
            Assert.AreEqual(normal, MapEditorRoomAirOverlay.LevelAt(snapshot, new Vector2Int(10, 20)));
            Assert.AreEqual(AtmosRoomAirPalette.LevelOf(50f), MapEditorRoomAirOverlay.LevelAt(snapshot, new Vector2Int(11, 20)));
            Assert.AreEqual(MapEditorRoomAirOverlay.NotDrawn, MapEditorRoomAirOverlay.LevelAt(snapshot, new Vector2Int(12, 20)), "Space");
            Assert.AreEqual(AtmosRoomAirPalette.Levels - 1, MapEditorRoomAirOverlay.LevelAt(snapshot, new Vector2Int(13, 20)));
            Assert.AreEqual(MapEditorRoomAirOverlay.NotDrawn, MapEditorRoomAirOverlay.LevelAt(snapshot, new Vector2Int(10, 21)), "A wall");
            Assert.AreEqual(MapEditorRoomAirOverlay.NotDrawn, MapEditorRoomAirOverlay.LevelAt(snapshot, new Vector2Int(11, 21)), "Nothing simulated");
            Assert.AreEqual(MapEditorRoomAirOverlay.NotDrawn, MapEditorRoomAirOverlay.LevelAt(snapshot, new Vector2Int(9, 20)), "Outside the atlas");

            byte[] levels = Enumerable.Repeat(MapEditorRoomAirOverlay.NotDrawn, 256).ToArray();
            levels[0] = (byte)normal;
            levels[17] = 0;
            Mesh mesh = MapEditorRoomAirOverlay.BuildChunkMesh(levels);
            Assert.AreEqual(8, mesh.vertexCount, "One square per tinted tile");
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        [Test]
        public void TheAtmosLegend_ExplainsRoomAir()
        {
            Assert.IsTrue(SystemsViewLegend.EntriesFor(SystemsViewKind.Atmos).Any(e => e.Text == AtmosRoomAirPalette.LegendText));
        }

        private static DisplayStyle Display(VisualElement root, string name) => root.Q(name).style.display.value;

        private static void Click(Button button)
        {
            using NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }

        private sealed class Authorizer : IMapEditorAuthorizer
        {
            private readonly Func<NetworkConnection, bool> _allow;
            public Authorizer(Func<NetworkConnection, bool> allow) => _allow = allow;
            public bool TryAuthorize(NetworkConnection conn) => conn != null && _allow(conn);
        }

        /// <summary>The hub's server half with an admin (1) and a non-admin (2), both editing; what it sends, as text.</summary>
        private sealed class Hub
        {
            public readonly NetworkConnection Admin = new() { ClientId = 1 };
            public readonly NetworkConnection Player = new() { ClientId = 2 };
            public readonly HashSet<NetworkConnection> Editing = new();
            public readonly List<string> Sent = new();
            public readonly List<string> Toasts = new();
            public readonly MapEditorSystemsViewServer Server;

            public Hub(params ISystemsViewSimulation[] simulations)
            {
                Editing.Add(Admin);
                Editing.Add(Player);
                Server = new MapEditorSystemsViewServer(Editing.Contains, conn => conn == Admin, () => new[] { Admin, Player },
                    (conn, paused, may) => Sent.Add($"{conn.ClientId}:{(paused ? "paused" : "running")}:{(may ? "may" : "no")}"),
                    (conn, text) => Toasts.Add($"{conn.ClientId}:{text}"), () => simulations);
            }
        }

        private sealed class Simulation : ISystemsViewSimulation
        {
            public bool SimulationPaused { get; set; }
            public int Steps;
            public void StepOnce() => Steps++;
        }
    }
}
