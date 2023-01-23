using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MuscleLayer : BiologicalLayer
{

    public override float OxygenConsumptionRate { get => 1f; }
    public override BodyLayerType LayerType
    {
        get { return BodyLayerType.Muscle; }
        protected set { LayerType = value; }
    }

    public MuscleLayer(BodyPart bodyPart) : base(bodyPart)
    {

    }

    protected override void SetSuceptibilities()
    {
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Slash, 2f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Puncture, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Pressure, 0f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Heat, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Cold, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Shock, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Acid, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Oxy, 1.5f));
    }
}
