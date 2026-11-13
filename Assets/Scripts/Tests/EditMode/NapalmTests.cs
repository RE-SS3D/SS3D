using NUnit.Framework;
using SS3D.Systems.Combat;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    public sealed class NapalmTests
    {
        private GameObject _surface;
        private Collider _collider;

        [SetUp]
        public void SetUp()
        {
            _surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _surface.transform.position = new Vector3(100f, -0.5f, 100f);
            _surface.transform.localScale = new Vector3(10f, 1f, 10f);
            _collider = _surface.GetComponent<Collider>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_surface);

        [Test]
        public void RepaintingRefreshesOnePatch_AndItBurnsOutWithoutMoreFuel()
        {
            NapalmPatchSet fuel = new();
            Vector3 point = new(100f, 0f, 100f);
            fuel.Deposit(_collider, point, Vector3.up);
            int id = fuel.Patches[0].Id;
            fuel.Step(4f);
            fuel.Deposit(_collider, point + Vector3.right * 0.2f, Vector3.up);
            Assert.AreEqual(1, fuel.Patches.Count);
            Assert.AreEqual(id, fuel.Patches[0].Id);
            fuel.Step(5f);
            Assert.AreEqual(1, fuel.Patches.Count, "Fire remains after firing stops.");
            fuel.Step(1.1f);
            Assert.AreEqual(1, fuel.Patches.Count, "Cold fuel leaves a scorch record.");
            Assert.AreEqual(0f, fuel.Patches[0].Remaining);
        }

        [Test]
        public void FuelSticksToItsMovingSurface_AndVanishesWhenSurfaceIsRemoved()
        {
            NapalmPatchSet fuel = new();
            Vector3 point = new(100f, 0f, 100f);
            fuel.Deposit(_collider, point, Vector3.up);
            _surface.transform.position += Vector3.right * 3f;
            Assert.That(Vector3.Distance(point + Vector3.right * 3f, fuel.Patches[0].Point), Is.LessThan(0.001f));
            _surface.SetActive(false);
            fuel.Step(0.25f);
            Assert.IsEmpty(fuel.Patches);
        }

        [Test]
        public void SurfaceSidesDoNotMerge_AndDepositsAreBounded()
        {
            NapalmPatchSet fuel = new();
            Vector3 point = new(100f, 0f, 100f);
            fuel.Deposit(_collider, point, Vector3.up);
            fuel.Deposit(_collider, point, Vector3.down);
            Assert.AreEqual(2, fuel.Patches.Count);
            for (int i = 1; i < NapalmPatchSet.Capacity + 32; i++) fuel.Deposit(_collider, point + Vector3.right * (i * 2f), Vector3.up);
            Assert.AreEqual(NapalmPatchSet.Capacity, fuel.Patches.Count);
            fuel.Clear();
            Assert.IsEmpty(fuel.Patches, "World reset clears persistent fuel.");
        }

        [Test]
        public void FireCannotHurtThroughTheSurfaceOrAnInterveningWall()
        {
            Vector3 point = new(100f, 0f, 100f);
            Assert.IsTrue(NapalmSprayRules.Exposes(point, Vector3.up, point + Vector3.up * 0.5f));
            Assert.IsFalse(NapalmSprayRules.Exposes(point, Vector3.up, point - Vector3.up * 0.5f));
            Assert.IsFalse(NapalmSprayRules.Exposes(point, Vector3.up, point + Vector3.right * 2f));
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                wall.transform.position = point + Vector3.up * 0.3f;
                wall.transform.localScale = new Vector3(1f, 0.05f, 1f);
                Physics.SyncTransforms();
                Assert.IsFalse(NapalmSprayRules.Exposes(point, Vector3.up, point + Vector3.up * 0.6f));
            }
            finally { Object.DestroyImmediate(wall); }
        }

        [Test]
        public void SprayIsAWideHorizontalFan()
        {
            Vector3 left = NapalmSprayRules.Direction(Vector3.forward, 0);
            Vector3 right = NapalmSprayRules.Direction(Vector3.forward, NapalmSprayRules.RayCount - 1);
            Assert.Less(left.x, -0.4f);
            Assert.Greater(right.x, 0.4f);
            for (int i = 0; i < NapalmSprayRules.RayCount; i++)
            {
                Vector3 ray = NapalmSprayRules.Direction(Vector3.forward, i);
                Assert.Less(Mathf.Abs(ray.y), 0.11f);
                Assert.Greater(ray.z, 0.89f);
            }
        }

        [Test]
        public void FlameContactUsesLimbVolume_NotOneBodyRootPoint()
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                body.layer = LayerMask.NameToLayer("Characters");
                body.transform.position = new Vector3(100.85f, 1f, 100f);
                Physics.SyncTransforms();
                Collider limb = body.GetComponent<Collider>();
                Assert.IsTrue(NapalmSprayRules.Touches(new Vector3(100f, 0f, 100f), Vector3.up, limb),
                    "Brushing the visible flame edge catches the body's collider.");
                body.transform.position = new Vector3(100f, 1f, 100.35f);
                Physics.SyncTransforms();
                Assert.IsTrue(NapalmSprayRules.Touches(new Vector3(100f, 1.4f, 100f), Vector3.forward, limb),
                    "A wall flame at chest height reaches the body even when the root's sample is lower.");
            }
            finally { Object.DestroyImmediate(body); }
        }

        [Test]
        public void BurningIsBoundedSeparatelyFromColdScorches()
        {
            NapalmPatchSet fuel = new();
            for (int i = 0; i < NapalmPatchSet.Capacity + 32; i++)
                fuel.Deposit(_collider, new Vector3(i * 2f, 0f, 100f), Vector3.up);
            int burning = 0;
            foreach (NapalmPatchSet.Patch patch in fuel.Patches) if (patch.Remaining > 0f) burning++;
            Assert.AreEqual(NapalmPatchSet.MaxBurning, burning);
            fuel.Step(7f);
            Assert.AreEqual(NapalmPatchSet.Capacity, fuel.Patches.Count);
            foreach (NapalmPatchSet.Patch patch in fuel.Patches) Assert.AreEqual(0f, patch.Remaining);
        }

        [Test]
        public void WorldHubRegistersNapalmInFishNetBehaviourList()
        {
            GameObject hub = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/NetworkSystemsHub.prefab");
            NapalmSubSystem napalm = hub.GetComponent<NapalmSubSystem>();
            Assert.IsNotNull(napalm);
            UnityEditor.SerializedObject serialized = new(napalm);
            int index = serialized.FindProperty("_componentIndexCache").intValue;
            UnityEditor.SerializedObject network = new(hub.GetComponent<FishNet.Object.NetworkObject>());
            Assert.AreSame(napalm, network.FindProperty("_networkBehaviours").GetArrayElementAtIndex(index).objectReferenceValue);
        }
    }
}
