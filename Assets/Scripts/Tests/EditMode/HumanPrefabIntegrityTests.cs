using System.Linq;
using NUnit.Framework;
using SS3D.Hacks;
using SS3D.Systems.Inventory.Containers;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// Guards TECH_DEBT.md §1.1 "enforcement is convention only" — catches drift that hand-editing
    /// Human.prefab's YAML would otherwise let through silently. See
    /// Documents/architecture/systems/entities-prefab-recipes.md § Tests.
    /// </summary>
    public class HumanPrefabIntegrityTests
    {
        private const string HumanPrefabPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";
        private const string TestHumanPrefabPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/TestHuman.prefab";

        [TestCase(HumanPrefabPath)]
        [TestCase(TestHumanPrefabPath)]
        public void Prefab_HasNoMissingScripts(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Could not load prefab at {prefabPath}");

            // A missing-script component surfaces as a Unity "fake null" MonoBehaviour reference.
            int missingCount = prefab.GetComponentsInChildren<MonoBehaviour>(true).Count(behaviour => behaviour == null);

            Assert.AreEqual(0, missingCount, $"{prefabPath} has {missingCount} component(s) with a missing script.");
        }

        [Test]
        public void HumanPrefab_HasNoDevOnlyHackComponents()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
            Assert.IsNotNull(prefab, $"Could not load prefab at {HumanPrefabPath}");

            int hacks = prefab.GetComponentsInChildren<MonoBehaviour>(true).Count(b => b != null && b.GetType().Assembly.GetName().Name == "SS3D.Hacks");

            Assert.AreEqual(0, hacks, "Human.prefab must not ship SS3D.Hacks debug components to players.");
        }

        [Test]
        public void HumanPrefab_HasRagdollPresentationAuthority()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
            Assert.IsNotNull(prefab, $"Could not load prefab at {HumanPrefabPath}");

            Assert.IsTrue(
                prefab.TryGetComponent(out SS3D.Systems.Entities.Humanoid.Ragdoll _),
                "Human.prefab must keep Ragdoll as the body presentation authority.");
        }

        [TestCase("Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/HumanHead.prefab")]
        [TestCase("Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/HumanTorso.prefab")]
        public void BodyPart_HasNoRootContainerInteractive(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Could not load prefab at {prefabPath}");

            Assert.IsFalse(
                prefab.TryGetComponent(out ContainerInteractive _),
                $"{prefabPath} root must not expose a world ContainerInteractive (combat/examine targeting clarity).");
        }

        [Test]
        public void HumanPrefab_HandsAreWiredLeftThenRight()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
            Assert.IsNotNull(prefab, $"Could not load prefab at {HumanPrefabPath}");

            Assert.IsTrue(prefab.TryGetComponent(out Hands hands), "Human.prefab must have a Hands component.");
            Assert.IsNotNull(hands.PlayerHands, "Hands.PlayerHands must not be null.");
            Assert.AreEqual(2, hands.PlayerHands.Count, "Hands.PlayerHands must reference exactly one Left and one Right hand.");
            Assert.IsNotNull(hands.PlayerHands[0], "Hands.PlayerHands[0] must not be a dangling reference.");
            Assert.IsNotNull(hands.PlayerHands[1], "Hands.PlayerHands[1] must not be a dangling reference.");

            // Hands.OnStartServer selects PlayerHands.FirstOrDefault() as the initial active hand —
            // order matters, not just membership. See HandsPrefabSetup remarks.
            Assert.AreEqual(HandSide.Left, hands.PlayerHands[0].Side, "Hands.PlayerHands[0] must be the Left hand (initial active hand).");
            Assert.AreEqual(HandSide.Right, hands.PlayerHands[1].Side, "Hands.PlayerHands[1] must be the Right hand.");
        }

        [Test]
        public void HumanPrefab_HasCombatInteractionNetworkBesideController()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
            Assert.IsNotNull(prefab, $"Could not load prefab at {HumanPrefabPath}");

            Assert.IsTrue(
                prefab.TryGetComponent(out SS3D.Systems.Interactions.InteractionController _),
                "Human.prefab must keep InteractionController.");
            Assert.IsTrue(
                prefab.TryGetComponent(out SS3D.Systems.Combat.CombatInteractionNetwork _),
                "Human.prefab must have CombatInteractionNetwork (Harm primary RPCs).");
        }
    }
}
