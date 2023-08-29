using Coimbra;
using FishNet;
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandBodyPart : BodyPart
{

    [SerializeField] private Hand _hand;

    public override void Init(BodyPart parent)
    {
        base.Init(parent);
    }

    protected override void AddInitialLayers()
    {
        TryAddBodyLayer(new MuscleLayer(this));
        TryAddBodyLayer(new BoneLayer(this));
        TryAddBodyLayer(new CirculatoryLayer(this));
        TryAddBodyLayer(new NerveLayer(this));
        InvokeOnBodyPartLayerAdded();
    }
}
