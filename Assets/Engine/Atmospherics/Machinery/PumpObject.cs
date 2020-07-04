using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class PumpObject : MonoBehaviour, IAtmosLoop, IInteractionTarget
    {
        public enum PumpType
        {
            Pressure,
            Volume
        }

        private TileObject[] tileNeighbours = { null, null };
        private PipeObject[] atmosNeighbours = { null, null };

        private const float maxPressureSetting = 4500f;
        private const float maxVolumeSetting = 200f;
        private const float molesPerStep = 5f;
        private const float stepsToEqualize = 10f;

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

                float[] inputGasses = input.GetAtmosContainer().GetGasses();
                float[] outputGasses = output.GetAtmosContainer().GetGasses();

                // And the output pressure is acceptable
                if (pumpType == PumpType.Pressure)
                {
                    if (output.GetPressure() <= TargetPressure - 0.1f)
                    {
                        float totalMoles = input.GetTotalMoles();
                        
                        // Calculate necessary moles to transfer using PV=nRT
                        float pressureDifference = TargetPressure - output.GetPressure();
                        float transferMoles = pressureDifference * 1000 * output.volume / (output.GetAtmosContainer().GetTemperature() * Gas.gasConstant);

                        // Reach our target pressure in N steps
                        transferMoles = transferMoles / stepsToEqualize;

                        // We can't transfer more moles than there are
                        if (transferMoles > totalMoles)
                            transferMoles = totalMoles;

                        for (int i = 0; i < Gas.numOfGases; i++)
                        {
                            // Divide the moles according to their percentage
                            float molePerGas = (inputGasses[i] / totalMoles) * transferMoles;
                            if (inputGasses[i] > 0f)
                            {
                                inputGasses[i] -= molePerGas;
                                outputGasses[i] += molePerGas;
                            }
                        }
                        input.SetStateActive();
                        output.SetStateActive();
                    }
                }
                // TODO: different pump speeds between volume/pressure pumps
                else if (pumpType == PumpType.Volume)
                {
                    // At 200 L/s
                    float inputVolume = input.volume * 1000 / CurrentVolumeSetting;
                    float transferMoles = input.GetPressure() * 1000 * inputVolume / (input.GetAtmosContainer().GetTemperature() * Gas.gasConstant);
                    float totalMoles = input.GetTotalMoles();

                    for (int i = 0; i < Gas.numOfGases; i++)
                    {
                        // We can't transfer more moles than there are
                        if (transferMoles > totalMoles)
                            transferMoles = totalMoles;

                        // Divide the moles according to their percentage
                        float molePerGas = (inputGasses[i] / totalMoles) * transferMoles;

                        if (inputGasses[i] >= molePerGas)
                        {
                            inputGasses[i] -= molePerGas;
                            outputGasses[i] += molePerGas;
                            input.SetStateActive();
                            output.SetStateActive();
                        }
                    }
                }
            }
        }

        public void SetTileNeighbour(TileObject neighbour, int index)
        {
            if (index == 0 || index == 1)
                tileNeighbours[index] = neighbour;
        }

        public void SetAtmosNeighbours()
        {
            int i = 0;
            foreach (TileObject tile in tileNeighbours)
            {
                if (tile != null)
                    atmosNeighbours[i] = tile.transform.GetComponentInChildren<PipeObject>();
                i++;
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
 