using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BiologicalLayer
{
    public List<DamageTypeQuantity> DamageTypeQuantities;
    public float TotalDamage => DamageTypeQuantities.Sum(x => x.quantity);

    

    /// <summary>
    /// Add damage without going above 100 for any given type.
    /// </summary>
    /// <param name="damageQuantity"></param>
    public void InflictDamage(DamageTypeQuantity damageQuantity)
    {
        int damageTypeIndex = DamageTypeQuantities.FindIndex(x => x.damageType == damageQuantity.damageType);
        if (damageTypeIndex == -1)
        {
            if (damageQuantity.quantity > 100)
            {
                damageQuantity.quantity = 100;
            }
            DamageTypeQuantities.Add(damageQuantity);
        }
        else
        {
            float newDamageQuantity = damageQuantity.quantity + DamageTypeQuantities[damageTypeIndex].quantity;
            if (newDamageQuantity > 100)
            {
                DamageTypeQuantities[damageTypeIndex].quantity = 100;
            }
            else
            {
                DamageTypeQuantities[damageTypeIndex].quantity = newDamageQuantity;
            }
        }
    }

    public void HealDamage(DamageTypeQuantity damageQuantity)
    {
        int damageTypeIndex = DamageTypeQuantities.FindIndex(x => x.damageType == damageQuantity.damageType);
        if (damageTypeIndex == -1)
        {
            return;
        }
        else
        {
            float newDamageQuantity = DamageTypeQuantities[damageTypeIndex].quantity - damageQuantity.quantity;
            if (newDamageQuantity < 0)
            {
                DamageTypeQuantities[damageTypeIndex].quantity = 0;
            }
            else
            {
                DamageTypeQuantities[damageTypeIndex].quantity = newDamageQuantity;
            }
        }
    }

    public float GetDamageTypeQuantity(DamageType damageType)
    {
        int damageTypeIndex = DamageTypeQuantities.FindIndex(x => x.damageType == damageType);
        return damageTypeIndex == -1 ? 0 : DamageTypeQuantities[damageTypeIndex].quantity;
    }

    public bool IsDestroyed()
    {
        return TotalDamage >= 100;
    }

}
