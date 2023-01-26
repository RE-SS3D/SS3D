using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using FishNet.Object;

public abstract class BodyLayer
{
    public abstract BodyLayerType LayerType { get; protected set; }

    /// <summary>
    /// The body part containing this body layer.
    /// </summary>
    public BodyPart BodyPart { get; protected set; }

    /// <summary>
    /// Events fired when damages are received on this layer.
    /// </summary>
    public event EventHandler<DamageEventArgs> DamageReceivedEvent;

    /// <summary>
    /// Quantity of damages on this bodyLayer
    /// </summary>
    protected List<DamageTypeQuantity> DamageTypeQuantities;

    /// <summary>
    /// Minimum amount of damage to do to make any actual damage.
    /// </summary>
    protected List<DamageTypeQuantity> DamageResistance;

    /// <summary>
    /// Susceptibility to damage, damages are multiplied by this number.
    /// </summary>
    protected List<DamageTypeQuantity> DamageSuceptibility;

    protected virtual float MaxDamage => 100;

    protected const float MinDamage = 0;

    public float TotalDamage => DamageTypeQuantities.Sum(x => x.quantity);

    public BodyLayer(BodyPart bodyPart)
    {
        DamageResistance = new List<DamageTypeQuantity>();
        DamageSuceptibility= new List<DamageTypeQuantity>();
        DamageTypeQuantities = new List<DamageTypeQuantity>();
        SetResistances();
        SetSuceptibilities();
        BodyPart = bodyPart;    
    }

    /// <summary>
    /// Add damage without going above max damage for any given type.
    /// </summary>
    /// <param name="damageQuantity"></param>
    public virtual void InflictDamage(DamageTypeQuantity damage)
    {
        float currentDamageQuantity = GetDamageTypeQuantity(damage.damageType);
        damage.quantity = ApplyResistanceAndSusceptibility(damage);

        if (currentDamageQuantity == MinDamage)
        {
            
            if (damage.quantity > MaxDamage)
            {
                damage.quantity = MaxDamage;
            }
            DamageTypeQuantities.Add(damage);
        }
        else
        {
            int damageTypeIndex = DamageTypeQuantities.FindIndex(x => x.damageType == damage.damageType);

            float newDamageQuantity = damage.quantity + DamageTypeQuantities[damageTypeIndex].quantity;
            if (newDamageQuantity > MaxDamage)
            {
                DamageTypeQuantities[damageTypeIndex].quantity = MaxDamage;
            }
            else
            {
                DamageTypeQuantities[damageTypeIndex].quantity = newDamageQuantity;
            }
        }

        OnDamageInflicted(damage);
        // TODO : Apply some sync stuff in bodybehaviour.
    }

    public virtual void HealDamage(DamageTypeQuantity damage)
    {
        float currentDamageQuantity = GetDamageTypeQuantity(damage.damageType);
        if (currentDamageQuantity == MinDamage)
        {
            return;
        }
        else
        {
            int damageTypeIndex = DamageTypeQuantities.FindIndex(x => x.damageType == damage.damageType);
            float newDamageQuantity = DamageTypeQuantities[damageTypeIndex].quantity - damage.quantity;
            if (newDamageQuantity < MinDamage)
            {
                DamageTypeQuantities.RemoveAt(damageTypeIndex);
            }
            else
            {
                DamageTypeQuantities[damageTypeIndex].quantity = newDamageQuantity;
            }
        }
    }

    /// <summary>
    /// Get the amount of a given damage type on this body layer.
    /// </summary>
    public float GetDamageTypeQuantity(DamageType damageType)
    {
        int damageTypeIndex = DamageTypeQuantities.FindIndex(x => x.damageType == damageType);
        return damageTypeIndex == -1 ? MinDamage : DamageTypeQuantities[damageTypeIndex].quantity;
    }

    /// <summary>
    /// Return the susceptibility to a particular kind of damage. Susceptibility is one if no modifiers.
    /// </summary>
    public float GetDamageTypeSusceptibility(DamageType damageType)
    {
        int damageTypeIndex = DamageSuceptibility.FindIndex(x => x.damageType == damageType);
        return damageTypeIndex == -1 ? 1 : DamageSuceptibility[damageTypeIndex].quantity;
    }

    /// <summary>
    /// Return the damage resistance for a given damage type.
    /// If no resistance is found, the default resistance is 0.
    /// </summary>
    public float GetDamageResistance(DamageType damageType)
    {
        int damageTypeIndex = DamageResistance.FindIndex(x => x.damageType == damageType);
        return damageTypeIndex == -1 ? 0 : DamageSuceptibility[damageTypeIndex].quantity;
    }

    public virtual bool IsDestroyed()
    {
        return TotalDamage >= MaxDamage;
    }

    protected float ApplyResistanceAndSusceptibility(DamageTypeQuantity damageTypeQuantity)
    {
        float susceptibility = GetDamageTypeSusceptibility(damageTypeQuantity.damageType);
        float resistance = GetDamageResistance(damageTypeQuantity.damageType);
        return damageTypeQuantity.quantity * susceptibility - resistance;
    }

    public virtual void OnDamageInflicted(DamageTypeQuantity damageQuantity)
    {
        var args = new DamageEventArgs(damageQuantity);
        DamageReceivedEvent.Invoke(this, args);
    }

    /// <summary>
    /// Set all resistances on this body layer. By default, there are none and resistance is 0.
    /// </summary>
    protected virtual void SetResistances()
    {
        return;
    }

    /// <summary>
    /// Set all susceptibilities on this body layer. By default, susceptibility is 1.
    /// </summary>
    protected virtual void SetSuceptibilities()
    {
        return;
    }

}
