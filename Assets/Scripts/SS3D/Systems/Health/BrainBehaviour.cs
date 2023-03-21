using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class to handle network and monobehaviour related code for the Brain class.
/// Put as little logic as possible in there (humble object pattern).
/// </summary>
public class BrainBehaviour : BodyPartBehaviour
{
    public float PainAmount { get; private set; }

    public Brain Brain;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float ComputeAveragePain()
    {
        return Brain.ComputeAveragePain();
    }
}
