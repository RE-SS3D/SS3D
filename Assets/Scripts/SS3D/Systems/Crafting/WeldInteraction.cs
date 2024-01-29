using SS3D.Systems.Crafting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeldInteraction : CraftingInteraction
{
    public WeldInteraction(float delay, Transform characterTransform) : base(delay, characterTransform, CraftingInteractionType.Weld) { }

    public override string GetGenericName()
    {
        return "HandCraft";
    }
}
