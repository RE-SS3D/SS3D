using NUnit.Framework;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Health;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>Melee reach is measured from the attacker's chest to the target surface, not to its centre.</summary>
    public class MeleeReachTests
    {
        private GameObject _target;

        [TearDown]
        public void TearDown()
        {
            if (_target != null)
            {
                Object.DestroyImmediate(_target);
            }
        }

        [TestCase(0.9f, true)]
        [TestCase(1.05f, true)]
        [TestCase(1.2f, false)]
        [TestCase(1.5f, false)]
        public void ZoneReach_UsesDistanceToTheColliderSurface(float surfaceDistance, bool expected)
        {
            // 0.4 m box whose near face sits surfaceDistance ahead of the chest.
            _target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _target.transform.localScale = Vector3.one * 0.4f;
            _target.transform.position = new Vector3(0f, 1.2f, surfaceDistance + 0.2f);
            Physics.SyncTransforms();

            bool inReach = ZoneTargetResolver.IsMeleeZoneReachInRange(
                new Vector3(0f, 1.2f, 0f), MeleeHitInteraction.Reach, _target.GetComponent<Collider>());

            Assert.AreEqual(expected, inReach);
        }
    }
}
