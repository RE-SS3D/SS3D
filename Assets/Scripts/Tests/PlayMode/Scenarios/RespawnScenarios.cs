using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FishNet;
using FishNet.Connection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Networking;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inputs;
using SS3D.Systems.Rounds;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SS3D.Tests
{
    // Guards (observer): a body-less player observing mid-round gets an owned ghost and no Human, and can neither respawn nor observe twice (observer.md §2, EntitySubSystem.TryObserve)
    // Guards: death leaves a corpse and a ghost, the cooldown refuses an early respawn, and a respawn gives a controllable new body and removes only the previous corpse, twice in a row (death-cloning-respawn §6, EntitySubSystem.TryRespawn; the old respawn-cleanup pilot) · Not cheaper because: needs a live round, the ghost swap and the spawn path on a networked host
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Respawn)]
    public class RespawnScenarios : ScenarioFixture
    {
        private ServerSettings _settingsBefore;

        [UnitySetUp]
        public IEnumerator SaveSettings()
        {
            _settingsBefore = Settings.Current;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator RestoreSettings()
        {
            // Gone after a scenario that leaves the session; the next boot loads the settings again.
            if (SubSystems.TryGet(out ServerSettingsService settings))
                settings.ApplyServerSettingsForTesting(_settingsBefore);
            yield break;
        }

        [UnityTest]
        public IEnumerator Kill_Ghost_Respawn_NewBodyIsControllable()
        {
            for (int life = 1; life <= 2; life++)
            {
                Entity body = HostEntity;
                SetRespawnRules(cooldownSeconds: 30);
                yield return Console.Expect($"kill {ServerHelpers.LocalCkey}", "Player killed");
                yield return WaitFor(IsGhost, $"life {life}: the dead player becomes a ghost");

                Assert.IsFalse(SubSystems.Get<EntitySubSystem>().TryRespawn(InstanceFinder.ClientManager.Connection, out string refusal),
                    $"Life {life}: the cooldown refuses an early respawn");
                StringAssert.StartsWith("You can respawn in", refusal);
                Assert.IsTrue(body != null && body.IsSpawned, $"Life {life}: a refused respawn keeps the corpse");

                SetRespawnRules(cooldownSeconds: 0);
                Assert.IsTrue(SubSystems.Get<EntitySubSystem>().TryRespawn(InstanceFinder.ClientManager.Connection, out refusal),
                    $"Life {life}: respawn succeeds once the cooldown is gone ({refusal})");
                yield return WaitFor(() => HostEntity != null && !IsGhost() && HostEntity.NetworkObject.ClientInitialized,
                    $"life {life}: the host's client has the new body");
                yield return WaitFor(() => body == null, $"life {life}: respawn removes the previous corpse");
                Assert.AreEqual(1, Object.FindObjectsByType<Human>(FindObjectsSortMode.None).Length,
                    $"Life {life}: one living Human and no corpses");

                Entity fresh = HostEntity;
                Assert.IsTrue(fresh.IsOwner, $"Life {life}: the host owns the new body");
                EmptyHands(fresh);
                yield return null;
                yield return Console.Expect("spawn MedicalPatch hand", "spawned in hand");
                yield return Console.Run("spawn MedicalPatch hand");
                StringAssert.Contains("not empty", Console.LastAnswer, $"Life {life}: the new body's hand holds what it was given");
            }
        }

        [UnityTest]
        public IEnumerator Observe_MidRound_GhostWithNoBody_CannotRespawn()
        {
            EntitySubSystem entities = SubSystems.Get<EntitySubSystem>();
            NetworkConnection conn = InstanceFinder.ClientManager.Connection;
            SetRespawnRules(cooldownSeconds: 0);
            Assert.IsFalse(entities.TryObserve(conn, out _), "A player who has a body can't observe");

            // Stand in for a player who never took a body this round.
            Entity body = Body;
            Mind bodyMind = entities.DespawnBodyForTesting(Host);
            if (bodyMind != null && bodyMind.IsSpawned)
                InstanceFinder.ServerManager.Despawn(bodyMind.NetworkObject);
            yield return WaitFor(() => body == null && HostEntity == null, "the host has no body");
            int humans = Object.FindObjectsByType<Human>(FindObjectsSortMode.None).Length;

            Assert.IsTrue(entities.TryObserve(conn, out string refusal), $"Observing a running round succeeds ({refusal})");
            yield return WaitFor(() => IsGhost() && HostEntity.NetworkObject.ClientInitialized && HostEntity.IsOwner,
                "the host's client controls an observer ghost");
            Assert.AreEqual(humans, Object.FindObjectsByType<Human>(FindObjectsSortMode.None).Length, "Observing spawns no body");
            Assert.IsTrue(entities.IsObserver(Host), "The server records the host as an observer");

            Assert.IsFalse(entities.TryRespawn(conn, out refusal), "An observer can't respawn, even with respawn on and no cooldown");
            StringAssert.Contains("Observers", refusal);
            Assert.IsFalse(entities.TryObserve(conn, out _), "Observing twice is refused");
        }

        // #215: when a session ended (leaving to the main menu), the body's controllers had already lost ownership
        // by OnDisable and skipped unsubscribing, so every later click NRE'd in the destroyed controller.
        [UnityTest]
        public IEnumerator LeavingToMainMenu_LeavesNoInputCallbacksBehind()
        {
            yield return WaitFor(() => Body.IsOwner, "the host owns the scenario body");
            Entity body = Body;
            InputActionAsset inputs = SubSystems.Get<InputSubSystem>().Inputs.asset;

            SubSystems.Get<NetworkSessionSubSystem>().LeaveToMainMenu();
            yield return WaitFor(() => body == null && SceneManager.GetActiveScene().name == Scenes.Launcher,
                "the session ends, the body is destroyed and the main menu loads", 30f);
            yield return null;

            List<string> stale = CallbacksOnDestroyedObjects(inputs);
            Assert.IsEmpty(stale, "Input callbacks still bound to destroyed objects:\n" + string.Join("\n", stale));
        }

        private static readonly string[] CallbackFields = { "m_OnStarted", "m_OnPerformed", "m_OnCanceled" };

        private static List<string> CallbacksOnDestroyedObjects(InputActionAsset asset)
        {
            List<string> stale = new();
            foreach (InputAction action in asset)
            {
                foreach (string fieldName in CallbackFields)
                {
                    // CallbackArray<T> is internal to the Input System; read it by reflection.
                    object callbacks = typeof(InputAction)
                        .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(action);
                    Assert.IsNotNull(callbacks, $"InputAction.{fieldName} not found (Input System internals changed?)");
                    Type type = callbacks.GetType();
                    int length = (int)type.GetProperty("length").GetValue(callbacks);
                    PropertyInfo indexer = type.GetProperty("Item");
                    for (int i = 0; i < length; i++)
                    {
                        foreach (Delegate callback in ((Delegate)indexer.GetValue(callbacks, new object[] { i })).GetInvocationList())
                        {
                            if (callback.Target is Object target && target == null)
                            {
                                stale.Add($"{action.actionMap?.name}/{action.name} {fieldName}: "
                                    + $"{callback.Method.DeclaringType?.Name}.{callback.Method.Name}");
                            }
                        }
                    }
                }
            }

            return stale;
        }

        private static ServerSettingsService Settings => SubSystems.Get<ServerSettingsService>();

        private bool IsGhost() => HostEntity != null && HostEntity.GetComponent<HumanoidGhostController>() != null;

        private static void SetRespawnRules(int cooldownSeconds)
        {
            ServerSettings settings = Settings.Current;
            settings.RespawnEnabled = true;
            settings.RemoveCorpseOnRespawn = true;
            settings.RespawnCooldownSeconds = cooldownSeconds;
            Settings.ApplyServerSettingsForTesting(settings);
        }
    }
}
