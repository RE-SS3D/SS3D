using SS3D.Engine.Health;
using UnityEngine;

/// <summary>
///     Message that tells client which UI action to perform
/// </summary>
public class UpdateHungerStateMessage : ServerMessage
{
    public HungerState State;

    public override void Process()
    {
        // TODO: Handle hungerstate update when a player manager is implemented
        //MetabolismSystem metabolismSystem = PlayerManager.LocalPlayer.GetComponent<MetabolismSystem>();
        //metabolismSystem.HungerState = State;
    }

    public static UpdateHungerStateMessage Send(GameObject recipient, HungerState state)
    {
        UpdateHungerStateMessage msg = new UpdateHungerStateMessage
        {
            State = state
        };
        msg.SendTo(recipient);
        return msg;
    }
}