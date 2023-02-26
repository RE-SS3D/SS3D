﻿using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SS3D.Core;
using SS3D.Core.Behaviours;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace SS3D.Networking
{
    public class NetworkPhysicsDebug : NetworkActor
    {
        public List<GameObject> SodaCans;
        public int SpawnQuantity = 2;
        public Transform SpawnPosition;

        public bool EnableForceField;
        public float ForceMultiplier = 1f;

        public List<Rigidbody> SpawnedCans;

        protected override void OnStart()
        {
            base.OnStart();
            SystemLocator.Get<InputSystem>().Inputs.Other.SpawnCans.performed += SpawnSodaCans;
        }

        [ContextMenu("Spawn Soda Cans")]
        public void SpawnSodaCans(InputAction.CallbackContext callbackContext)
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