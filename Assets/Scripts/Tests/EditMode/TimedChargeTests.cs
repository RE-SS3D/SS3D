using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.StructuralDamage;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    public class TimedChargeTests
    {
        private const string Path = "Assets/Content/WorldObjects/Items/Weapons/C4.prefab";

        [TestCase("C4", ExplosiveKind.C4, 15f)]
        [TestCase("X4", ExplosiveKind.X4, 15f)]
        [TestCase("SyndicateMiniBomb", ExplosiveKind.Minibomb, 5f)]
        public void Content_HasPhysicalScaleCountdownAndRegisteredSounds(string name, ExplosiveKind kind, float fuse)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/WorldObjects/Items/Weapons/" + name + ".prefab");
            Assert.NotNull(prefab);
            Assert.NotNull(prefab.GetComponent<TimedCharge>());
            Assert.That(prefab.GetComponent<TimedCharge>().Kind, Is.EqualTo(kind));
            Assert.That(prefab.GetComponent<TimedCharge>().FuseDuration, Is.EqualTo(fuse));
            Assert.That(prefab.GetComponentInChildren<ChargeCountdownDisplay>() != null, Is.EqualTo(kind != ExplosiveKind.Minibomb));
            Bounds bounds = prefab.GetComponent<MeshFilter>().sharedMesh.bounds;
            Assert.That(bounds.size.x, Is.InRange(0.08f, 0.4f), "FBX root scale must be baked");
            if (kind != ExplosiveKind.Minibomb)
                Assert.That(bounds.size.y, Is.InRange(0.05f, 0.12f), "Charge lies flat on its broad face");
            Assert.That(prefab.GetComponent<BoxCollider>().size, Is.EqualTo(bounds.size));
            SerializedObject charge = new(prefab.GetComponent<TimedCharge>());
            SS3D.Data.AssetDatabases.AssetDatabase sounds = AssetDatabase.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Sounds.asset");
            foreach (string field in new[] { "_countdownId", "_confirmId" })
            {
                string id = charge.FindProperty(field).stringValue;
                Assert.That(id, Is.Not.Empty);
                Assert.That(sounds.Assets[id], Is.TypeOf<AudioClip>());
                if (field == "_countdownId") Assert.That(((AudioClip)sounds.Assets[id]).length, Is.EqualTo(fuse).Within(0.01f));
            }
            Assert.NotNull(charge.FindProperty("_wirecuttersAsset").objectReferenceValue);
        }

        [Test]
        public void CountdownStop_LeavesAReusedPoolVoiceWithItsNewOwner()
        {
            GameObject chargeObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
            GameObject otherOwner = new("Other sound owner");
            GameObject voice = new("Pooled voice");
            try
            {
                TimedCharge charge = chargeObject.GetComponent<TimedCharge>();
                AudioSource source = voice.AddComponent<AudioSource>();
                source.transform.SetParent(otherOwner.transform);
                typeof(TimedCharge).GetField("_countdown", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(charge, source);
                typeof(TimedCharge).GetMethod("StopCountdown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(charge, null);
                Assert.That(source.transform.parent, Is.EqualTo(otherOwner.transform), "Despawn must not detach a voice already reused for the explosion");
                Assert.IsNull(typeof(TimedCharge).GetField("_countdown", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(charge));
                source.transform.SetParent(chargeObject.transform);
                typeof(TimedCharge).GetField("_countdown", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(charge, source);
                typeof(TimedCharge).GetMethod("StopCountdown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(charge, null);
                Assert.IsNull(source.transform.parent, "An owned countdown is returned to the pool before item despawn");
            }
            finally { Object.DestroyImmediate(voice); Object.DestroyImmediate(chargeObject); Object.DestroyImmediate(otherOwner); }
        }

        [Test]
        public void Defuse_RequiresWirecuttersAndAnUnexpiredFuse()
        {
            GameObject chargeObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
            GameObject cutters = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/WorldObjects/Items/Functional/Tools/Engineering/Wirecutters.prefab"));
            GameObject crowbar = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/WorldObjects/Items/Functional/Tools/Engineering/Crowbar.prefab"));
            try
            {
                TimedCharge charge = chargeObject.GetComponent<TimedCharge>();
                InteractionEvent good = new(cutters.GetComponent<Item>(), charge);
                InteractionEvent wrong = new(crowbar.GetComponent<Item>(), charge);
                IInteraction defuse = charge.CreateTargetInteractions(good).Single(i => i.GetGenericName() == "DefuseC4");
                Assert.That(defuse.Priority, Is.EqualTo(40), "Defuse must outrank pickup through IInteraction dispatch");
                Assert.False(defuse.CanInteract(good), "An unarmed charge cannot be defused");
                typeof(TimedCharge).GetField("_armed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(charge, true);
                typeof(TimedCharge).GetField("_deadline", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(charge, double.MaxValue);
                Assert.True(defuse.CanInteract(good));
                Assert.False(defuse.CanInteract(wrong));
                Assert.That(((DelayedInteraction)defuse).ClientDelay, Is.EqualTo(3f));
                typeof(TimedCharge).GetField("_deadline", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(charge, -1d);
                Assert.False(defuse.CanInteract(good), "Expiry wins a defuse finishing on the deadline");
            }
            finally { Object.DestroyImmediate(chargeObject); Object.DestroyImmediate(cutters); Object.DestroyImmediate(crowbar); }
        }
    }
}
