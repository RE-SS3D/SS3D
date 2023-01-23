using SS3D.Systems.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class Brain : BiologicalLayer
{
    public override float OxygenConsumptionRate { get => 5f; }

    public override BodyLayerType LayerType
    {
        get { return BodyLayerType.Nerve; }
        protected set { LayerType = value; }
    }

    public Brain(BodyPart bodyPart) : base(bodyPart)
    {

    }

    protected override void SetSuceptibilities()
    {
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Slash, 2f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Puncture, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Heat, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Cold, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Shock, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Rad, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Toxic, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Acid, 1.5f));
    }

    /// <summary>
    /// 
    /// </summary>
    private void ProcessPain()
    {
       
    }
}
