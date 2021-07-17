using System.Collections;
using SS3D.Content.Furniture.Storage;
using SS3D.Engine.Inventory;
using UnityEngine;
using Mirror;
using UnityEngine.Assertions;

// This handles the cell charger, responsable for exclusivery recharging batteries
public class CellCharger : NetworkBehaviour
{
    // Visual container for the cell to be put in
    public AttachedContainer AttachedContainer;
    public MeshRenderer wiresMeshRenderer;
    [SerializeField]private BlinkingLight redLight;
    [SerializeField]private BlinkingLight orangeLight;
    [SerializeField]private BlinkingLight yellowLight;
    [SerializeField]private BlinkingLight greenLight;

    private void Start()
    {
        Assert.IsNotNull(AttachedContainer);

        AttachedContainer.Container.ContentsChanged += (_, items, type) =>
        {
            // Power cell does not attach to match wires
            // wiresMeshRenderer.enabled = !AttachedContainer.Container.Empty; 
            if (AttachedContainer.Container.Empty)
            {
                // Cell Charger is empty
                TurnOffLights();
            }
        };
        if (isServer)
        {
            StartCoroutine(StartCharge());
        }
    }

    IEnumerator StartCharge()
    {
        Recharge(); // turns on the lights as soon as the powercell is attached
        while (true)
        {
            yield return new WaitForSeconds(2f);
            Recharge();
        }
    }

    // Recharges the current cell that is placed on it
    private void Recharge()
    {
        foreach (Item item in AttachedContainer.Container.Items)
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
    /// Turns on lights based on powerPercentage.
    /// Power groups are (0 - .25), (.25 - .50), (.50 - .75), (.75 - 1) 
    /// </summary>
    /// <param name="powerPercentage">Value between 0 - 1</param>
    private void UpdateLights(float powerPercentage) 
    {
        if (0 <= powerPercentage && powerPercentage < .25f) // power @ 25%
        {
            redLight.MakeLightBlink();
        }
        else if (.25f <= powerPercentage && powerPercentage < .50f) // power @ 50%
        {
            redLight.MakeLightStayOn();
            orangeLight.MakeLightBlink();
        }
        else if (.50f <= powerPercentage && powerPercentage < .75f) // power @ 75%
        {
            redLight.MakeLightStayOn();
            orangeLight.MakeLightStayOn();
            yellowLight.MakeLightBlink();
        }
        else if  (.75f <= powerPercentage && powerPercentage < 1f)
        {
            redLight.MakeLightStayOn();
            orangeLight.MakeLightStayOn();
            yellowLight.MakeLightStayOn();
            greenLight.MakeLightBlink();
        }
        else if (1f <= powerPercentage)
        {
            redLight.MakeLightStayOn();
            orangeLight.MakeLightStayOn();
            yellowLight.MakeLightStayOn();
            greenLight.MakeLightStayOn();
        }
    }
}