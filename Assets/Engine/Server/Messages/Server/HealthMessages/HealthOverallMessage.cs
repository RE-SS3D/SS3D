using System.Collections;
using UnityEngine;
using Mirror;
using SS3D.Engine.Health;

/// <summary>
///     Tells client to update overall health stats
/// </summary>
public class HealthOverallMessage : ServerMessage
{
    public uint EntityToUpdate;
    public float OverallHealth;

    public override void Process()
    {
        LoadNetworkObject(EntityToUpdate);
        NetworkObject.GetComponent<CreatureHealth>().UpdateClientHealthStats(OverallHealth);
    }

    public static HealthOverallMessage Send(GameObject recipient, GameObject entityToUpdate, float overallHealth)
    {
        HealthOverallMessage msg = new HealthOverallMessage
        {
            EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
            OverallHealth = overallHealth,
        };
        msg.SendTo(recipient);
        return msg;
    }

    public static HealthOverallMessage SendToAll(GameObject entityToUpdate, float overallHealth)
    {
        HealthOverallMessage msg = new HealthOverallMessage
        {
            EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
            OverallHealth = overallHealth,
        };
        msg.SendToAll();
        return msg;
    }
}