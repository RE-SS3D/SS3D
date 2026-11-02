using System.Linq;
using FishNet;
using SS3D.Systems.Entities;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Combat;
using UnityEngine.Rendering.Universal;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Rendering.URP;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.ScreenEffects;
using UnityEngine;
using UnityEngine.InputSystem;
using SS3D.Core;
using SS3D.Systems.Comms;
using SS3D.Systems.Rounds;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Commands;
using UnityEditor;
using UnityEngine.UIElements;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Sandbox server pilot (<c>2026-11_sandbox-server.md</c>): the host applies the sandbox profile in
    /// Play Mode, as <c>-sandbox</c> does at boot, then lets the server run two rounds on its own with
    /// nobody pressing Ready or Start.
    /// </summary>
    public static partial class LobbyLifecyclePilot
    {
        private const string SandboxMode = "sandbox";
        private const string RoundResetRegressionMode = "round-reset-regressions";

        [MenuItem("SS3D/Testing/Run Round Reset Regressions")]
        public static void RunRoundResetRegressions() => Start(RoundResetRegressionMode);

        private const string HealthRegressionMode = "health-regressions";

        // Runs only spawn, incapacitation, blackout/menu, ghost and respawn checks.
        public static void RunHealthRegressions() => Start(HealthRegressionMode);

        private const string SandboxScope = "Single-editor Host session with the sandbox profile applied in Play Mode "
            + "(no -sandbox flag in the editor). Two automatic round cycles; nobody presses Ready or Start. "
            + "Does not cover a remote client, non-admin sandbox players or a dedicated server build.";

        [MenuItem("SS3D/Testing/Run Sandbox Pilot")]
        public static void RunSandbox() => Start(SandboxMode);

        private static SS3D.Systems.BugReports.BugReportSubSystem BugReports() =>
            SubSystems.Get<SS3D.Systems.BugReports.BugReportSubSystem>();

        private static VisualElement BugDialog() =>
            SubSystems.Get<SS3D.UI.Shell.UiShellSubSystem>().Document.rootVisualElement.Q("bug-report");

        private static bool AutosaveExists() =>
            SubSystems.Get<SS3D.Systems.Persistence.PersistenceSubSystem>().StationTemplateExists(_validationStation + RoundSubSystem.AutosaveSuffix);

        private static bool StationEmpty() => SubSystems.Get<TileSubSystem>().CurrentMap.ChunkCount == 0;

        /// <summary>Places an Assistant spawn on a plenum tile the template left empty, so its presence proves the edit.</summary>
        private static void EditEmptySpawnTile()
        {
            TileSubSystem tiles = SubSystems.Get<TileSubSystem>();
            _mapEditSpawnPosition = tiles.CurrentMap.GetAllChunks()
                .SelectMany(chunk => chunk.GetAllTilePlacedObjects())
                .Where(tile => tile.Layer == TileLayer.Plenum)
                .Select(tile => tile.transform.position)
                .First(position => !tiles.SpawnPoints.HasAt(position));
            SubSystems.Get<MapEditorSubSystem>().SubmitCommands(new MapEditorCommandDto[] { new MapEditorCommandDto
            {
                Kind = MapEditorCommandKind.PlaceSpawnPoint, AssetName = "spawn:job:Assistant",
                Position = _mapEditSpawnPosition, Direction = Direction.North,
            } });
        }

        private static AtmosSimulation _previousAtmos;
        private static TileCoord _depletedAtmosCoord;
        private static GameObject[] _roundDecals;

        private static void SeedRoundResetRegressions()
        {
            AtmosSubSystem atmos = SubSystems.Get<AtmosSubSystem>();
            _previousAtmos = atmos.Simulation;
            bool found = false;
            _previousAtmos.ForEachCell((coord, _) =>
            {
                if (!found && _previousAtmos.DebugGetMoles(coord, AtmosConstants.Oxygen) > 0f)
                {
                    _depletedAtmosCoord = coord;
                    found = true;
                }
            });
            Require(found, "Station has a breathable cell for the reset regression");
            // Freeze diffusion so this cell remains depleted until the next restore.
            atmos.SimulationPaused = true;
            _previousAtmos.DebugAddMoles(_depletedAtmosCoord, AtmosConstants.Oxygen,
                -_previousAtmos.DebugGetMoles(_depletedAtmosCoord, AtmosConstants.Oxygen));
            Require(_previousAtmos.DebugGetMoles(_depletedAtmosCoord, AtmosConstants.Oxygen) == 0f,
                "Oxygen is depleted before round reset");

            DecalProjector[] before = Object.FindObjectsByType<DecalProjector>(FindObjectsSortMode.None);
            Transform body = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Local()).transform;
            BulletHoleDecalSpawner.Spawn(body.position, Vector3.up);
            BloodDecalSpawner.SpawnAtAnchor(body, 1f);
            _roundDecals = Object.FindObjectsByType<DecalProjector>(FindObjectsSortMode.None)
                .Except(before).Select(projector => projector.gameObject).ToArray();
            Require(_roundDecals.Length == 2, "Blood and bullet-hole surface decals exist before reset");
        }

        private static Human _corpse;
        private static int _resetHealthLines;
        private static int _bugLines;

        /// <summary>Lines of a jsonl file under the server's Data folder (LocalStorage layout).</summary>
        private static string[] JsonlLines(string name)
        {
            string path = SS3D.Data.Paths.GetPath(SS3D.Data.GamePaths.Data, true) + name + ".jsonl";
            return System.IO.File.Exists(path) ? System.IO.File.ReadAllLines(path) : System.Array.Empty<string>();
        }

        private static T LastJsonl<T>(string name) => JsonUtility.FromJson<T>(JsonlLines(name).Last());

        private static RespawnPrompt Prompt() => SubSystems.Get<EntitySubSystem>().GetComponent<RespawnPrompt>();

        private static bool LocalEntityIsGhost() =>
            SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Local()) is { } entity
            && entity.GetComponent<HumanoidGhostController>() != null;

        private static Vector3 Flat(Vector3 position) => new(position.x, 0, position.z);

        private static ResetHealthEntry _firstReset;

        /// <summary>The reset-health line lands just after Stopped; wait for this reset's line.</summary>
        private static bool ResetLogged() =>
            JsonlLines(SS3D.Data.Persistence.PersistencePaths.ResetHealth).Length > _resetHealthLines + (_firstReset != null ? 1 : 0);

        /// <summary>The reset wrote its health line; nothing from the station is left; the second reset matches the first.</summary>
        private static void VerifyResetHealth(int resets)
        {
            Require(JsonlLines(SS3D.Data.Persistence.PersistencePaths.ResetHealth).Length == _resetHealthLines + resets,
                $"reset-health.jsonl has a line for reset {resets}");
            ResetHealthEntry entry = LastJsonl<ResetHealthEntry>(SS3D.Data.Persistence.PersistencePaths.ResetHealth);
            Timeline.Add($"reset {resets}: {JsonUtility.ToJson(entry)}");
            Require(entry.roundId == Rounds().RoundId && entry.players == 1, "Health line carries round ID and player count");
            Require(entry.tileObjects == 0 && entry.items == 0, "No tile objects or items survive the unload");
            if (resets == 1)
            {
                _firstReset = entry;
                return;
            }

            Require(entry.networkObjects == _firstReset.networkObjects,
                $"Spawned network objects match the first reset ({_firstReset.networkObjects} → {entry.networkObjects})");
        }

        private static string SidebarSub() => Shell()?.Q<Label>(className: "lobby-shell__countdown-sub")?.text;

        private static void VerifyAtmosReset()
        {
            AtmosSubSystem atmos = SubSystems.Get<AtmosSubSystem>();
            Require(!ReferenceEquals(_previousAtmos, atmos.Simulation), "New round owns a fresh atmos simulation");
            _depletedAtmosCoord.MapId = SubSystems.Get<TileSubSystem>().CurrentMap.MapId;
            Require(atmos.Simulation.DebugGetMoles(_depletedAtmosCoord, AtmosConstants.Oxygen) > 0f,
                "Fresh station restores oxygen to the depleted cell");
            Require(!atmos.SimulationPaused, "New round resumes atmos simulation");
        }

        private static Step[] BuildRoundResetSteps()
        {
            Step[] sandbox = BuildSandboxSteps();
            return sandbox.Take(4).Concat(new Step[]
            {
                new Step
                {
                    Name = "Deplete oxygen, stamp decals, and stop the round",
                    Enter = () =>
                    {
                        SeedRoundResetRegressions();
                        Rounds().ChangeRoundStateMessageStubBroadcast(new SS3D.Systems.Rounds.Messages.ChangeRoundStateMessage(false));
                    },
                    Until = () => Rounds().CurrentRoundState == RoundState.Stopped && StationEmpty() && ResetLogged(),
                    Verify = () => Require(_roundDecals.All(decal => decal == null), "Both surface decals are destroyed on Stop"),
                },
                new Step
                {
                    Name = "Restore the same station for a second round",
                    Enter = () => Rounds().ChangeRoundStateMessageStubBroadcast(new SS3D.Systems.Rounds.Messages.ChangeRoundStateMessage(true)),
                    Timeout = 180,
                    Until = () => Rounds().CurrentRoundState == RoundState.Ongoing && Spawned(),
                    Verify = VerifyAtmosReset,
                },
            }).ToArray();
        }

        private static Step[] BuildSandboxSteps() => new Step[]
        {
            new Step
            {
                Name = "Lobby ready on host boot",
                Timeout = 180,
                Until = () => Online() && Local() != null && Rounds() != null && Visible(Shell())
                    && Rounds().CurrentRoundState == RoundState.Stopped,
                Verify = () =>
                {
                    Require(!Rounds().AutoStartEnabled && Rounds().AutoStartSecondsLeft == 0, "A normal server does not start rounds on its own");
                    CreateValidationStation(valid: true);
                    _resetHealthLines = JsonlLines(SS3D.Data.Persistence.PersistencePaths.ResetHealth).Length;
                    _bugLines = JsonlLines(SS3D.Data.Persistence.PersistencePaths.BugReports).Length;
                },
            },
            new Step
            {
                Name = "Apply the sandbox profile with a two-minute first round",
                Enter = () =>
                {
                    ServerSettings settings = SandboxProfile.Apply(Rounds().SettingsForServer);
                    settings.Map = _validationStation;
                    settings.RoundLengthMinutes = 2;
                    settings.RespawnCooldownSeconds = 8;
                    Rounds().ApplyServerSettingsForTesting(settings);
                },
                Until = () => Rounds().AutoStartSecondsLeft > 0 && SidebarSub() == "Starting In",
                Verify = () =>
                {
                    Require(!IsReady(), "Countdown started without anyone pressing Ready");
                    Require(!MapEditorPermissions.PersistenceAllowed, "Sandbox profile turns map persistence off");
                    Require(Rounds().Motd == SandboxProfile.Motd, "MOTD carries the sandbox rules");
                    Capture("sandbox-autostart-countdown");
                },
            },
            new Step
            {
                Name = "First round starts and spawns the unready host",
                Timeout = 180,
                Until = () => Rounds().CurrentRoundState == RoundState.Ongoing && Spawned(),
                Verify = () =>
                {
                    // Let the body finish starting on the client before it dies.
                    Settle(2);
                    Require(SubSystems.Get<SS3D.Systems.Gamemodes.GamemodeSubSystem>().CurrentGamemodeName == "Extended", "Sandbox rounds run Extended");
                    Require(Rounds().StationName == _validationStation, "Auto-start restored the selected station");
                },
            },
            new Step
            {
                Name = "Body finishes starting",
                Until = Settled,
            },
            new Step
            {
                Name = "Critical state drops the held weapon and blocks hand actions",
                Enter = () =>
                {
                    Human body = (Human)SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Local());
                    Hand hand = body.GetComponent<Hands>().SelectedHand;
                    GameObject weapon = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/Content/WorldObjects/Items/Weapons/M4.prefab"), body.transform.position, Quaternion.identity);
                    InstanceFinder.ServerManager.Spawn(weapon);
                    hand.Pickup(weapon.GetComponent<Item>());
                    Require(!hand.IsEmpty(), "Weapon held before critical state");
                    body.GetComponent<HumanHealthController>().TickHealth(
                        toxinIntake: HealthConstants.CriticalToxinConcentration + 5f);
                    Require(hand.IsEmpty(), "Critical state drops the held weapon");
                    Require(!hand.CanUseHand && !hand.CanContinueInteraction(), "Critical state blocks new and ongoing hand actions");
                    hand.Pickup(weapon.GetComponent<Item>());
                    Require(hand.IsEmpty(), "Server refuses pickup while critical");
                    Require(body.GetComponent<HumanHealthController>().Snapshot.MovementSpeedMultiplier == 0f,
                        "Critical state blocks health-driven movement");
                    body.GetComponent<HumanHealthController>().RestoreSystemicPools();
                    Require(hand.CanUseHand, "Treatment restores hand control");
                    InstanceFinder.ServerManager.Despawn(weapon);
                    SubSystems.Get<ScreenEffectsSubSystem>().SetDebugOverrideActive(true);
                    SubSystems.Get<ScreenEffectsSubSystem>().SetEffect(ScreenEffectType.Unconscious, 1f);
                    // Unconsciousness fades at 0.3/sec; wait for a fully opaque veil.
                    Settle(4);
                },
                Until = Settled,
            },
            new Step
            {
                Name = "Escape menu stays visible over the unconsciousness blackout",
                Enter = () => TapKey(Key.Escape),
                Until = () => Visible(Shell()),
                Verify = () =>
                {
                    Require(UiBackdropBlurContext.BlackoutIntensity > 0.99f, "World blackout is opaque");
                    Require(GameObject.Find("ScreenEffectsBlackout") == null, "No overlay canvas can cover the menu");
                    Capture("sandbox-blackout-menu");
                },
            },
            new Step
            {
                Name = "Close the menu and release the blackout",
                Enter = () =>
                {
                    TapKey(Key.Escape);
                    SubSystems.Get<ScreenEffectsSubSystem>().SetEffect(ScreenEffectType.Unconscious, 0f);
                    SubSystems.Get<ScreenEffectsSubSystem>().SetDebugOverrideActive(false);
                },
                Until = () => !Visible(Shell()),
            },
            new Step
            {
                Name = "Host dies and becomes a ghost with a respawn countdown",
                Enter = () =>
                {
                    _corpse = (Human)SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Local());
                    _corpse.Kill();
                },
                Until = () => LocalEntityIsGhost() && Prompt().IsShown && !VisionRenderContext.Enabled,
                Verify = () =>
                {
                    Require(!Prompt().CanRespawn, "Respawn button waits for the cooldown");
                    Require(!SubSystems.Get<EntitySubSystem>().TryRespawn(InstanceFinder.ClientManager.Connection, out string refusal)
                        && refusal.StartsWith("You can respawn in"), "Server refuses a respawn during the cooldown");
                    Capture("sandbox-ghost-respawn-prompt");
                },
            },
            new Step
            {
                Name = "Ghost hides the body HUD and releases hand hotkeys",
                Until = () => _corpse.GetComponent<Hands>() == null
                    && SubSystems.Get<SS3D.UI.MainHud.MainHudSubSystem>().GetComponent<UIDocument>()
                        .rootVisualElement.Q(className: "main-hud").resolvedStyle.display == DisplayStyle.None,
                Verify = () =>
                {
                    TapKey(Key.Q);
                    TapKey(Key.X);
                    Settle(0.5);
                },
            },
            new Step
            {
                Name = "Ghost hand hotkeys do not invoke destroyed components",
                Until = Settled,
                Verify = () => Capture("sandbox-ghost-no-body-hud"),
            },
            new Step
            {
                Name = "Ghost opens the map editor",
                Enter = () => TapKey(Key.B),
                Until = MapEditorActive,
            },
            new Step
            {
                Name = "Ghost edits the map",
                Enter = EditEmptySpawnTile,
                Until = () => UnsavedMapEdits() && MapEditSpawnPresent(),
                Verify = () => Capture("sandbox-ghost-editing"),
            },
            new Step
            {
                Name = "Ghost closes the map editor",
                Enter = () => TapKey(Key.B),
                Until = () => !MapEditorActive() && Settled(),
                Verify = () =>
                {
                    Require(LocalEntityIsGhost(), "Still a ghost after closing the editor");
                    Require(!VisionRenderContext.Enabled, "Ghost FOV stays disabled after map editor closes");
                },
            },
            new Step
            {
                Name = "Respawn button enables after the cooldown",
                Timeout = 20,
                Until = () => Prompt().CanRespawn,
            },
            new Step
            {
                Name = "Respawn gives a fresh body and removes the previous corpse",
                Enter = () => Click(Root().Q<Button>("respawn-prompt-button")),
                Until = () => Spawned() && !LocalEntityIsGhost() && !Prompt().IsShown,
                Verify = () =>
                {
                    Entity body = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Local());
                    Require(body is Human && body != _corpse, "Respawned into a new Human");
                    Require(SubSystems.Get<SS3D.UI.MainHud.MainHudSubSystem>().GetComponent<UIDocument>()
                        .rootVisualElement.Q(className: "main-hud").resolvedStyle.display != DisplayStyle.None,
                        "Body HUD returns on respawn");
                    Require(_corpse == null, "Sandbox removes the previous corpse after respawn");
                    Vector3 spawn = SubSystems.Get<TileSubSystem>().SpawnPoints.Records[0].Position;
                    Require(Vector3.Distance(Flat(body.transform.position), Flat(spawn)) < 1.5f, "New body stands at a spawn point");
                    Capture("sandbox-respawned");
                },
            },
            new Step
            {
                Name = "/bug in chat saves a report with round and state",
                Enter = () => SubSystems.Get<CommsSubSystem>().SendOoc(OocChannel.Ooc, "/bug sandbox pilot report"),
                Until = () => HostChatContains($"Bug report saved (round {Rounds().RoundId}). Thanks!"),
                Verify = () =>
                {
                    Require(JsonlLines(SS3D.Data.Persistence.PersistencePaths.BugReports).Length == _bugLines + 1, "bugs.jsonl gained one line");
                    BugReportEntry bug = LastJsonl<BugReportEntry>(SS3D.Data.Persistence.PersistencePaths.BugReports);
                    Require(bug.note == "sandbox pilot report" && bug.state == "alive" && bug.roundId == Rounds().RoundId
                        && bug.ckey == Local().Ckey, "Report carries note, alive state, round ID and ckey");
                    Require(!HostChatContains("/bug sandbox pilot report"), "The command is not echoed to OOC chat");
                },
            },
            new Step
            {
                Name = "F8 opens the bug report dialog with the moment frozen",
                Enter = () => BugReports().Open("pressed F8"),
                Until = () => BugReports().IsOpen,
                Verify = () =>
                {
                    VisualElement dialog = BugDialog();
                    Require(dialog.Q<Label>("capture-context").text.StartsWith($"ROUND #{Rounds().RoundId} · CAPTURED AT "),
                        "The dialog names the round and the moment");
                    Require(dialog.Q<Label>("context-badge").text == "IN ROUND · ALIVE", "Context badge reads in round, alive");
                    Require(dialog.Q<Button>("submit").text == "SAVE REPORT", "Own host saves the report locally");
                    Require(dialog.Q("include-map").style.display == DisplayStyle.Flex, "The map option appears in a local session");
                    Require(!dialog.Q<Button>("submit").enabledSelf, "Save waits for a summary");
                    dialog.Q<TextField>("summary").value = "Sandbox pilot dialog report";
                    dialog.Q<TextField>("details").value = "Both inputs must remain readable while reporting a bug.";
                    Require(dialog.Q<Button>("submit").enabledSelf, "A summary enables Save");
                    Capture("sandbox-bug-report-dialog");
                },
            },
            new Step
            {
                Name = "Saving writes a local bundle and shows where",
                Enter = () => BugReports().View.TrySubmit(),
                Until = () => BugReports().View.IsSavedScreen && BugReports().LastBundlePath != null,
                Verify = () =>
                {
                    string path = BugReports().LastBundlePath;
                    Require(System.IO.File.Exists(path), "The bundle exists");
                    using (System.IO.Compression.ZipArchive zip = new(System.IO.File.OpenRead(path), System.IO.Compression.ZipArchiveMode.Read))
                    {
                        string[] names = zip.Entries.Select(entry => entry.FullName).ToArray();
                        Require(names.Contains("report.json") && names.Contains("screenshot.png"), "Bundle has the report and screenshot");
                        Require(names.Any(name => name.StartsWith("map/")), "Bundle has the current map (the editor was used this round)");
                        Require(names.Any(name => name.StartsWith("logs/")), "Bundle has the player's logs");
                    }
                    Capture("sandbox-bug-report-saved");
                },
            },
            new Step
            {
                Name = "Close dismisses the saved screen",
                Enter = () => BugReports().Close(),
                Until = () => !BugReports().IsOpen,
                Verify = () => System.IO.File.Delete(BugReports().LastBundlePath),
            },
            new Step
            {
                Name = "One-minute warning",
                Timeout = 90,
                Until = () => HostChatContains("The round ends in 1 minute."),
                Verify = () =>
                {
                    Require(Rounds().CurrentRoundState == RoundState.Ongoing, "Warning arrives while the round runs");
                    SeedRoundResetRegressions();
                },
            },
            new Step
            {
                Name = "First round ends on its own and the station is wiped",
                Timeout = 90,
                Until = () => Rounds().CurrentRoundState == RoundState.Stopped && !Spawned() && StationEmpty() && ResetLogged(),
                Verify = () =>
                {
                    Require(!AutosaveExists(), "No round-end autosave with persistence off");
                    Require(UnityEngine.Object.FindObjectsByType<Human>(FindObjectsSortMode.None).Length == 0,
                        "No bodies or corpses survive the reset");
                    Require(_roundDecals.All(decal => decal == null), "Blood and bullet-hole decals are destroyed on Stop");
                    VerifyResetHealth(1);
                    ServerSettings settings = Rounds().SettingsForServer;
                    settings.RoundLengthMinutes = 1;
                    Rounds().ApplyServerSettingsForTesting(settings);
                },
            },
            new Step
            {
                Name = "Second countdown starts without Ready",
                Timeout = 30,
                Until = () => Rounds().AutoStartSecondsLeft > 0,
                Verify = () => Require(!IsReady(), "Ready flags are not remembered across rounds"),
            },
            new Step
            {
                Name = "Second round starts on a fresh station",
                Timeout = 180,
                Until = () => Rounds().CurrentRoundState == RoundState.Ongoing && Spawned(),
                Verify = () =>
                {
                    Require(!MapEditSpawnPresent(), "Last round's edit is gone from the fresh station");
                    Require(!UnsavedMapEdits(), "Editor history starts clean");
                    VerifyAtmosReset();
                },
            },
            new Step
            {
                Name = "Second round ends on its own",
                Timeout = 120,
                Until = () => Rounds().CurrentRoundState == RoundState.Stopped && !Spawned() && StationEmpty() && ResetLogged(),
                Verify = () =>
                {
                    VerifyResetHealth(2);
                    Capture("sandbox-second-reset");
                },
            },
        };
    }
}
