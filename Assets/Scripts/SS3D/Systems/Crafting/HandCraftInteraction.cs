using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Systems.Audio;
using SS3D.Systems.Crafting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCraftInteraction : CraftingInteraction
{
    public HandCraftInteraction(float delay, Transform characterTransform) : base(delay, characterTransform, CraftingInteractionType.Handcraft) { }

    public override string GetGenericName()
    {
        return "HandCraft";
    }
}
