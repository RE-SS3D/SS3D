using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTypeQuantity
{
    public DamageType damageType;
    public float quantity;

    public DamageTypeQuantity(DamageType damageType, float quantity)
    {
        this.damageType = damageType;
        this.quantity = quantity;
    }
}
