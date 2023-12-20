using Coimbra;
using FishNet;
using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SingleStepCraftable : NetworkActor, ICraftable
{
    public int StepAmount => 1;

    public int CurrentStepNumber => 0;

    public bool IsLastStep => true;

    public abstract void Craft(IInteraction interaction, InteractionEvent interactionEvent);

    public abstract void Modify(IInteraction interaction, InteractionEvent interactionEvent);

}