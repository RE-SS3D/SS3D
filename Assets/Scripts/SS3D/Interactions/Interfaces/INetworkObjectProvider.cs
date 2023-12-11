using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface INetworkObjectProvider
{
    public NetworkObject NetworkObject { get; }
}
