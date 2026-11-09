using System.Collections.Generic;
using NUnit.Framework;
using ObjectAssetReference = SS3D.Data.AssetDatabases.ObjectAssetReference;
using SS3D.Systems.Electricity;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    /// <summary>Breakable, replaceable fixture bulbs: rules, prefab wiring and the shot test against the glass.</summary>
    public class LightFixtureBulbTests
    {
        private const string Fixtures = "Assets/Content/WorldObjects/Structures/WallMounts/Lights/";
        private const string Items = "Assets/Content/WorldObjects/Items/Functional/Parts/Lights/";

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
        public void OnlyAWholeBulbOfTheSameKindGoesIntoANonWholeSocket()
        {
            Assert.IsTrue(LightBulbRules.CanInsert(LightBulbState.Empty, LightBulbKind.Bulb, LightBulbKind.Bulb, false));
            Assert.IsTrue(LightBulbRules.CanInsert(LightBulbState.Broken, LightBulbKind.Tube, LightBulbKind.Tube, false));
            Assert.IsFalse(LightBulbRules.CanInsert(LightBulbState.Intact, LightBulbKind.Bulb, LightBulbKind.Bulb, false));
            Assert.IsFalse(LightBulbRules.CanInsert(LightBulbState.Empty, LightBulbKind.Bulb, LightBulbKind.Tube, false));
            Assert.IsFalse(LightBulbRules.CanInsert(LightBulbState.Empty, LightBulbKind.Bulb, LightBulbKind.Bulb, true));
        }

        [Test]
        public void AnythingInTheSocketComesOutIntoAnEmptyHand()
        {
            Assert.IsTrue(LightBulbRules.CanRemove(LightBulbState.Intact, true));
            Assert.IsTrue(LightBulbRules.CanRemove(LightBulbState.Broken, true));
            Assert.IsFalse(LightBulbRules.CanRemove(LightBulbState.Empty, true));
            Assert.IsFalse(LightBulbRules.CanRemove(LightBulbState.Broken, false));
        }

        [Test]
        public void OnlyAWholeBulbEmits()
        {
            Assert.IsTrue(LightBulbRules.CanEmit(LightBulbState.Intact));
            Assert.IsFalse(LightBulbRules.CanEmit(LightBulbState.Broken));
            Assert.IsFalse(LightBulbRules.CanEmit(LightBulbState.Empty));
        }

        [Test]
        public void AShotBreaksTheNearestBulbBeforeWhereItLands()
        {
            Ray shot = new(Vector3.zero, Vector3.forward);
            Bounds near = new(new Vector3(0f, 0f, 3f), Vector3.one * 0.2f);
            Bounds far = new(new Vector3(0f, 0f, 6f), Vector3.one * 0.2f);
            Bounds aside = new(new Vector3(1f, 0f, 2f), Vector3.one * 0.2f);

            float nearest = 10f;
            Assert.IsTrue(LightBulbRules.CrossesBefore(shot, far, ref nearest));
            Assert.IsTrue(LightBulbRules.CrossesBefore(shot, near, ref nearest));
            Assert.IsFalse(LightBulbRules.CrossesBefore(shot, far, ref nearest), "a farther bulb must not replace the nearer one");
            Assert.IsFalse(LightBulbRules.CrossesBefore(shot, aside, ref nearest));

            float wall = 2f;
            Assert.IsFalse(LightBulbRules.CrossesBefore(shot, near, ref wall), "a round that lands short must not reach the bulb");
        }

        [TestCase("LightBulbFixture", LightBulbKind.Bulb, "LightBulb", "LightBulbBroken")]
        [TestCase("LightTubeFixture", LightBulbKind.Tube, "LightTube", "LightTubeBroken")]
        public void FixtureIsWiredForBreakingAndReplacing(string fixture, LightBulbKind kind, string intactItem, string brokenItem)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Fixtures + fixture + ".prefab");
            LightFixtureBulb bulb = prefab.GetComponent<LightFixtureBulb>();
            Assert.IsNotNull(bulb, "Run SS3D → Electricity → Run Content Prefab Recipes");
            Assert.AreEqual(kind, bulb.Kind);

            SerializedObject so = new(bulb);
            GameObject intact = (GameObject)so.FindProperty("_intactBulb").objectReferenceValue;
            GameObject broken = (GameObject)so.FindProperty("_brokenBulb").objectReferenceValue;
            Assert.IsTrue(intact != null && intact.activeSelf, "the whole bulb shows by default");
            Assert.IsTrue(broken != null && !broken.activeSelf, "the broken bulb is hidden until a shot");
            Assert.AreEqual(bulb, new SerializedObject(prefab.GetComponent<LightPower>()).FindProperty("_bulb").objectReferenceValue);
            AssertItem(so.FindProperty("_intactItem"), intactItem, kind, false);
            AssertItem(so.FindProperty("_brokenItem"), brokenItem, kind, true);

            Assert.IsNull(prefab.GetComponent<Collider>(), "no collider on the root's layer, where line of sight would hit it");
            BoxCollider box = prefab.GetComponentInChildren<BoxCollider>(true);
            Assert.IsTrue(box != null && box.isTrigger, "the housing needs a trigger for reach, never a solid collider");
            Assert.AreEqual(LayerMask.NameToLayer("Ignore Raycast"), box.gameObject.layer, "rounds and line of sight must pass the housing");
            Assert.IsNotNull(prefab.GetComponent<SS3D.Systems.Selection.Selectable>());
        }

        [TestCase("LightBulbFixture")]
        [TestCase("LightTubeFixture")]
        public void BreakingSwapsTheGlassAndHidesTheHitBox(string fixture)
        {
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Fixtures + fixture + ".prefab"));
            _instantiated.Add(root);
            LightFixtureBulb bulb = root.GetComponent<LightFixtureBulb>();
            SerializedObject so = new(bulb);
            GameObject intact = (GameObject)so.FindProperty("_intactBulb").objectReferenceValue;
            GameObject broken = (GameObject)so.FindProperty("_brokenBulb").objectReferenceValue;

            Assert.IsTrue(bulb.TryGetGlassBounds(out Bounds glass));
            Ray shot = new(glass.center + root.transform.forward * 3f, -root.transform.forward);
            float wall = 4f;
            Assert.IsTrue(LightBulbRules.CrossesBefore(shot, glass, ref wall), "a shot at the glass from the room hits it");

            bulb.SetState(LightBulbState.Broken);
            Assert.IsFalse(intact.activeSelf);
            Assert.IsTrue(broken.activeSelf);
            Assert.IsFalse(bulb.TryGetGlassBounds(out _), "broken glass can't be broken again");

            bulb.SetState(LightBulbState.Empty);
            Assert.IsFalse(intact.activeSelf);
            Assert.IsFalse(broken.activeSelf);

            bulb.SetState(LightBulbState.Intact);
            Assert.IsTrue(intact.activeSelf);
            Assert.IsFalse(broken.activeSelf);
        }

        private static void AssertItem(SerializedProperty property, string name, LightBulbKind kind, bool broken)
        {
            ObjectAssetReference reference = (ObjectAssetReference)property.objectReferenceValue;
            Assert.IsNotNull(reference, name);
            GameObject item = AssetDatabase.LoadAssetAtPath<GameObject>(Items + name + ".prefab");
            Assert.IsNotNull(item, name);
            Assert.AreEqual(AssetDatabase.AssetPathToGUID(Items + name + ".prefab"), reference.Id, name);
            Assert.AreEqual(reference, item.GetComponent<Item>().Asset, name);
            LightBulbItem bulb = item.GetComponent<LightBulbItem>();
            Assert.AreEqual(kind, bulb.Kind, name);
            Assert.AreEqual(broken, bulb.IsBroken, name);
        }
    }
}
