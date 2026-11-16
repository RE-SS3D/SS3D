using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Furniture;
using SS3D.Systems.Networking;
using SS3D.Systems.Selection;
using UnityEngine;

namespace SS3D.Tests.EditMode.Furniture
{
    public class AirLockProximityServiceTests
    {
        [Test]
        public void CollectNearby_ScalesWithNearbyDoors_NotTotalDoorCount()
        {
            AirLockProximityService service = CreateService();
            var farOpeners = new List<AirLockOpener>();
            var nearOpeners = new List<AirLockOpener>();

            try
            {
                // Far cluster: 200 doors well outside the 3×3 HashGrid neighborhood.
                Vector3 farOrigin = new Vector3(500f, 0f, 500f);
                for (int i = 0; i < 200; i++)
                {
                    AirLockOpener opener = CreateOpener(farOrigin + new Vector3(i % 10, 0f, i / 10));
                    service.Register(opener);
                    farOpeners.Add(opener);
                }

                // Near cluster: 5 doors around the origin HashGrid cell.
                for (int i = 0; i < 5; i++)
                {
                    AirLockOpener opener = CreateOpener(new Vector3(i * 0.5f, 0f, 0f));
                    service.Register(opener);
                    nearOpeners.Add(opener);
                }

                Assert.AreEqual(205, service.RegisteredCount);

                var nearby = new List<AirLockOpener>();
                service.CollectNearby(Vector3.zero, nearby);

                Assert.AreEqual(5, nearby.Count, "Only doors in the local HashGrid neighborhood should be collected.");
                CollectionAssert.AreEquivalent(nearOpeners, nearby);
            }
            finally
            {
                foreach (AirLockOpener opener in farOpeners)
                {
                    service.Unregister(opener);
                    Object.DestroyImmediate(opener.gameObject);
                }

                foreach (AirLockOpener opener in nearOpeners)
                {
                    service.Unregister(opener);
                    Object.DestroyImmediate(opener.gameObject);
                }

                Object.DestroyImmediate(service.gameObject);
            }
        }

        private static AirLockProximityService CreateService()
        {
            var go = new GameObject(nameof(AirLockProximityService) + "_Test");
            return go.AddComponent<AirLockProximityService>();
        }

        private static AirLockOpener CreateOpener(Vector3 position)
        {
            var go = new GameObject("AirLockOpener_Test");
            go.transform.position = position;
            go.AddComponent<Selectable>();
            return go.AddComponent<AirLockOpener>();
        }
    }
}
