using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneLayer : BiologicalLayer
{

    public override float OxygenConsumptionRate { get => 0.1f; }

    protected const float BaseBloodProduction = 0.1f;

    public override BodyLayerType LayerType
    {
        get { return BodyLayerType.Bone; }
        protected set { LayerType = value; } 
    }

    public BoneLayer(BodyPart bodyPart) : base(bodyPart)
    {

    }

    protected override void SetSuceptibilities()
    {
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Crush, 2f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Puncture, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Pressure, 0f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Cold, 0.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Shock, 0.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Toxic, 0.8f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Oxy, 0f));
    }

    /// <summary>
    /// Generate a certain amount of blood and put it in the circulatory system if there's one.
    /// </summary>
    public virtual void ProduceBlood()
    {
        throw new NotImplementedException();
    }
}
