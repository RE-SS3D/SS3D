using Coimbra;
using FishNet.Object;
using SS3D.Core.Behaviours;
using System;
using UnityEngine;

public class RecipeIngredient : NetworkActor, IRecipeIngredient
{
    [Server]
    public void Consume()
    {
        NetworkObject.Despawn();
        gameObject.Dispose(true);
    }
}
