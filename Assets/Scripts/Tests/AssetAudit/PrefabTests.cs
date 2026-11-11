using System;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SS3D.Attributes;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Roles;
using SS3D.Systems.Selection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetAudit
{
    public class PrefabTests
    {
        #region Tests
        /// <summary>
        /// Test to confirm that prefabs within the project are on the correct layers.
        /// The purpose of this is to ensure that any prefabs instantiated at runtime are added to the correct layer.
        /// </summary>
        [Test, TestCaseSource(nameof(AllPrefabs))]
        public void PrefabsAreOnTheirMandatedLayers(GameObject prefab)
        {
            StringBuilder sb = new();
            MonoBehaviour[] prefabMonoBehaviours = prefab.GetComponentsInChildren<MonoBehaviour>();
            bool allRelevantMonoBehavioursAreOnTheRightLayer = AssetAuditUtilities.CheckMonoBehavioursForCorrectLayer(prefabMonoBehaviours, ref sb);
            Assert.IsTrue(allRelevantMonoBehavioursAreOnTheRightLayer, sb.ToString());
        }

        /// <summary>
        /// Test to confirm that prefabs within the project do not have missing scripts.
        /// Missing scripts can occur when a script is deleted, or when script meta files are recreated.
        /// </summary>
        [Test, TestCaseSource(nameof(AllPrefabs))]
        public void PrefabsDoNotHaveMissingScripts(GameObject prefab)
        {
            StringBuilder sb = new();
            bool allScriptsExist = AssetAuditUtilities.CheckGameObjectForMissingScripts(prefab, ref sb);
            Assert.IsTrue(allScriptsExist, sb.ToString());
        }

        /// <summary>
        /// An item's rigidbody must not interpolate. Held or stored, the item is kinematic and parented to its
        /// container's display point, but an interpolated body keeps writing its own last physics pose over the
        /// transform every frame, so the item hangs where it was simulated and trails behind the holder (the
        /// soda cans did this from #1197 until 2026-11).
        /// </summary>
        [Test, TestCaseSource(nameof(AllPrefabs))]
        public void ItemRigidbodiesDoNotInterpolate(GameObject prefab)
        {
            if (prefab.GetComponentInChildren<Item>(true) == null)
            {
                return;
            }

            string[] interpolated = prefab.GetComponentsInChildren<Rigidbody>(true)
                .Where(body => body.interpolation != RigidbodyInterpolation.None)
                .Select(body => $"{body.name} ({body.interpolation})")
                .ToArray();
            Assert.IsEmpty(interpolated,
                $"{prefab.name}: set Interpolate to None on {string.Join(", ", interpolated)}");
        }

        /// <summary>
        /// The hand holds an item by its attachment point, so a point off the item's mesh makes it hang beside or
        /// below the hand. Item prefabs copied from a bigger item keep the template's point (the emergency oxygen
        /// tank held 0.3 m below the hand on the full tank's point until 2026-11).
        /// </summary>
        [Test, TestCaseSource(nameof(AllPrefabs))]
        public void ItemAttachmentPointsSitOnTheItem(GameObject prefab)
        {
            // Body parts are items too, but their points are body sockets, not grips.
            Item item = prefab.GetComponent<Item>();
            if (item == null || !AssetDatabase.GetAssetPath(prefab).StartsWith("Assets/Content/WorldObjects/Items/"))
            {
                return;
            }

            Bounds? meshBounds = null;
            foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Bounds local = filter.sharedMesh.bounds;
                Bounds inRoot = new(prefab.transform.InverseTransformPoint(filter.transform.TransformPoint(local.center)),
                    Vector3.Scale(local.size, filter.transform.lossyScale));
                if (meshBounds is Bounds sofar)
                {
                    sofar.Encapsulate(inRoot);
                    meshBounds = sofar;
                }
                else
                {
                    meshBounds = inRoot;
                }
            }

            if (meshBounds is not Bounds bounds)
            {
                return;
            }

            // A little slack: some grips sit just off a handle or a strap.
            bounds.Expand(0.1f);
            foreach (Transform point in new[] { item.AttachmentPoint, item.AttachmentPointAlt })
            {
                if (point == null)
                {
                    continue;
                }

                Vector3 local = prefab.transform.InverseTransformPoint(point.position);
                Assert.IsTrue(bounds.Contains(local),
                    $"{prefab.name}: attachment point '{point.name}' at {local:F3} is off the item's mesh ({bounds.min:F2} to {bounds.max:F2})");
            }
        }

        /// <summary>
        /// These pieces start as copies of plain clothing, so the recipe must replace the template's root mesh and examine key.
        /// </summary>
        [TestCase("SyndicateHardsuit")]
        [TestCase("SyndicateHardsuitHelmet")]
        [TestCase("SyndicateMagboots")]
        public void HardsuitPiecesKeepNothingOfTheirTemplate(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Content/WorldObjects/Items/Clothing/{name}.prefab");
            Assert.IsNull(prefab.GetComponent<MeshRenderer>(), "The root still draws the template's mesh");
            Object key = new SerializedObject(prefab.GetComponent<SimpleExaminable>()).FindProperty("key").objectReferenceValue;
            Assert.AreEqual(name, key.name, "Examine still reads as the template");
        }

        /// <summary>
        /// Shoes and gloves are one item kept on the left side, so a loadout filling the right side would spawn a second pair.
        /// </summary>
        [Test]
        public void LoadoutsDoNotFillTheSecondSideOfAPair()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:RoleLoadout"))
            {
                RoleLoadout loadout = AssetDatabase.LoadAssetAtPath<RoleLoadout>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsFalse(loadout.EquipmentAssets.ContainsKey(ContainerType.ShoeRight), loadout.name);
                Assert.IsFalse(loadout.EquipmentAssets.ContainsKey(ContainerType.GloveRight), loadout.name);
            }
        }

        /// <summary>
        /// A rigid helmet has no folded form, so every view of it shows one mesh.
        /// </summary>
        [Test]
        public void HardsuitHelmetLooksTheSameWornAndHeld()
        {
            Transform helmet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/WorldObjects/Items/Clothing/SyndicateHardsuitHelmet.prefab").transform;
            Assert.AreEqual(
                helmet.Find("WornShape").GetComponent<MeshFilter>().sharedMesh,
                helmet.Find("WorldShape").GetComponent<MeshFilter>().sharedMesh);
        }

        /// <summary>
        /// Interaction targets must be shader-pickable via Selectable on the same object or a parent.
        /// </summary>
        [Test, TestCaseSource(nameof(AllPrefabs))]
        public void InteractionTargetsHaveSelectableForShaderPicking(GameObject prefab)
        {
            StringBuilder sb = new();
            bool allTargetsArePickable = AssetAuditUtilities.CheckInteractionTargetsHaveSelectable(prefab, ref sb);
            Assert.IsTrue(allTargetsArePickable, sb.ToString());
        }
        #endregion

        #region Helper functions
        public static GameObject[] AllPrefabs()
        {
            return AssetAuditUtilities.AllPrefabs();
        }

        #endregion
    }
}
