using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Roles;
using SS3D.Systems.StructuralDamage;
using UnityEditor;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// The Syndicate bomb (explosives-destruction.md §6): timer floor of 90 s, bolts that lock once armed, the
    /// nuke's screwdriver-then-wirecutters defuse, and content that can be dragged but never lifted.
    /// </summary>
    public sealed class SyndicateBombTests
    {
        private static readonly SyndicateBombState Fresh = SyndicateBombState.Fresh;

        [Test]
        public void Timer_StartsAtTheMinimum_AndStaysInRange()
        {
            Assert.AreEqual(90f, Fresh.TimerSeconds);
            Assert.IsFalse(Fresh.CanLowerTimer);
            Assert.IsTrue(Fresh.CanRaiseTimer);
            Assert.AreEqual(90f, Fresh.WithTimer(10f).TimerSeconds, "Never below 90 seconds");
            Assert.AreEqual(SyndicateBombState.MaxTimerSeconds, Fresh.WithTimer(99999f).TimerSeconds);
            Assert.IsFalse(Fresh.WithTimer(SyndicateBombState.MaxTimerSeconds).CanRaiseTimer);
        }

        [Test]
        public void Arming_UsesTheSetTimer_AndLocksIt()
        {
            SyndicateBombState armed = Fresh.WithTimer(120f).ArmedAt(10d);
            Assert.IsTrue(armed.Armed);
            Assert.AreEqual(130d, armed.Deadline);
            Assert.AreEqual(120f, armed.Remaining(10d));
            Assert.IsFalse(armed.CanSetTimer);
            Assert.IsFalse(armed.CanArm);
            Assert.IsFalse(armed.ShouldDetonate(129.9d));
            Assert.IsTrue(armed.ShouldDetonate(130d));
        }

        [Test]
        public void Bolts_LockWhileArmed_ButCanStillGoOn()
        {
            Assert.IsTrue(Fresh.CanMove);
            Assert.IsTrue(Fresh.CanAnchor);
            Assert.IsFalse(Fresh.CanUnanchor);

            SyndicateBombState armedLoose = Fresh.ArmedAt(0d);
            Assert.IsTrue(armedLoose.CanMove, "An armed but loose bomb can still be dragged away");
            Assert.IsTrue(armedLoose.CanAnchor);

            SyndicateBombState armedBolted = armedLoose.WithAnchored(true);
            Assert.IsFalse(armedBolted.CanMove);
            Assert.IsFalse(armedBolted.CanUnanchor, "Bolts are locked while armed");

            SyndicateBombState defused = armedBolted.WithPanelOpen().Defused();
            Assert.IsTrue(defused.CanUnanchor, "A defused bomb can be unbolted again");
        }

        [Test]
        public void Defuse_IsScrewdriverThenWirecutters_AndCanBeRearmed()
        {
            SyndicateBombState armed = Fresh.ArmedAt(0d);
            Assert.IsTrue(armed.CanOpenPanel(1d));
            Assert.IsFalse(armed.CanCutWires(1d), "Panel first");

            SyndicateBombState open = armed.WithPanelOpen();
            Assert.IsTrue(open.CanCutWires(1d));
            Assert.IsFalse(open.CanCutWires(90d), "Too late at the deadline");

            SyndicateBombState defused = open.Defused();
            Assert.IsFalse(defused.Armed);
            Assert.IsFalse(defused.PanelOpen);
            Assert.IsFalse(defused.ShouldDetonate(1000d));
            Assert.IsTrue(defused.CanArm);
        }

        [Test]
        public void Spent_RefusesEverything()
        {
            SyndicateBombState spent = Fresh.ArmedAt(0d).Detonated();
            Assert.IsFalse(spent.CanArm);
            Assert.IsFalse(spent.CanSetTimer);
            Assert.IsFalse(spent.CanMove);
            Assert.IsFalse(spent.CanAnchor);
            Assert.IsFalse(spent.ShouldDetonate(1000d));
        }

        [Test]
        public void Beeps_SpeedUpTowardZero()
        {
            Assert.Greater(SyndicateBombState.BeepInterval(60f), SyndicateBombState.BeepInterval(20f));
            Assert.Greater(SyndicateBombState.BeepInterval(20f), SyndicateBombState.BeepInterval(5f));
        }

        [Test]
        public void BombPrefab_IsDraggable_AndTooHeavyForAnyContainer()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Content/WorldObjects/Items/Weapons/SyndicateBomb.prefab");
            Assert.IsNotNull(prefab, "Run SS3D > Structural Damage > Run Content Prefab Recipes");
            Item item = prefab.GetComponent<Item>();
            Assert.IsTrue(item.TooHeavyToLift);
            Assert.IsNotNull(prefab.GetComponent<Draggable>());
            Assert.IsNotNull(prefab.GetComponent<SyndicateBomb>());
            Assert.IsNotNull(item.Asset, "Registered in the item database");

            Bounds bounds = prefab.GetComponent<MeshFilter>().sharedMesh.bounds;
            Assert.That(bounds.size.x, Is.InRange(0.5f, 0.7f), "As authored: about 60 cm across");
            Assert.That(bounds.size.y, Is.LessThan(bounds.size.x), "Lies flat");

            GameObject host = new("container host");
            try
            {
                AttachedContainer container = host.AddComponent<AttachedContainer>();
                Assert.IsFalse(container.CanContainItem(item), "No hand, slot or locker takes it");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Beacon_PointsAtTheBomb_AndOnlyTheFirstOperativeCarriesIt()
        {
            GameObject beacon = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Content/WorldObjects/Items/Weapons/SyndicateBombBeacon.prefab");
            Assert.IsNotNull(beacon);
            SerializedObject so = new(beacon.GetComponent<SyndicateBombBeacon>());
            Assert.IsNotNull(so.FindProperty("_bombAsset").objectReferenceValue);

            RoleLoadout operative = AssetDatabase.LoadAssetAtPath<RoleLoadout>(
                "Assets/Content/Data/Roles/Loadouts/OperativeLoadout.asset");
            Item beaconItem = beacon.GetComponent<Item>();
            Assert.AreEqual(1, operative.FirstHolderContents.SelectMany(c => c.Items).Count(r => r == beaconItem.Asset));
            Assert.IsFalse(operative.EquipmentContents.SelectMany(c => c.Items).Any(r => r == beaconItem.Asset),
                "Not in every operative's bag");
        }
    }
}
