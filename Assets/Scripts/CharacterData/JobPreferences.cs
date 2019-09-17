using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;

public enum FallbackMethod { RANDOM, ASSISTANT, CANCELJOIN } // CANCELJOIN is bad but i do not have a better formulation for the player finaly not playing

public class SyncListPreferences : SyncList<Job> { }

// This class is used to sync a character job preferences 
[Serializable]
public class JobPreferences
{
    [SyncVar]
    public Job HighPriorityJob;
    public SyncListPreferences mediumPriorityJobs;
    public SyncListPreferences lowPriorityJobs;
    public SyncListPreferences neverJobs;

    [SyncVar]
    public FallbackMethod fallback;
}
