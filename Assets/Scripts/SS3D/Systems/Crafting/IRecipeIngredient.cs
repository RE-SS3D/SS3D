﻿using SS3D.Data.Enums;
using SS3D.Interactions.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRecipeIngredient : INetworkObjectProvider, IGameObjectProvider, IAssetRefProvider
{
    public void Consume();
}
