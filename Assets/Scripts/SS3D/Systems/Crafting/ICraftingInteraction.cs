using SS3D.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICraftingInteraction
{
    public bool CanCraft(InteractionEvent interactionEvent);

    public void Craft(InteractionEvent interactionEvent);
}
