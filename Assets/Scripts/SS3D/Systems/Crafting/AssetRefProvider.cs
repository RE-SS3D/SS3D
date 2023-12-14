using SS3D.Data.Enums;
using SS3D.Interactions.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAssetRefProvider : INetworkObjectProvider, IGameObjectProvider
{
    ItemId ItemId { get; }
}
