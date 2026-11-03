using NUnit.Framework;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEditor;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Rifle reload: the magazine rides the left hand mid-clip and always ends back on the gun.
    /// </summary>
    public class ReloadMagazineHandoffTests
    {
        private GameObject _gun;
        private GameObject _hand;
        private Transform _magazine;
        private RangedWeaponItemExtension _weapon;

        [SetUp]
        public void SetUp()
        {
            _gun = new GameObject("Gun");
            _magazine = new GameObject("Magazine").transform;
            _magazine.SetParent(_gun.transform, false);
            _magazine.localPosition = new Vector3(0f, -0.08f, -0.1f);
            _hand = new GameObject("LeftHand");
            _hand.transform.position = new Vector3(1f, 1f, 1f);

            _weapon = _gun.AddComponent<RangedWeaponItemExtension>();
            SerializedObject so = new SerializedObject(_weapon);
            so.FindProperty("_magazine").objectReferenceValue = _magazine;
            so.FindProperty("_magazineHandPosition").vector3Value = Vector3.zero;
            so.FindProperty("_magazineHandEuler").vector3Value = Vector3.zero;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gun);
            Object.DestroyImmediate(_hand);
        }

        [TestCase(-1f, 0f)]
        [TestCase(0f, 0f)]
        [TestCase(0.1f, 0f)]
        [TestCase(0.3f, 1f)]
        [TestCase(0.44f, 0f)]
        [TestCase(0.9f, 0f)]
        public void HandWeight_FollowsClipPhases(float normalizedTime, float expected)
        {
            Assert.AreEqual(expected, ReloadMagazineHandoff.HandWeight(normalizedTime), 1e-5f);
        }

        [Test]
        public void HandWeight_RampsWithoutJumps()
        {
            float previous = 0f;
            for (float t = 0f; t <= 1f; t += 0.001f)
            {
                float weight = ReloadMagazineHandoff.HandWeight(t);
                Assert.Less(Mathf.Abs(weight - previous), 0.1f, $"weight jumps at t={t}");
                previous = weight;
            }
        }

        [Test]
        public void Apply_CarriesMagazineInHand_ThenSeatsItBack()
        {
            ReloadMagazineHandoff handoff = new ReloadMagazineHandoff();
            Vector3 rest = _magazine.localPosition;

            handoff.Apply(_weapon, _hand.transform, 0.3f);
            Assert.That(Vector3.Distance(_magazine.position, _hand.transform.position), Is.LessThan(1e-4f));
            Assert.AreEqual(_gun.transform, _magazine.parent, "magazine must stay parented to the gun");

            handoff.Apply(_weapon, _hand.transform, 0.8f);
            Assert.That(Vector3.Distance(_magazine.localPosition, rest), Is.LessThan(1e-5f));
        }

        [Test]
        public void Apply_RestoresMagazine_WhenWeaponIsNoLongerWielded()
        {
            ReloadMagazineHandoff handoff = new ReloadMagazineHandoff();
            Vector3 rest = _magazine.localPosition;

            handoff.Apply(_weapon, _hand.transform, 0.3f);
            handoff.Apply(null, null, -1f);

            Assert.That(Vector3.Distance(_magazine.localPosition, rest), Is.LessThan(1e-5f));
        }
    }
}
