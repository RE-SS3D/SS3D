using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrainBehaviour : NetworkBehaviour
{
    public float PainAmount { get; private set; }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float GetPainFromNerves()
    {
        var nerves = GetComponentsInChildren<INerveSignalTransmitter>();
        float painSum = 0;
        foreach (var nerve in nerves)
        {
            if (nerve.IsConnectedToCentralNervousSystem)
            {
                painSum += nerve.ProducePain();
            }
            
        }
        return painSum;
    }
}
