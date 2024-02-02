using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using SS3D.Systems.Crafting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using SS3D.Core;

public class Craft : MonoBehaviour, IInteractionSourceExtension
{
    [SerializeField]
    private List<CraftingInteractionType> types;

    public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
    {
        OpenCraftingMenuInteraction openCraftingMenuInteraction= new OpenCraftingMenuInteraction(types);

        foreach (IInteractionTarget target in targets)
        {
            interactions.Add(new InteractionEntry(target, openCraftingMenuInteraction));
        }

    }
}
