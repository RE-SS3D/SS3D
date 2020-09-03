using System.Collections;
using System.Collections.Generic;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class VentObject : MonoBehaviour, IAtmosLoop, IInteractionTarget
    {
        public enum OperatingMode
        {
            Off,
            Internal,
            External
        }

        public OperatingMode mode;
        public float TargetPressure = 101.3f;
        public PipeLayer pipeLayer;

        private PipeObject connectedPipe;
        private Animator anim;
        private bool deviceActive = true;
        private bool internalActive = false;

        public void Initialize()
        {
            // Get the animator for our spin animation
            anim = GetComponent<Animator>();

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
            bool ventActive = false;

            if (deviceActive)
            {
                PipeObject input = connectedPipe;
                AtmosObject output = GetComponentInParent<TileObject>().atmos;

                if (input != null || input.GetTotalMoles() > 0)
                {

                    AtmosContainer inputContainer = input.GetAtmosContainer();


                    if (mode == OperatingMode.External)
                    {
                        // If the output pressure is acceptable
                        if (output.GetPressure() <= TargetPressure - 1f)
                        {
                            ventActive = true;
                            float totalMoles = input.GetTotalMoles();

                            // Calculate necessary moles to transfer using PV=nRT
                            float pressureDifference = TargetPressure - output.GetPressure();
                            float transferMoles = pressureDifference * 1000 * output.GetAtmosContainer().Volume / (output.GetAtmosContainer().GetTemperature() * Gas.gasConstant);

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

                    else if (mode == OperatingMode.Internal)
                    {
                        // If the output pressure is acceptable
                        if (input.GetPressure() >= TargetPressure + 1f)
                        {
                            ventActive = true;
                            float totalMoles = input.GetTotalMoles();

                            // Calculate necessary moles to transfer using PV=nRT
                            float pressureDifference = input.GetPressure() - TargetPressure;
                            float transferMoles = pressureDifference * 1000 * input.GetAtmosContainer().Volume / (input.GetAtmosContainer().GetTemperature() * Gas.gasConstant);

                            // We can not transfer more moles than the machinery allows
                            transferMoles = Mathf.Min(Gas.maxMoleTransfer, transferMoles);

                            // We can't transfer more moles than there are in the input
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
            }

            // Update the animator
            anim.SetBool("ventActive", ventActive);
            anim.SetBool("deviceActive", deviceActive);
        }

        public void SetTileNeighbour(TileObject tile, int index)
        {
            return;
        }

        public void SetAtmosNeighbours()
        {
            return;
        }

        public IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = deviceActive ? "Stop vent" : "Start vent", Interact = ActiveInteract, RangeCheck = true
                },
                new SimpleInteraction
                {
                    Name = internalActive ? "External mode" : "Internal mode", Interact = ModeInteract, RangeCheck = true
                }
            };
        }

        private void ActiveInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            deviceActive = !deviceActive;
        }

        private void ModeInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            if (mode == OperatingMode.Internal)
            {
                mode = OperatingMode.External;
                internalActive = false;
            }
            else if (mode == OperatingMode.External)
            {
                mode = OperatingMode.Internal;
                internalActive = true;
            }
        }
    }
}