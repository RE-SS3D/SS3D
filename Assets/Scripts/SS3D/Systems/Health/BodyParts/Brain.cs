using SS3D.Systems.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEditor;
using SS3D.Substances;

/// <summary>
/// When the brain dies, the player dies.
/// </summary>
public class Brain : BodyPart
{
    public float PainAmount { get; private set; }

    protected override void AddInitialLayers()
    {
        TryAddBodyLayer(new CirculatoryLayer(this));
        TryAddBodyLayer(new NerveLayer(this));
        TryAddBodyLayer(new OrganLayer(this));
        InvokeOnBodyPartLayerAdded();

    }

    public override void DestroyBodyPart()
    {
        Entity entity = GetComponentInParent<Entity>();
        entity?.Kill();
        InvokeOnBodyPartDestroyed();
        Dispose(true);
    }
}
