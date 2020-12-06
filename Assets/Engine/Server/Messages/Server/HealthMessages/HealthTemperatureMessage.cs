using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update temperature
/// </summary>
public class HealthTemperatureMessage : ServerMessage
{
    public float temperature;

    public override void Process()
    {
        // TODO: Handle temperature update when a player list is implemented
        // PlayerManager.LocalPlayerScript.playerHealth?.UpdateClientTemperatureStats(temperature);
    }

    public static HealthTemperatureMessage Send(GameObject entityToUpdate, float temperatureValue)
    {
        HealthTemperatureMessage msg = new HealthTemperatureMessage
        {
            temperature = temperatureValue
        };
        msg.SendTo(entityToUpdate);
        return msg;
    }
}