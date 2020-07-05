using System.Collections;
using System.Collections.Generic;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class VentObject : MonoBehaviour, IAtmosLoop
    {
        public enum OperatingMode
        {
            Off,
            Internal,
            External
        }

        private const float stepsToEqualize = 10f;

        public OperatingMode mode;
        public float TargetPressure = 101.3f;
        public PipeLayer pipeLayer;

        private PipeObject connectedPipe;

        public void Initialize()
        {
            // We only check the pipes that are on our own tile
            TileObject tileObject = GetComponentInParent<TileObject>();
            PipeObject[] pipes = tileObject.GetComponentsInChildren<PipeObject>();

            foreach (PipeObject pipe in pipes)
            {
                // Only take the pipe which matches the seleced layer
                if (pipe.layer == pipeLayer)
                {
                    connectedPipe = pipe;
                }
            }
        }

        public void Step()
        {
            PipeObject input = connectedPipe;
            AtmosObject output = GetComponentInParent<TileObject>().atmos;
            AtmosContainer inputContainer = input.GetAtmosContainer();

            if (input == null || input.GetTotalMoles() == 0)
                return;

            if (mode == OperatingMode.External)
            {
                // If the output pressure is acceptable
                if (output.GetPressure() <= TargetPressure - 0.1f)
                {
                    float totalMoles = input.GetTotalMoles();

                    // Calculate necessary moles to transfer using PV=nRT
                    float pressureDifference = TargetPressure - output.GetPressure();
                    float transferMoles = pressureDifference * 1000 * output.GetAtmosContainer().Volume / (output.GetAtmosContainer().GetTemperature() * Gas.gasConstant);

                    // Reach our target pressure in N steps
                    transferMoles = transferMoles / stepsToEqualize;

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

            else if (mode == OperatingMode.Internal)
            {
                // If the output pressure is acceptable
                if (input.GetPressure() <= TargetPressure - 0.1f)
                {
                    float totalMoles = input.GetTotalMoles();

                    // Calculate necessary moles to transfer using PV=nRT
                    float pressureDifference = TargetPressure - input.GetPressure();
                    float transferMoles = pressureDifference * 1000 * output.GetAtmosContainer().Volume / (output.GetAtmosContainer().GetTemperature() * Gas.gasConstant);

                    // Reach our target pressure in N steps
                    transferMoles = transferMoles / stepsToEqualize;

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
        }

        public void SetTileNeighbour(TileObject tile, int index)
        {
            return;
        }

        public void SetAtmosNeighbours()
        {
            return;
        }
    }
}