using System.Collections;
using UnityEngine;
using Mirror;
using SS3D.Engine.Health;

/// <summary>
///     Tells client to update conscious state
/// </summary>
public class HealthConsciousMessage : ServerMessage
{
    public uint EntityToUpdate;
    public ConsciousState ConsciousState;

    public override void Process()
    {
        LoadNetworkObject(EntityToUpdate);
        if (NetworkObject == null)
        {
            return;
        }

        var creatureHealth = NetworkObject.GetComponent<CreatureHealth>();

        if (creatureHealth != null)
        {
            creatureHealth.UpdateClientConsciousState(ConsciousState);
        }
        else
        {
            Logger.Log($"Creature Health not found for {NetworkObject} skipping conscious state update", Category.Health);
        }
    }

    public static HealthConsciousMessage Send(GameObject recipient, GameObject entityToUpdate, ConsciousState consciousState)
    {
        HealthConsciousMessage msg = new HealthConsciousMessage
        {
            EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
            ConsciousState = consciousState
        };
        msg.SendTo(recipient);
        return msg;
    }

    public static HealthConsciousMessage SendToAll(GameObject entityToUpdate, ConsciousState consciousState)
    {
        HealthConsciousMessage msg = new HealthConsciousMessage
        {
            EntityToUpdate = entityToUpdate.GetComponent<NetworkIdentity>().netId,
            ConsciousState = consciousState
        };
        msg.SendToAll();
        return msg;
    }
}