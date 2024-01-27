using FishNet;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;

public abstract class MultiStepCraftable : NetworkActor, ICraftable
{

    protected int _stepAmount = 1;

    protected string _currentStepName = "";

    public string CurrentStepName => _currentStepName;


    public abstract void Craft(IInteraction interaction, InteractionEvent interactionEvent);

    public abstract void Modify(IInteraction interaction, InteractionEvent interactionEvent, string step);

    void Awake()
    {
        if (!gameObject.TryGetComponent(out IWorldObjectAsset targetAssetReference))
        {
            _currentStepName = "";
        }
        else
        {
            _currentStepName = targetAssetReference.Asset.Prefab.name;
        }
    }

}
