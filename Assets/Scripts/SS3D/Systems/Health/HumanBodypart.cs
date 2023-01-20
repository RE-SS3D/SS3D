using Codice.Client.Commands;
using Coimbra;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class HumanBodypart : BodyPart
{
    MuscleLayer muscleLayer;
    BoneLayer boneLayer;
    CirculatoryLayer circulatoryLayer;

    public event EventHandler<DamageEventArgs> DamageReceivedEvent;

    public void InflictDamage(DamageTypeQuantity damageTypeQuantity)
    {
        switch(damageTypeQuantity.damageType)
        {
            case DamageType.Acid:
                DealAcidDamage(damageTypeQuantity); break;
            case DamageType.Heat:
                DealHeatDamage(damageTypeQuantity); break;

            // ... And tutti quanti.


        }

        OnDamageInflicted(damageTypeQuantity);
    }

    private void InflictDamageOnLayer(DamageTypeQuantity damageQuantity, BiologicalLayerType layerType)
    {
        switch (layerType)
        {
            case BiologicalLayerType.Bone:
                boneLayer.InflictDamage(damageQuantity); break;
            case BiologicalLayerType.Muscle:
                muscleLayer.InflictDamage(damageQuantity); break;
            case BiologicalLayerType.Circulatory:
                circulatoryLayer.InflictDamage(damageQuantity); break;
        }
        if (boneLayer.IsDestroyed())
        {
            LooseBodyPart();
        }
    }

    private void LooseBodyPart()
    {
        var bodyparts = GetComponentsInChildren<HumanBodypart>();
        foreach (var bodyPart in bodyparts) 
        {
            bodyPart.LooseBodyPart();
        }
        gameObject.Destroy();
    }

    public void OnDamageInflicted(DamageTypeQuantity damageQuantity)
    {
        var args = new DamageEventArgs(damageQuantity);
        DamageReceivedEvent.Invoke(this, args);
    }

    /// <summary>
    /// Acid damage won't hurt bone until there's no muscle.
    /// </summary>
    /// <param name="damageTypeQuantity"></param>
    protected virtual void DealAcidDamage(DamageTypeQuantity damageTypeQuantity)
    {
        float damageQuantity = damageTypeQuantity.quantity;
        if (!muscleLayer.IsDestroyed())
        {
            InflictDamageOnLayer(damageTypeQuantity, BiologicalLayerType.Muscle);
            InflictDamageOnLayer(damageTypeQuantity, BiologicalLayerType.Circulatory);
            InflictDamageOnLayer(damageTypeQuantity, BiologicalLayerType.Nerve);
        }
        else
        {
            InflictDamageOnLayer(damageTypeQuantity, BiologicalLayerType.Bone);
        }
    }

    protected virtual void DealHeatDamage(DamageTypeQuantity damageTypeQuantity)
    {
        InflictDamageOnLayer(damageTypeQuantity, BiologicalLayerType.Muscle);
        InflictDamageOnLayer(damageTypeQuantity, BiologicalLayerType.Circulatory);
        InflictDamageOnLayer(damageTypeQuantity, BiologicalLayerType.Bone);
        InflictDamageOnLayer(damageTypeQuantity, BiologicalLayerType.Nerve);
    }

    protected virtual void DealOxyDamage(DamageTypeQuantity damageTypeQuantity)
    {
        InflictDamageOnLayer(damageTypeQuantity, BiologicalLayerType.Muscle);
        InflictDamageOnLayer(damageTypeQuantity, BiologicalLayerType.Circulatory);
    }

}
