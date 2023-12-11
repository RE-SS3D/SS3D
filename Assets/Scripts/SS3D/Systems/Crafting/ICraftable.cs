using SS3D.Data.Enums;
using SS3D.Interactions.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICraftable : INetworkObjectProvider, IGameObjectProvider
{
    ItemId ItemId { get; }

    public void Consume();

    public void Craft(GameObject target, GameObject result);
}
