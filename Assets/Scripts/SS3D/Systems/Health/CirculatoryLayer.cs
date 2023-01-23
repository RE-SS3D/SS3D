using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CirculatoryLayer : BiologicalLayer
{
    public override float OxygenConsumptionRate { get => 0.5f; }

    public override BodyLayerType LayerType
    {
        get { return BodyLayerType.Circulatory; }
        protected set { LayerType = value; }
    }
    public CirculatoryLayer(BodyPart bodyPart) : base(bodyPart)
    {

    }

    protected override void SetSuceptibilities()
    {
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Slash, 2f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Puncture, 2f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Toxic, 1.5f));
    }

    public void Bleed()
    {

    }


}
