using SS3D.Systems;
using SS3D.Systems.Health;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanTorso : BodyPart
{
    public Heart heart;
    public Lungs leftLung;
    public Lungs rightLungs;

    public override void OnStartServer()
    {
        base.OnStartServer();
        AddInternalBodyPart(heart);
        AddInternalBodyPart(leftLung);
        AddInternalBodyPart(rightLungs);
    }

    protected override void AddInitialLayers()
    {
        TryAddBodyLayer(new MuscleLayer(this));
        TryAddBodyLayer(new BoneLayer(this));
        TryAddBodyLayer(new CirculatoryLayer(this, 8f));
        TryAddBodyLayer(new NerveLayer(this));
        InvokeOnBodyPartLayerAdded();
    }

    protected override void AfterSpawningCopiedBodyPart()
    {
        return;
    }

    protected override void BeforeDestroyingBodyPart()
    {
        return;
    }
}
