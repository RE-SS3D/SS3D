using SS3D.Substances;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CirculatoryLayer : BodyLayer
{

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
}
