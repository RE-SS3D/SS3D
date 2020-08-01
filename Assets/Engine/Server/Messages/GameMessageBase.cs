﻿using Mirror;
using UnityEngine;

public abstract class GameMessageBase : MessageBase
{
    public GameObject NetworkObject;
    public GameObject[] NetworkObjects;

    /// <summary>
    /// Called before any message processing takes place
    /// </summary>
    public virtual void PreProcess(NetworkConnection sentBy, GameMessageBase b)
    {
        b.Process(sentBy);
    }

    public abstract void Process();

    public virtual void Process(NetworkConnection sentBy)
    {
        Process();
    }

    protected bool LoadNetworkObject(uint id)
    {
        if (NetworkIdentity.spawned.ContainsKey(id))
        {
            NetworkObject = NetworkIdentity.spawned[id].gameObject;
            return true;
        }

        return false;
    }

    protected void LoadMultipleObjects(uint[] ids)
    {
        NetworkObjects = new GameObject[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            var netId = ids[i];
            if (NetworkIdentity.spawned.ContainsKey(netId))
            {
                NetworkObjects[i] = NetworkIdentity.spawned[netId].gameObject;
            }
        }
    }
}
