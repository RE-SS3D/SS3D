using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NerveLayer : BiologicalLayer
{
    public override float OxygenConsumptionRate { get => 0.2f; }

    public override BodyLayerType LayerType
    {
        get { return BodyLayerType.Nerve; }
        protected set { LayerType = value; }
    }

    public NerveLayer(BodyPart bodyPart) : base(bodyPart)
    {

    }

    protected override void SetSuceptibilities()
    {
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Slash, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Pressure, 0.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Shock, 2f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Rad, 1.5f));
        DamageSuceptibility.Add(new DamageTypeQuantity(DamageType.Toxic, 1.2f));
    }

    /// <summary>
    /// Produces a given amount of pain depending on other tissues damages.
    /// It also depends on the state of the never layer.
    /// When nerves are closed to be destroyed, they send less and less pain.
    /// </summary>
    public float ProducePain()
    {
        throw new NotImplementedException();
    }
}
