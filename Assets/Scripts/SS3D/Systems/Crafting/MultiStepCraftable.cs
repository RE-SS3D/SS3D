using FishNet;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core.Behaviours;

public abstract class MultiStepCraftable : NetworkActor, ICraftable
{

    protected int _stepAmount = 1;

    protected int _currentStepNumber = 0;

    public int StepAmount => _stepAmount;

    public int CurrentStepNumber => _currentStepNumber;

    public bool IsLastStep => _stepAmount == _currentStepNumber;

    public abstract void Craft(GameObject instance, IInteraction interaction, InteractionEvent interactionEvent);

    public abstract void Modify(IInteraction interaction, InteractionEvent interactionEvent);

}
