using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class PumpObject : PipeGeneric, IAtmosLoop, IInteractionTarget
    {
        public enum PumpType
        {
            Pressure,
            Volume
        }

        private const float maxPressureSetting = 4500f;
        private const float maxVolumeSetting = 200f;
        private const float molesPerStep = 5f;

        public PumpType pumpType;
        public float TargetPressure = 300f;
        public float CurrentVolumeSetting = 200f;
        public bool pumpActive= false;

        public void Step()
        {
            // If both sides of the pump are connected
            if (pumpActive && atmosNeighbours[0] && atmosNeighbours[1])
            {
                PipeObject input = atmosNeighbours[0];
                PipeObject output = atmosNeighbours[1];

                if (input.GetTotalMoles() == 0)
                    return;

                AtmosContainer inputContainer = input.GetAtmosContainer();

                // And the output pressure is acceptable
                if (pumpType == PumpType.Pressure)
                {
                    if (output.GetPressure() <= TargetPressure - 0.1f)
                    {
                        float totalMoles = input.GetTotalMoles();
                        
                        // Calculate necessary moles to transfer using PV=nRT
                        float pressureDifference = TargetPressure - output.GetPressure();
                        float transferMoles = pressureDifference * 1000 * output.volume / (output.GetAtmosContainer().GetTemperature() * Gas.gasConstant);

                        // We can not transfer more moles than the machinery allows
                        transferMoles = Mathf.Min(Gas.maxMoleTransfer, transferMoles);

                        // We can't transfer more moles than there are
                        if (transferMoles > totalMoles)
                            transferMoles = totalMoles;

                        for (int i = 0; i < Gas.numOfGases; i++)
                        {
                            // Divide the moles according to their percentage
                            float molePerGas = (inputContainer.GetGas(i) / totalMoles) * transferMoles;
                            if (inputContainer.GetGas(i) > 0f)
                            {
                                input.RemoveGas(i, molePerGas);
                                output.AddGas(i, molePerGas);
                            }
                        }
                    }
                }
                // TODO: different pump speeds between volume/pressure pumps
                else if (pumpType == PumpType.Volume)
                {
                    // At 200 L/s
                    float inputVolume = input.volume * 1000 / CurrentVolumeSetting;
                    float transferMoles = input.GetPressure() * 1000 * inputVolume / (input.GetAtmosContainer().GetTemperature() * Gas.gasConstant);
                    float totalMoles = input.GetTotalMoles();

                    // We can not transfer more moles than the machinery allows
                    transferMoles = Mathf.Min(molesPerStep, transferMoles);

                    for (int i = 0; i < Gas.numOfGases; i++)
                    {
                        // We can't transfer more moles than there are
                        if (transferMoles > totalMoles)
                            transferMoles = totalMoles;

                        // Divide the moles according to their percentage
                        float molePerGas = (inputContainer.GetGas(i) / totalMoles) * transferMoles;

                        if (inputContainer.GetGas(i) >= molePerGas)
                        {
                            input.RemoveGas(i, molePerGas);
                            output.AddGas(i, molePerGas);
                        }
                    }
                }
            }
        }

        public void SetActive(bool pumpActive)
        {
            this.pumpActive = pumpActive;
        }

        public void Initialize()
        {
            SetAtmosNeighbours();
        }

        public IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = pumpActive ? "Stop pump" : "Start pump", Interact = PumpInteract, RangeCheck = true
                }
            };
        }

        private void PumpInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            SetActive(!pumpActive);
        }
    }
}
 