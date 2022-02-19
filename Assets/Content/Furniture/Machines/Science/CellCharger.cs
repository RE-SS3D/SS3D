using System.Collections;
using SS3D.Content.Furniture.Storage;
using SS3D.Engine.Inventory;
using UnityEngine;
using Mirror;
using UnityEngine.Assertions;

// This handles the cell charger, responsible exclusively for recharging batteries
public class CellCharger : NetworkBehaviour
{
    // Visual container for the cell to be put in
    public ContainerDescriptor containerDescriptor;
    public MeshRenderer wiresMeshRenderer;
    [SerializeField]private BlinkingLight redLight;
    [SerializeField]private BlinkingLight orangeLight;
    [SerializeField]private BlinkingLight yellowLight;
    [SerializeField]private BlinkingLight greenLight;

    // This is used to tell clients which lights should be on.
    [SyncVar(hook = nameof(SyncLightState))] private BatteryLightState lightState;

    private void Start()
    {
        Assert.IsNotNull(containerDescriptor);

        containerDescriptor.attachedContainer.Container.ContentsChanged += (_, items, type) =>
        {
            // Power cell does not attach to match wires
            // wiresMeshRenderer.enabled = !AttachedContainer.Container.Empty; 
            if (containerDescriptor.attachedContainer.Container.Empty)
            {
                // Cell Charger is empty
                if (isServer)
                {
                    SyncLightState(BatteryLightState.NO_BATTERY);
                }
            }
            else
            {
                // Cell Charger now has a battery in it, so update the lights based on battery charge.
                // (this prevents the delay between putting a battery in and seeing the lights change)
                if (isServer)
                {
                    foreach (Item item in containerDescriptor.attachedContainer.Container.Items)
                    {
                        IChargeable chargeable = item.GetComponent<IChargeable>();
                        if (chargeable != null) UpdateLights(chargeable.GetPowerPercentage());
                    }
                }
            }
        };
        if (isServer)
        {
            StartCoroutine(StartCharge());
        }
    }

    [Server]
    IEnumerator StartCharge()
    {
        Recharge(); // turns on the lights as soon as the powercell is attached
        while (true)
        {
            yield return new WaitForSeconds(2f);
            Recharge();
        }
    }

    [Server]
    // Recharges the current cell that is placed on it
    private void Recharge()
    {
        foreach (Item item in containerDescriptor.attachedContainer.Container.Items)
        {
            IChargeable chargeable = item.GetComponent<IChargeable>();
            if (chargeable != null)
            {
                chargeable.AddCharge(chargeable.GetChargeRate());
                UpdateLights(chargeable.GetPowerPercentage());
            }
        }
    }
    
    /// <summary>
    /// Turn off all lights
    /// </summary>
    private void TurnOffLights() 
    {
        redLight.TurnLightOff();
        orangeLight.TurnLightOff();
        yellowLight.TurnLightOff();
        greenLight.TurnLightOff();
    }

    /// <summary>
    /// Changes lightState variable based on powerPercentage.
    /// Power groups are (0 - .25), (.25 - .50), (.50 - .75), (.75 - 1) 
    /// </summary>
    /// <param name="powerPercentage">Value between 0 - 1</param>
    [Server]
    private void UpdateLights(float powerPercentage) 
    {
        if (0 <= powerPercentage && powerPercentage < .25f) // power @ 25%
        {
            SyncLightState(BatteryLightState.RED_FLASHING);
        }
        else if (.25f <= powerPercentage && powerPercentage < .50f) // power @ 50%
        {
            SyncLightState(BatteryLightState.ORANGE_FLASHING);
        }
        else if (.50f <= powerPercentage && powerPercentage < .75f) // power @ 75%
        {
            SyncLightState(BatteryLightState.YELLOW_FLASHING);
        }
        else if  (.75f <= powerPercentage && powerPercentage < 1f)
        {
            SyncLightState(BatteryLightState.GREEN_FLASHING);
        }
        else if (1f <= powerPercentage)
        {
            SyncLightState(BatteryLightState.BATTERY_FULL);
        }
    }

    [Server]
    /// <summary>
    /// Helper function called by server to change the lightState.
    /// It simply calls the SyncVar hook on the server, which changes
    /// the variable, which in turn causes the SyncVar hook on the 
    /// client to be called.
    /// </summary>
    private void SyncLightState(BatteryLightState newState)
    {
        if (newState != lightState)
        {
            SyncLightState(lightState, newState);
        }
    }

    /// <summary>
    /// Turns on lights based on the lightState variable.
    /// This is a SyncVar hook function. It is *automatically* called on the
    /// client when the lightState variable is changed on the server.
    /// This is the only place that lightState should be set directly.
    /// </summary>
    private void SyncLightState(BatteryLightState oldState, BatteryLightState newState){
        lightState = newState;
        switch (lightState)
        {
            case BatteryLightState.NO_BATTERY:
                TurnOffLights();
                break;
            case BatteryLightState.RED_FLASHING:
                redLight.MakeLightBlink();
                break;
            case BatteryLightState.ORANGE_FLASHING:
                redLight.MakeLightStayOn();
                orangeLight.MakeLightBlink();
                break;
            case BatteryLightState.YELLOW_FLASHING:
                redLight.MakeLightStayOn();
                orangeLight.MakeLightStayOn();
                yellowLight.MakeLightBlink();
                break;
            case BatteryLightState.GREEN_FLASHING:
                redLight.MakeLightStayOn();
                orangeLight.MakeLightStayOn();
                yellowLight.MakeLightStayOn();
                greenLight.MakeLightBlink();
                break;
            case BatteryLightState.BATTERY_FULL:
                redLight.MakeLightStayOn();
                orangeLight.MakeLightStayOn();
                yellowLight.MakeLightStayOn();
                greenLight.MakeLightStayOn();
                break;
        }
    }

    private enum BatteryLightState
    {
        NO_BATTERY,
        RED_FLASHING,
        ORANGE_FLASHING,
        YELLOW_FLASHING,
        GREEN_FLASHING,
        BATTERY_FULL
    }
}