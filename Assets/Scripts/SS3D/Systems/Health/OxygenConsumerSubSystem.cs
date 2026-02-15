using SS3D.Core.Behaviours;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public class OxygenConsumerSubSystem : NetworkSubSystem
    {
        private readonly List<IOxygenConsumer> consumerList = new List<IOxygenConsumer>();
        private float _timer = 0f;
        private float _timeBeforeConsuming = 1f;

        void Update()
        {
            if (!IsServer) return;

            _timer += Time.deltaTime;

            if (_timer > _timeBeforeConsuming)
            {
                _timer = 0f;

                for (int i= consumerList.Count-1; i>=0; i--)
                {
                    consumerList[i].ConsumeOxygen();
                }
            }
        }

        public void RegisterConsumer(IOxygenConsumer consumer)
        {
            consumerList.Add(consumer);
        }

        public void UnregisterConsumer(IOxygenConsumer consumer)
        {
            consumerList.Remove(consumer);
        }
    }
}
