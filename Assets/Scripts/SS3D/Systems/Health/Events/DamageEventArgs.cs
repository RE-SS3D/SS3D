using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageEventArgs : EventArgs
{

    public DamageEventArgs(DamageTypeQuantity damageQuantity)
    {
        DamageTypeQuantity = damageQuantity;
    }
    public DamageTypeQuantity DamageTypeQuantity { get; set; }

}
