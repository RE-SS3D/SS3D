using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IOxygenConsumer
{
    public void RegisterToOxygenConsumerSystem();

    public void ConsumeOxygen();
}
