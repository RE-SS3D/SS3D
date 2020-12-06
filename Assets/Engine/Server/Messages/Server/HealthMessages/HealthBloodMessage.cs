using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Engine.Health;
using UnityEngine;

public class HealthBloodMessage : ServerMessage
{
    public uint EntityToUpdate;
    public int HeartRate;
    public float BloodLevel;
    public float OxygenDamage;
    public float ToxinLevel;

    public override void Process()
    {
        LoadNetworkObject(EntityToUpdate);
        NetworkObject.GetComponent<CreatureHealth>().UpdateClientBloodStats(HeartRate, BloodLevel, OxygenDamage, ToxinLevel);
    }

    public static HealthBloodMessage Send(GameObject recipient, GameObject entityToUpdate, int heartRate, float bloodLevel,
        float oxygenDamage, float toxinLevel)
    {
        HealthBloodMessage msg = new HealthBloodMessage
        {
            EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
            HeartRate = heartRate,
            BloodLevel = bloodLevel,
            OxygenDamage = oxygenDamage,
            ToxinLevel = toxinLevel
        };

        msg.SendTo(recipient);
        return msg;
    }

    public static HealthBloodMessage SendToAll(GameObject entityToUpdate, int heartRate, float bloodLevel,
        float oxygenDamage, float toxinLevel)
    {
        HealthBloodMessage msg = new HealthBloodMessage
        {
            EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
            HeartRate = heartRate,
            BloodLevel = bloodLevel,
            OxygenDamage = oxygenDamage,
            ToxinLevel = toxinLevel
        };
        msg.SendToAll();
        return msg;
    }
}
