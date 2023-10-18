using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
    public interface IOxygenConsumer
    {
        public void RegisterToOxygenConsumerSystem();

        public void ConsumeOxygen();
    }
}
