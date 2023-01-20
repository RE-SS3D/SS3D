using SS3D.Systems.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Brain : MonoBehaviour
{
    public List<DamageTypeQuantity> DamageTypeQuantities;
    public float TotalDamage => DamageTypeQuantities.Sum(x => x.quantity);

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

        if(TotalDamage >= 100)
        {
            //gameObject.GetComponent<Kill>().ClientKill();
        }
    }
}
