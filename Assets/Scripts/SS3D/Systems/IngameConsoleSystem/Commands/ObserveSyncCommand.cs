using FishNet;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Electricity;
using SS3D.Systems.Entities;
using SS3D.Systems.Furniture;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Testing;
using System;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Client-local checks that another player's actions reached this process: the observer half of
    /// the observer-sync smoke scenario (Testing/multiplayer/scenarios/observer-sync*.txt). Each check
    /// throws while the replicated state isn't there, so the automation DSL's <c>wait_console</c>
    /// retries it until it holds and a plain <c>console</c> line fails the script.
    /// Door and light checks compare against a <c>baseline</c> taken before the other player acts,
    /// so they don't depend on the map's layout.
    /// </summary>
    public class ObserveSyncCommand : Command
    {
        private static int _baselineLitFixtures = -1;
        private static int _baselineOpenAirlocks = -1;

        public override string ShortDescription => "Assert another player's actions replicated here (observer-sync smoke)";
        public override string Usage => "(baseline | held (item) | hurt | door-opened | lights-off)\n"
            + "Throws (failing an automation script) when the check doesn't hold.\n"
            + "example: observesync baseline\n"
            + "example: observesync held MedicalPatch";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Offline;

        private record CalculatedValues(string Check, string Item) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            string result = values.Check switch
            {
                "baseline" => TakeBaseline(),
                "held" => CheckHeld(values.Item),
                "hurt" => CheckHurt(),
                "door-opened" => CheckDoorOpened(),
                _ => CheckLightsOff(),
            };

            TestSignal.Emit(this, "ObserveSync", $"{values.Check}: {result}");
            return result;
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            if (args.Length == 0)
            {
                return response.MakeInvalid(WrongArgsText);
            }

            string check = args[0].ToLowerInvariant();
            return check switch
            {
                "held" when args.Length == 2 => response.MakeValid(new CalculatedValues(check, args[1])),
                "baseline" or "hurt" or "door-opened" or "lights-off" when args.Length == 1 =>
                    response.MakeValid(new CalculatedValues(check, null)),
                _ => response.MakeInvalid(WrongArgsText),
            };
        }

        private static string TakeBaseline()
        {
            _baselineLitFixtures = CountLitFixtures();
            _baselineOpenAirlocks = CountOpenAirlocks();
            return $"lit fixtures {_baselineLitFixtures}, open airlocks {_baselineOpenAirlocks}";
        }

        private static string CheckHeld(string itemName)
        {
            Entity other = RequireOtherPlayer();
            if (!other.TryGetHumanInventory(out HumanInventory inventory) || inventory.Hands == null)
            {
                throw new InvalidOperationException($"{other.name} has no hands on this client");
            }

            string wanted = Normalize(itemName);
            Item[] held = inventory.Hands.HandContainers.SelectMany(container => container.Items).Where(item => item != null).ToArray();
            if (!held.Any(item => Normalize(item.Name).Contains(wanted) || Normalize(item.gameObject.name).Contains(wanted)))
            {
                throw new InvalidOperationException(
                    $"{other.name} holds [{string.Join(", ", held.Select(item => item.Name))}] on this client, not {itemName}");
            }

            return $"{other.name} holds {itemName}";
        }

        private static string CheckHurt()
        {
            Entity other = RequireOtherPlayer();
            HumanHealthController health = other.GetComponent<HumanHealthController>();
            if (health == null)
            {
                throw new InvalidOperationException($"{other.name} has no health controller");
            }

            HealthSnapshot snapshot = health.Snapshot;
            if (snapshot.WorstZoneBrute <= 0f && snapshot.WorstZoneBurn <= 0f && !snapshot.IsBleeding)
            {
                throw new InvalidOperationException($"{other.name} shows no damage on this client");
            }

            return $"{other.name} brute {snapshot.WorstZoneBrute:F0}, burn {snapshot.WorstZoneBurn:F0}, bleeding {snapshot.IsBleeding}";
        }

        private static string CheckDoorOpened()
        {
            RequireBaseline();
            int open = CountOpenAirlocks();
            if (open <= _baselineOpenAirlocks)
            {
                throw new InvalidOperationException($"open airlocks {open}, baseline {_baselineOpenAirlocks}");
            }

            return $"open airlocks {_baselineOpenAirlocks} -> {open}";
        }

        private static string CheckLightsOff()
        {
            RequireBaseline();
            int lit = CountLitFixtures();
            if (lit >= _baselineLitFixtures)
            {
                throw new InvalidOperationException($"lit fixtures {lit}, baseline {_baselineLitFixtures}");
            }

            return $"lit fixtures {_baselineLitFixtures} -> {lit}";
        }

        private static Entity RequireOtherPlayer()
        {
            NetworkConnection self = InstanceFinder.ClientManager.Connection;
            Entity other = SubSystems.Get<EntitySubSystem>().SpawnedPlayers
                .FirstOrDefault(entity => entity != null && entity.Owner != self);
            if (other == null)
            {
                throw new InvalidOperationException("No other player's body is spawned on this client");
            }

            return other;
        }

        private static void RequireBaseline()
        {
            if (_baselineLitFixtures < 0)
            {
                throw new InvalidOperationException("Run 'observesync baseline' before the other player acts");
            }
        }

        private static int CountLitFixtures() =>
            UnityEngine.Object.FindObjectsByType<LightPower>(FindObjectsSortMode.None)
                .Count(light => light.Visual != LightPower.FixtureVisual.Off);

        private static int CountOpenAirlocks() =>
            UnityEngine.Object.FindObjectsByType<AirLockOpener>(FindObjectsSortMode.None).Count(airlock => airlock.IsOpen);

        private static string Normalize(string name) => (name ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
    }
}
