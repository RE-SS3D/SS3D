using FishNet;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Systems.Inventory.Items;
using System;

/// <summary>
/// Body part for a human head.
/// </summary>
public class HeadBodyPart : BodyPart
{
    public Brain brain;

    public override void Init(BodyPart parent)
    {
        base.Init(parent);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _internalBodyParts.Container.AddItem(brain.gameObject.GetComponent<Item>());
    }

    protected override void AddInitialLayers()
    {
        TryAddBodyLayer(new MuscleLayer(this));
        TryAddBodyLayer(new BoneLayer(this));
        TryAddBodyLayer(new CirculatoryLayer(this));
        TryAddBodyLayer(new NerveLayer(this));
        InvokeOnBodyPartLayerAdded();
    }

    protected override void DetachBodyPart()
    {
        if (_isDetached) return;
        DetachChildBodyParts();
        HideSeveredBodyPart();

        // When detached, spawn a head and set player's mind to be in the head,
        // so that player can still play as a head (death is near though..).
        BodyPart go = SpawnDetachedBodyPart();
        MindSystem entitySystem = Subsystems.Get<MindSystem>();
        entitySystem.SwapMinds(GetComponentInParent<Entity>(), go.GetComponent<Entity>());
        go.GetComponent<NetworkObject>().RemoveOwnership();

        InvokeOnBodyPartDetached();
        _isDetached = true;
        Dispose(false);
    }
}
