using SS3D.Core.Behaviours;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public class OxygenConsumerSubSystem : NetworkSubSystem
    {
        private readonly List<IOxygenConsumer> _consumerList = new();
        private float _timer;
        private float _timeBeforeConsuming = 1f;

        public void RegisterConsumer(IOxygenConsumer consumer)
        {
            _consumerList.Add(consumer);
        }

        public void UnregisterConsumer(IOxygenConsumer consumer)
        {
            _consumerList.Remove(consumer);
        }

        protected void Update()
        {
            if (!IsServer)
            {
                return;
            }

            _timer += Time.deltaTime;

            if (_timer > _timeBeforeConsuming)
            {
                _timer = 0f;

                for (int i = _consumerList.Count - 1; i >= 0; i--)
                {
                    _consumerList[i].ConsumeOxygen();
                }
            }
        }
    }
}
