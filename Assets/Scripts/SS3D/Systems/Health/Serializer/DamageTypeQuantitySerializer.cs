using FishNet.Serializing;
using SS3D.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DamageTypeQuantitySerializer
{
    public static void WriteDamageTypeQuantity(this Writer writer, DamageTypeQuantity damageTypeQuantity)
    {
        writer.WriteInt32((int)damageTypeQuantity.damageType);
        writer.WriteDouble(damageTypeQuantity.quantity);
    }

    public static DamageTypeQuantity ReadDamageTypeQuantity(this Reader reader)
    {
        var damageType = (DamageType) reader.ReadInt32();
        var quantity = reader.ReadDouble();
        return new DamageTypeQuantity(damageType, (float) quantity);
    }
}
