using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using FishNet.Object;


/// <summary>
/// Be careful when adding fields to a bodylayer, or when creating a new bodylayer.
/// They need to be synced and therefore sent over the network, this means that you need to serialize them properly.
/// </summary>
public abstract class BodyLayer
{
    public abstract BodyLayerType LayerType { get; }

    /// <summary>
    /// The body part containing this body layer.
    /// </summary>
    public BodyPart BodyPart { get; set; }

    /// <summary>
    /// Events fired when damages are received on this layer.
    /// </summary>
    public event EventHandler<DamageEventArgs> DamageReceivedEvent;

    /// <summary>
    /// Quantity of damages on this bodyLayer
    /// </summary>
    protected List<DamageTypeQuantity> _damageTypeQuantities;

    /// <summary>
    /// Minimum amount of damage to do to make any actual damage.
    /// </summary>
    protected List<DamageTypeQuantity> _damageResistances;

    /// <summary>
    /// Susceptibility to damage, damages are multiplied by this number.
    /// </summary>
    protected List<DamageTypeQuantity> _damageSuceptibilities;

    public virtual float MaxDamage => 100;

    public const float MinDamage = 0;

    public float TotalDamage => DamageTypeQuantities.Sum(x => x.quantity);

    public List<DamageTypeQuantity> DamageTypeQuantities => _damageTypeQuantities;
    public List<DamageTypeQuantity> DamageResistances => _damageResistances;
    public List<DamageTypeQuantity> DamageSuceptibilities => _damageSuceptibilities;

    /// <summary>
    /// TODO : Put default damage suceptibility and resistance into a scriptable object and replace those lists with "damage * modifier".
    /// They should be empty most of the time as they are modifiers.
    /// </summary>
    /// <param name="bodyPart"></param>
    public BodyLayer(BodyPart bodyPart)
    {
        _damageResistances = new List<DamageTypeQuantity>();
        _damageSuceptibilities= new List<DamageTypeQuantity>();
        _damageTypeQuantities = new List<DamageTypeQuantity>();
        SetResistances();
        SetSuceptibilities();
        BodyPart = bodyPart;    
    }

    public BodyLayer(BodyPart bodyPart, List<DamageTypeQuantity> damages, List<DamageTypeQuantity> susceptibilities, List<DamageTypeQuantity> resistances)
    {
        _damageResistances = resistances;
        _damageSuceptibilities = susceptibilities;
        _damageTypeQuantities = damages;
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
        int damageTypeIndex = _damageSuceptibilities.FindIndex(x => x.damageType == damageType);
        return damageTypeIndex == -1 ? 1 : _damageSuceptibilities[damageTypeIndex].quantity;
    }

    /// <summary>
    /// Return the damage resistance for a given damage type.
    /// If no resistance is found, the default resistance is 0.
    /// </summary>
    public float GetDamageResistance(DamageType damageType)
    {
        int damageTypeIndex = _damageResistances.FindIndex(x => x.damageType == damageType);
        return damageTypeIndex == -1 ? 0 : _damageSuceptibilities[damageTypeIndex].quantity;
    }

    public virtual bool IsDestroyed()
    {
        return TotalDamage >= MaxDamage;
    }

    /// <summary>
    /// Modify the quantity of damages taken by applying susceptibility first, and by substracting resistance after.
    /// </summary>
    protected float ApplyResistanceAndSusceptibility(DamageTypeQuantity damageTypeQuantity)
    {
        float susceptibility = GetDamageTypeSusceptibility(damageTypeQuantity.damageType);
        float resistance = GetDamageResistance(damageTypeQuantity.damageType);
        float modifiedDamages = damageTypeQuantity.quantity * susceptibility - resistance;
        return modifiedDamages < 0 ? 0 : modifiedDamages;
    }

    public virtual void OnDamageInflicted(DamageTypeQuantity damageQuantity)
    {
        var args = new DamageEventArgs(damageQuantity);
        if(DamageReceivedEvent!= null) DamageReceivedEvent.Invoke(this, args);

    }

	public void CopyLayerValues(BodyLayer layer)
	{
		_damageResistances = layer._damageResistances.Select(x => new DamageTypeQuantity(x.damageType, x.quantity)).ToList();
		_damageSuceptibilities = layer._damageSuceptibilities.Select(x => new DamageTypeQuantity(x.damageType, x.quantity)).ToList();
		_damageTypeQuantities = layer._damageTypeQuantities.Select(x => new DamageTypeQuantity(x.damageType, x.quantity)).ToList();
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
