using SS3D.Systems.Crafting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PryInteraction : CraftingInteraction
{
    public PryInteraction(float delay, Transform characterTransform) : base(delay, characterTransform, CraftingInteractionType.Pry) { }

    public override string GetGenericName()
    {
        return "Pry";
    }
}
