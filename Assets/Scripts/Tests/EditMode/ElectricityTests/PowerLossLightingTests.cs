using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Interactions;
using SS3D.Systems.Electricity;
using SS3D.Systems.StructuralDamage;
using SS3D.UI.MachineInterface;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// The two batteries under a room's lights (APC cell, then each fixture's own cell) and the breakable APC.
    /// </summary>
    public class PowerLossLightingTests
    {
        private const string ApcPath = "Assets/Content/WorldObjects/Structures/WallMounts/APC.prefab";

        private readonly List<GameObject> _instantiated = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _instantiated)
            {
                Object.DestroyImmediate(go);
            }

            _instantiated.Clear();
        }

        [Test]
        public void FixtureCell_StaysOutUntilTheFixtureHasHadMains()
        {
            FixtureEmergencyCell cell = new(10f, 5f);
            Assert.IsFalse(cell.CanLight, "a fixture that never had power doesn't light up on its battery");

            cell.OnMains(0f);
            Assert.IsTrue(cell.CanLight);
        }

        [Test]
        public void FixtureCell_RunsDownThenRechargesOnMains()
        {
            FixtureEmergencyCell cell = new(10f, 5f);
            cell.OnMains(0f);

            cell.Drain(4f);
            Assert.AreEqual(0.6f, cell.Charge, 0.001f);
            cell.Drain(10f);
            Assert.AreEqual(0f, cell.Charge);
            Assert.IsFalse(cell.CanLight, "a flat cell gives no light");

            cell.OnMains(2.5f);
            Assert.AreEqual(0.5f, cell.Charge, 0.001f);
            cell.OnMains(10f);
            Assert.AreEqual(1f, cell.Charge);
        }

        [Test]
        public void ApcPrefab_IsBreakable()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ApcPath);
            BreakableDevice device = prefab.GetComponent<BreakableDevice>();
            Assert.IsNotNull(device, "Run SS3D → Electricity → Run Content Prefab Recipes");
            Assert.AreEqual(60f, device.MaxIntegrity);
            Assert.IsFalse(device.IsBroken);
        }

        [Test]
        public void BrokenApc_FeedsNothing()
        {
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ApcPath));
            _instantiated.Add(root);
            ApcController apc = root.GetComponent<ApcController>();
            BreakableDevice device = root.GetComponent<BreakableDevice>();
            Assert.AreNotEqual(ApcControlFlags.None, apc.Channels);

            bool broke = false;
            device.OnBrokenChanged += broken => broke = broken;
            Assert.IsTrue(device.ServerApplyDamage(35f, StructuralDamageSource.Melee));
            Assert.IsFalse(device.IsBroken, "a crowbar swing dents it");
            Assert.IsTrue(device.ServerApplyDamage(35f, StructuralDamageSource.Melee));
            Assert.IsTrue(device.IsBroken && broke, "the second swing breaks it");
            Assert.IsFalse(device.ServerApplyDamage(35f, StructuralDamageSource.Melee), "a dead APC can't break again");

            Assert.AreEqual(ApcControlFlags.None, apc.Channels, "every channel is off");
            Assert.AreEqual(0f, apc.MaxDeliverableKw(0.2f), "its cell can't discharge");
            Assert.AreEqual(0f, apc.AddPowerKw(5f, 0.2f), "or charge");
            Assert.IsEmpty(apc.CreateTargetInteractions(new InteractionEvent(null, apc)), "nothing to open on a dead panel");

            device.ServerRepair();
            Assert.AreNotEqual(ApcControlFlags.None, apc.Channels);
        }
    }
}
