using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cloth = SS3D.Systems.Inventory.Containers.Cloth;

namespace EditorTests
{
    public class ClothingCoverageTests
    {
        private const string HumanPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";
        private const string ClothingRoot = "Assets/Content/WorldObjects/Items/Clothing/";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private Scene _scene;
        private GameObject _human;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            _human = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HumanPath), _scene);
        }

        [TearDown]
        public void TearDown() => EditorSceneManager.ClosePreviewScene(_scene);

        [TestCase("JumpsuitGrey")]
        [TestCase("JumpsuitEngineer")]
        [TestCase("JumpsuitBotany")]
        [TestCase("JumpsuitSecurity")]
        public void WearingJumpsuit_CoversBodyAndRestoresMorphsOnRemoval(string name)
        {
            ClothesDisplayer display = _human.GetComponent<ClothesDisplayer>();
            Item item = CreateItem(name);
            Cloth cloth = item.GetComponent<Cloth>();
            Assert.That(cloth.BodyMeshOverrides, Has.Length.EqualTo(2));
            var legs = _human.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => r.name == "HumanLegLeft" || r.name == "HumanLegRight").ToArray();
            Mesh[] original = legs.Select(r => r.sharedMesh).ToArray();
            foreach (SkinnedMeshRenderer leg in legs) leg.SetBlendShapeWeight(leg.sharedMesh.GetBlendShapeIndex("Fat"), 63f);

            Change(display, item, ClothType.Jumpsuit, true);
            foreach (string hidden in new[] { "HumanTorso", "HumanArmLeft", "HumanArmRight", "UnderwearTop", "UnderwearBottom" })
                Assert.That(Renderer(hidden).forceRenderingOff, Is.True, hidden);
            for (int i = 0; i < legs.Length; i++)
            {
                Assert.That(legs[i].sharedMesh, Is.Not.SameAs(original[i]));
                Assert.That(legs[i].GetBlendShapeWeight(legs[i].sharedMesh.GetBlendShapeIndex("Fat")), Is.EqualTo(63f));
            }

            // Outer equipment must not undo jumpsuit coverage when it is taken off first.
            Item suit = CreateItem("SyndicateHardsuit");
            Change(display, suit, ClothType.ExoSuit, true);
            Assert.That(Renderer("Jumpsuit").forceRenderingOff, Is.True);
            Change(display, suit, ClothType.ExoSuit, false);
            Assert.That(Renderer("Jumpsuit").forceRenderingOff, Is.False);
            Assert.That(Renderer("HumanTorso").forceRenderingOff, Is.True);

            Change(display, item, ClothType.Jumpsuit, false);
            Assert.That(Renderer("HumanTorso").forceRenderingOff, Is.False);
            for (int i = 0; i < legs.Length; i++)
            {
                Assert.That(legs[i].sharedMesh, Is.SameAs(original[i]));
                Assert.That(legs[i].GetBlendShapeWeight(legs[i].sharedMesh.GetBlendShapeIndex("Fat")), Is.EqualTo(63f));
            }
        }

        [Test]
        public void OnlyStandardJumpsuitsCarryAnkleMeshes()
        {
            string[] standard = { "JumpsuitGrey", "JumpsuitEngineer", "JumpsuitBotany", "JumpsuitSecurity" };
            string[] carriers = AssetDatabase.FindAssets("t:Prefab", new[] { ClothingRoot.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(prefab => prefab.TryGetComponent(out Cloth cloth) && cloth.BodyMeshOverrides is { Length: > 0 })
                .Select(prefab => prefab.name)
                .ToArray();
            // Imported job clothing starts as a copy of JumpsuitGrey; inheriting its overrides would cut other garments' legs.
            Assert.That(carriers, Is.EquivalentTo(standard));
        }

        [Test]
        public void AnkleMeshes_KeepExposedSkinSkinningAndEveryMorphFrame()
        {
            Cloth cloth = AssetDatabase.LoadAssetAtPath<GameObject>(ClothingRoot + "JumpsuitGrey.prefab").GetComponent<Cloth>();
            foreach (Cloth.BodyMeshOverride coverage in cloth.BodyMeshOverrides)
            {
                var renderer = (SkinnedMeshRenderer)Renderer(coverage.RendererName);
                Mesh source = coverage.BodyMesh, mask = coverage.CoveredMesh;
                Assert.That(source, Is.SameAs(renderer.sharedMesh));
                Assert.That(mask.triangles.Length, Is.GreaterThan(0).And.LessThan(source.triangles.Length));
                Assert.That(mask.bindposes, Is.EqualTo(source.bindposes));
                Assert.That(mask.blendShapeCount, Is.EqualTo(source.blendShapeCount));
                renderer.sharedMesh = mask;
                Mesh baked = new();
                renderer.BakeMesh(baked);
                Vector3[] restVertices = baked.vertices;
                UnityEngine.Object.DestroyImmediate(baked);
                renderer.sharedMesh = source;
                foreach (int vertex in mask.triangles.Distinct())
                {
                    Vector3 bodyPosition = _human.transform.InverseTransformPoint(renderer.transform.TransformPoint(restVertices[vertex]));
                    Assert.That(bodyPosition.y, Is.LessThanOrEqualTo(0.17001f));
                    BoneWeight w = mask.boneWeights[vertex];
                    Assert.That(w.weight0 + w.weight1 + w.weight2 + w.weight3, Is.EqualTo(1f).Within(0.0001f));
                }
                for (int shape = 0; shape < source.blendShapeCount; shape++)
                {
                    Assert.That(mask.GetBlendShapeName(shape), Is.EqualTo(source.GetBlendShapeName(shape)));
                    Assert.That(mask.GetBlendShapeFrameCount(shape), Is.EqualTo(source.GetBlendShapeFrameCount(shape)));
                    for (int frame = 0; frame < source.GetBlendShapeFrameCount(shape); frame++)
                    {
                        Vector3[] original = new Vector3[source.vertexCount], covered = new Vector3[mask.vertexCount];
                        source.GetBlendShapeFrameVertices(shape, frame, original, null, null);
                        mask.GetBlendShapeFrameVertices(shape, frame, covered, null, null);
                        Assert.That(covered.Take(original.Length), Is.EqualTo(original));
                    }
                }
            }
        }

        private Renderer Renderer(string name) => _human.GetComponentsInChildren<Renderer>(true).First(r => r.name == name);
        private Item CreateItem(string name) => ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ClothingRoot + name + ".prefab"), _scene)).GetComponent<Item>();

        private static void Change(ClothesDisplayer display, Item item, ClothType type, bool add)
        {
            // Exercise the same list and client callback used for equip and initial observer sync.
            Type dataType = typeof(ClothesDisplayer).GetNestedType("ClothDisplayData", BindingFlags.NonPublic);
            object data = Activator.CreateInstance(dataType, new object[] { type, item });
            object list = typeof(ClothesDisplayer).GetField("_clothedBodyParts", Private).GetValue(display);
            list.GetType().GetMethod(add ? "Add" : "Remove", new[] { dataType }).Invoke(list, new[] { data });
            MethodInfo callback = typeof(ClothesDisplayer).GetMethod("ClothedBodyPartsOnChange", Private);
            Type operationType = callback.GetParameters()[0].ParameterType;
            callback.Invoke(display, new[] { Enum.Parse(operationType, add ? "Add" : "RemoveAt"), 0, add ? null : data, add ? data : null, false });
        }
    }
}
