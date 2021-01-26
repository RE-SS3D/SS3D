using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class ScrubberObject : MonoBehaviour, IAtmosLoop, IInteractionTarget
    {
        public enum OperatingMode
        {
            Off,
            Scrubbing,
            Siphoning
        }

        public enum Range
        {
            Normal,
            Extended
        }

        public float MaxPressure = 4500f;
        public float TargetPressure = 101f;

        public OperatingMode mode = OperatingMode.Scrubbing;
        public Range range = Range.Normal;

        public bool filterOxygen = false;
        public bool filterNitrogen = false;
        public bool filterCarbonDioxide = false;
        public bool filterPlasma = false;


        public PipeLayer pipeLayer;

        private bool deviceActive = true;
        private bool siphonActive = false;
        private Animator anim;
        private float _targetPressure;
        private PipeObject connectedPipe;
        private TileObject[] tileNeighbours = { null, null, null, null };
        private AtmosObject[] atmosNeighbours = { null, null, null, null };
        private AtmosObject input = null;

        private AtmosGasses[] atmosGasses = (AtmosGasses[])Enum.GetValues(typeof(AtmosGasses));

        void Update()
        {
            if (_targetPressure != TargetPressure)
            {
                _targetPressure = Mathf.Clamp(TargetPressure, 0, MaxPressure);
                TargetPressure = _targetPressure;
            }
        }

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

            SetAtmosNeighbours();
        }

        public void Step()
        {
            int numOfTiles = 0;
            bool scrubActive = false;

            switch (range)
            {
                case Range.Normal:
                    numOfTiles = 1;
                    break;
                case Range.Extended:
                    numOfTiles = 5;
                    break;
            }
            if (deviceActive)
            {
                // We loop 1 or 5 times based on the range setting
                for (int i = 0; i < numOfTiles; i++)
                {

                    if (i == 0)
                    {
                        if (input == null)
                            input = GetComponentInParent<TileObject>().atmos;
                    }
                    else
                        input = atmosNeighbours[i - 1];

                    PipeObject output = connectedPipe;

                    if (input == null || input.GetTotalMoles() == 0 || output == null || mode == OperatingMode.Off)
                        return;

                    AtmosContainer inputContainer = input.GetAtmosContainer();
                    AtmosContainer outputContainer = output.GetAtmosContainer();



                    // If the output pressure is acceptable
                    if (output.GetPressure() <= TargetPressure - 1f)
                    {
                        float totalMoles = input.GetTotalMoles();

                        // Calculate necessary moles to transfer using PV=nRT
                        float pressureDifference = _targetPressure - output.GetPressure();
                        float transferMoles = pressureDifference * 1000 * output.volume / (output.GetAtmosContainer().GetTemperature() * Gas.gasConstant);

                        // We can not transfer more moles than the machinery allows
                        transferMoles = Mathf.Min(Gas.maxMoleTransfer, transferMoles);

                        // We don't transfer tiny amounts
                        transferMoles = Mathf.Max(transferMoles, Gas.minMoleTransfer);

                        // We can't transfer more moles than there are in the input
                        if (transferMoles > totalMoles)
                            transferMoles = totalMoles;

                        for (int j = 0; j < atmosGasses.Length; j++)
                        {
                            if (mode == OperatingMode.Siphoning)
                            {
                                scrubActive = true;

                                // Divide the moles according to their percentage
                                float molePerGas = (inputContainer.GetGas(atmosGasses[j]) / input.GetTotalMoles()) * transferMoles;
                                if (inputContainer.GetGas(atmosGasses[j]) > 0f)
                                {
                                    input.RemoveGas(atmosGasses[j], molePerGas);
                                    output.AddGas(atmosGasses[j], molePerGas);

                                }
                            }

                            // If scrubbing, remove only filtered gas
                            if (mode == OperatingMode.Scrubbing && IsFiltered(atmosGasses[j]))
                            {
                                if (inputContainer.GetGas(atmosGasses[j]) > 0f)
                                {
                                    scrubActive = true;

                                    // To avoid leaving a small amount of a certain gas, we apply the min threshold again
                                    float molePerGas = Mathf.Min(transferMoles, inputContainer.GetGas(atmosGasses[j]));

                                    input.RemoveGas(atmosGasses[j], molePerGas);
                                    output.AddGas(atmosGasses[j], molePerGas);
                                }
                            }
                        }
                    }
                }
            }

            // Update the animator
            anim.SetBool("scrubActive", scrubActive);
            anim.SetBool("deviceActive", deviceActive);
        }

        bool IsFiltered(AtmosGasses gas)
        {
            switch (gas)
            {
                case AtmosGasses.Oxygen:
                    return filterOxygen;
                case AtmosGasses.Nitrogen:
                    return filterNitrogen;
                case AtmosGasses.CarbonDioxide:
                    return filterCarbonDioxide;
                case AtmosGasses.Plasma:
                    return filterPlasma;
            }
            return false;
        }

        public void SetTileNeighbour(TileObject tile, int index)
        {
            tileNeighbours[index] = tile;
        }

        public void SetAtmosNeighbours()
        {
            int i = 0;
            foreach (TileObject tile in tileNeighbours)
            {
                if (tile != null)
                {
                    AtmosObject atmosObject = tile.atmos;
                    atmosNeighbours[i] = atmosObject;
                }
                i++;
            }
        }

        public IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = deviceActive ? "Stop scrubbber" : "Start scrubber", Interact = ActiveInteract, RangeCheck = true
                },
                new SimpleInteraction
                {
                    Name = siphonActive ? "Siphon mode" : "Scrubbing mode", Interact = ModeInteract, RangeCheck = true
                }
            };
        }

        private void ActiveInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            deviceActive = !deviceActive;
        }

        private void ModeInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            if (mode == OperatingMode.Scrubbing)
            {
                mode = OperatingMode.Siphoning;
                siphonActive = true;
            }
            else if (mode == OperatingMode.Siphoning)
            {
                mode = OperatingMode.Scrubbing;
                siphonActive = false;
            }
        }
    }
}