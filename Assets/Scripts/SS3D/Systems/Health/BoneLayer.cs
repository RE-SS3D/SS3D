using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneLayer : BodyLayer
{
    public override BodyLayerType LayerType
    {
        get { return BodyLayerType.Bone; }
    }

    public BoneLayer(BodyPart bodyPart) : base(bodyPart)
    {

    }

    public BoneLayer(BodyPart bodyPart,
    List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances)
    : base(bodyPart, damages, susceptibilities, resistances)
    {

    }

    protected override void SetSuceptibilities()
    {
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Crush, 2f));
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Puncture, 1.5f));
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Pressure, 0f));
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Cold, 0.5f));
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Shock, 0.5f));
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Toxic, 0.8f));
        _damageSuceptibilities.Add(new DamageTypeQuantity(DamageType.Oxy, 0f));
    }

    /// <summary>
    /// Generate a certain amount of blood and put it in the circulatory system if there's one.
    /// </summary>
    public virtual void ProduceBlood()
    {
        throw new NotImplementedException();
    }
}
