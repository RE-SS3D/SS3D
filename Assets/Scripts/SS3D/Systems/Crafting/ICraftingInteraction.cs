using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICraftingInteraction
{
    public bool CanCraft(InteractionEvent interactionEvent);

    public void Craft(IInteraction interaction, InteractionEvent interactionEvent);
}
