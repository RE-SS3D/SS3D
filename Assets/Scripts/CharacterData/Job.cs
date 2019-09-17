using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public enum Job
{
    JANITOR, ASSISTANT, CLOWN, CHEF, CHAPLAIN, BARMAN, BOTANIST, MECHANIC, ENGINEER, MINER, QUARTERMASTER,
    CONSTRUCTION_WORKER, MEDICAL_DOCTOR, GENETISCIST, ROBOTICIST, SCIENTIST, SECURITY_OFFICER, DETECTIVE, CHIEF_ENGINEER,
    RESEARCH_DIRECTOR, HEAD_OF_SECURITY, HEAD_OF_PERSONNEL, CAPTAIN, TRAITOR, NUCLEAR_OPERATIVE, WIZARD,
    CHANGELING, BLOB, GANG_LEADER, GANGSTER, REVOLUTIONNARY, SPY_THIEF, VAMPIRE, WEREWOLF, WRAITH, WRESTLER, PREDATOR,
    GRINCH, KRAMPUS, AI, CYBORG, MONKEY, CLUWNE, GHOST, GHOST_DRONE, CRITTER, SANTA_CLAUS, TOURIST, VICE_OFFICER, LAWYER,
    BARBER, BOXER, MAILMAN, VIP, PHARMACIST, UNION_REP, DIPLOMAT, MUSICIAL, SALESMAN, JOURNALIST, COACH, SOUS_CHEF, WAITER,
    APICULTURIST, RADIO_HOST, PSYCHOLOGIST, HEAD_SURGEON, MIME, TEST_SUBJECT, REGIONNAL_DIRECTOR, INSPECTOR, HOLLYWOOD_ACTOR
}

public enum JobCategories { CIVILIAN, ENGINEERING, RESEARCH, SECURITY, ANTAG, SPECIAL, DAILY, GIMMICK }

// The job class contains every information pertaining to a simple job, from its name to its spawn points
[Serializable]
public abstract class JobData
{
    [SyncVar]
    public Job job;
    [SyncVar]
    public JobCategories categories;
    [SyncVar]
    public string name;
    //public SpawnPoint[] spawnPoints;
    [SyncVar]
    public int openPositions; //how many positions of this kind can be filled at max (ex. 20 assistants and one sci head), only impacts job assignation at round start
    [SyncVar]
    public int defaultSalary;
    //public Loadout DefaultLoadout;
}
