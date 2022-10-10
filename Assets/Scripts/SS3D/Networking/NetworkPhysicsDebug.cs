using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SS3D.Core.Behaviours;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Networking
{
    public class NetworkPhysicsDebug : NetworkedSpessBehaviour
    {
        public List<GameObject> SodaCans;
        public int SpawnQuantity = 2;
        public Transform SpawnPosition;

        public bool EnableForceField;
        public float ForceMultiplier = 1f;

        public List<Rigidbody> SpawnedCans;

        [ContextMenu("Spawn Soda Cans")]
        public void SpawnSodaCans()
        {
            SpawnSodaCansTask();
        }

        public async void SpawnSodaCansTask()
        {
            for (int i = 0; i < SpawnQuantity; i++)
            {
                int randomIndex = Random.Range(0, SodaCans.Count);
                GameObject instance = Instantiate(SodaCans[randomIndex], SpawnPosition);
                ServerManager.Spawn(instance);

                instance.transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
                Rigidbody component = instance.GetComponent<Rigidbody>();
                component.AddTorque(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360), ForceMode.Impulse);

                SpawnedCans.Add(component);
                await UniTask.Delay(Random.Range(10,100));
            }
        }

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SpawnSodaCans();
            }

            if (!EnableForceField)
            {
                return;
            }

            

            foreach (Rigidbody spawnedCan in SpawnedCans)
            {
                Vector3 force = SpawnPosition.position - spawnedCan.position;

                spawnedCan.AddTorque(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360), ForceMode.Force);
                spawnedCan.AddForce(force * (Random.Range(.5f, 2) * ForceMultiplier));
            }
        }
    }
}