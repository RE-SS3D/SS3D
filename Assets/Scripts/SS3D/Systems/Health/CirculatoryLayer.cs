using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CirculatoryLayer : BiologicalLayer
{
    public override float OxygenConsumptionRate { get => 0.5f; }

    public override BodyLayerType LayerType
    {
        get { return BodyLayerType.Circulatory; }
    }
    public CirculatoryLayer(BodyPart bodyPart) : base(bodyPart)
    {

    }
    public CirculatoryLayer(BodyPart bodyPart,
    List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances)
    : base(bodyPart, damages, susceptibilities, resistances)
    {

    }

    protected override void SetSuceptibilities()
    {
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Slash, 2f));
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Puncture, 2f));
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Toxic, 1.5f));
    }

    public void Bleed()
    {

    }


}
