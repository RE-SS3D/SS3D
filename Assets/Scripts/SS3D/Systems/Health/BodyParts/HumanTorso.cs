using SS3D.Systems;
using SS3D.Systems.Health;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanTorso : BodyPart
{
    public Heart heart;

    public override void OnStartServer()
    {
        base.OnStartServer();
        AddInternalBodyPart(heart);
    }

    protected override void AddInitialLayers()
    {
        TryAddBodyLayer(new MuscleLayer(this));
        TryAddBodyLayer(new BoneLayer(this));
        TryAddBodyLayer(new CirculatoryLayer(this, 8f));
        TryAddBodyLayer(new NerveLayer(this));
        InvokeOnBodyPartLayerAdded();
    }
}
