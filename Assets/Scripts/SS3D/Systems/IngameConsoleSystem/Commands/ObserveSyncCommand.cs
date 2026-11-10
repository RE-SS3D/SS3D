using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Rendering.URP;
using SS3D.Systems.Electricity;
using SS3D.Systems.Entities;
using SS3D.Systems.Furniture;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Testing;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Client-local checks that another player's actions reached this process: the observer half of
    /// the observer-sync smoke scenario (Testing/multiplayer/scenarios/observer-sync*.txt). Each check
    /// returns a failed <see cref="CommandResult"/> while the replicated state isn't there, so the
    /// automation DSL's <c>wait_console</c> retries it until it holds and a plain <c>console</c> line fails the script.
    /// Door and light checks compare against a <c>baseline</c> taken before the other player acts,
    /// so they don't depend on the map's layout.
    /// <c>atmos</c> reports whether this process has a valid atmos GPU snapshot to render: the only
    /// process-local signal a headless client can check to confirm
    /// <see cref="SS3D.Systems.Atmospherics.Visualization.AtmosClientVisualizationBridge"/> applied a chunk patch
    /// (Documents/architecture/2026-07_atmos-client-visualization-sync.md).
    /// </summary>
    public class ObserveSyncCommand : Command
    {
        private const string AssertWord = "assert";

        private static int _baselineLitFixtures = -1;
        private static int _baselineOpenAirlocks = -1;

        private IReadOnlyList<Subcommand> _subcommands;

        public override string Name => "observesync";
        public override CommandCategory Category => CommandCategory.Test;
        public override string Description => "Check another player's actions replicated here (observer-sync smoke).";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Local;
        public override string Example => "held MedicalPatch";

        public override IReadOnlyList<Subcommand> Subcommands => _subcommands ??= new[]
        {
            new Subcommand("baseline", "Record lit fixtures and open airlocks before the other player acts.",
                (_, _) => Signal("baseline", CommandResult.Success(TakeBaseline()))),
            new Subcommand("held", "The other player holds an item whose name contains this.",
                (_, args) => Signal("held", CheckHeld(args.Text("item"))), P.Text("item")),
            new Subcommand("hurt", "The other player shows damage or bleeding.",
                (_, _) => Signal("hurt", CheckHurt())),
            new Subcommand("door-opened", "More airlocks are open than at the baseline.",
                (_, _) => Signal("door-opened", CheckDoorOpened())),
            new Subcommand("lights-off", "Fewer fixtures are lit than at the baseline.",
                (_, _) => Signal("lights-off", CheckLightsOff())),
            new Subcommand("atmos", "Report this process's atmos GPU snapshot; with assert, fail while it isn't valid.",
                (_, args) => CheckAtmos(args.Has("mode")), P.Choice("mode", AssertWord).Optional()),
        };

        private CommandResult Signal(string check, CommandResult result)
        {
            if (result.Ok)
            {
                TestSignal.Emit(this, "ObserveSync", $"{check}: {result.Message}");
            }

            return result;
        }

        private static string TakeBaseline()
        {
            _baselineLitFixtures = CountLitFixtures();
            _baselineOpenAirlocks = CountOpenAirlocks();
            return $"lit fixtures {_baselineLitFixtures}, open airlocks {_baselineOpenAirlocks}";
        }

        private static CommandResult CheckHeld(string itemName)
        {
            if (!TryGetOtherPlayer(out Entity other, out CommandResult failure))
            {
                return failure;
            }

            if (!other.TryGetHumanInventory(out HumanInventory inventory) || inventory.Hands == null)
            {
                return CommandResult.Error($"{other.name} has no hands on this client");
            }

            string wanted = Normalize(itemName);
            Item[] held = inventory.Hands.HandContainers.SelectMany(container => container.Items).Where(item => item != null).ToArray();
            if (!held.Any(item => Normalize(item.Name).Contains(wanted) || Normalize(item.gameObject.name).Contains(wanted)))
            {
                return CommandResult.Error(
                    $"{other.name} holds [{string.Join(", ", held.Select(item => item.Name))}] on this client, not {itemName}");
            }

            return CommandResult.Success($"{other.name} holds {itemName}");
        }

        private static CommandResult CheckHurt()
        {
            if (!TryGetOtherPlayer(out Entity other, out CommandResult failure))
            {
                return failure;
            }

            HumanHealthController health = other.GetComponent<HumanHealthController>();
            if (health == null)
            {
                return CommandResult.Error($"{other.name} has no health controller");
            }

            HealthSnapshot snapshot = health.Snapshot;
            if (snapshot.WorstZoneBrute <= 0f && snapshot.WorstZoneBurn <= 0f && !snapshot.IsBleeding)
            {
                return CommandResult.Error($"{other.name} shows no damage on this client");
            }

            return CommandResult.Success(
                $"{other.name} brute {snapshot.WorstZoneBrute:F0}, burn {snapshot.WorstZoneBurn:F0}, bleeding {snapshot.IsBleeding}");
        }

        private static CommandResult CheckDoorOpened()
        {
            if (!HasBaseline(out CommandResult failure))
            {
                return failure;
            }

            int open = CountOpenAirlocks();
            return open <= _baselineOpenAirlocks
                ? CommandResult.Error($"open airlocks {open}, baseline {_baselineOpenAirlocks}")
                : CommandResult.Success($"open airlocks {_baselineOpenAirlocks} -> {open}");
        }

        private static CommandResult CheckLightsOff()
        {
            if (!HasBaseline(out CommandResult failure))
            {
                return failure;
            }

            int lit = CountLitFixtures();
            return lit >= _baselineLitFixtures
                ? CommandResult.Error($"lit fixtures {lit}, baseline {_baselineLitFixtures}")
                : CommandResult.Success($"lit fixtures {_baselineLitFixtures} -> {lit}");
        }

        private CommandResult CheckAtmos(bool assert)
        {
            bool valid = AtmosRenderContext.TryGetSnapshot(out AtmosRenderContext.Snapshot snapshot);
            string payload = valid
                ? $"mapId={snapshot.MapId} atlasBounds={snapshot.AtlasBounds}"
                : "no snapshot";

            TestSignal.Emit(this, valid ? "AtmosClientSnapshotValid" : "AtmosClientSnapshotInvalid", payload);

            if (assert && !valid)
            {
                return CommandResult.Error("Atmos client snapshot is not valid — no chunk patches have been applied yet");
            }

            return CommandResult.Success(valid
                ? $"Atmos client snapshot valid ({payload})"
                : "Atmos client snapshot not valid (no chunk patches applied yet)");
        }

        private static bool TryGetOtherPlayer(out Entity other, out CommandResult failure)
        {
            NetworkConnection self = InstanceFinder.ClientManager.Connection;
            other = SubSystems.Get<EntitySubSystem>().SpawnedPlayers
                .FirstOrDefault(entity => entity != null && entity.Owner != self);
            failure = other == null
                ? CommandResult.Error("No other player's body is spawned on this client")
                : default;
            return other != null;
        }

        private static bool HasBaseline(out CommandResult failure)
        {
            bool has = _baselineLitFixtures >= 0;
            failure = has ? default : CommandResult.Error("Run 'observesync baseline' before the other player acts");
            return has;
        }

        private static int CountLitFixtures() =>
            UnityEngine.Object.FindObjectsByType<LightPower>(FindObjectsSortMode.None)
                .Count(light => light.Visual != LightPower.FixtureVisual.Off);

        private static int CountOpenAirlocks() =>
            UnityEngine.Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None).Count(airlock => airlock.IsOpen);

        private static string Normalize(string name) => (name ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
    }
}
