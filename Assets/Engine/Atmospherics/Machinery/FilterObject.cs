using System;
using System.Collections;
using System.Collections.Generic;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class FilterObject : PipeGeneric, IAtmosLoop, IInteractionTarget
    {
        public float MaxPressure = 4500f;
        public float TargetPressure = 101f;

        public bool filterOxygen = false;
        public bool filterNitrogen = false;
        public bool filterCarbonDioxide = false;
        public bool filterPlasma = false;

        public bool filterActive = false;

        private float _targetPressure;

        // Update is called once per frame
        void Update()
        {
            if (_targetPressure != TargetPressure)
            {
                _targetPressure = Mathf.Clamp(TargetPressure, 0, MaxPressure);
                TargetPressure = _targetPressure;
            }
        }
        public void SetActive(bool filterActive)
        {
            this.filterActive = filterActive;
        }

        public void Initialize()
        {
            SetAtmosNeighbours();
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

        public void Step()
        {
            if (filterActive)
            {
                PipeObject input = atmosNeighbours[0];
                PipeObject outputFiltered = atmosNeighbours[3];
                PipeObject outputOther = atmosNeighbours[1];

                // Return when there is no gas
                if (input == null || input.GetTotalMoles() <= 1f || outputFiltered == null || outputOther == null)
                    return;

                AtmosContainer inputContainer = input.GetAtmosContainer();

                // Both outputs must not be blocked
                if (outputFiltered.GetPressure() <= _targetPressure && outputOther.GetPressure() <= _targetPressure)
                {
                    // Use the pipe with the highest pressure as reference
                    PipeObject nearestOutput = (outputFiltered.GetPressure() > outputOther.GetPressure()) ? outputFiltered : outputOther;
                    
                    // Calculate necessary moles to transfer using PV=nRT
                    float pressureDifference = _targetPressure - nearestOutput.GetPressure();
                    float transferMoles = pressureDifference * 1000 * nearestOutput.volume / (nearestOutput.GetAtmosContainer().GetTemperature() * Gas.gasConstant);

                    // We can not transfer more moles than the machinery allows
                    transferMoles = Mathf.Min(Gas.maxMoleTransfer, transferMoles);

                    // We can't transfer more moles than there are in the input
                    if (transferMoles > input.GetTotalMoles())
                        transferMoles = input.GetTotalMoles();

                    // for (int i = 0; i < Gas.numOfGases; i++)
                    foreach (AtmosGasses gas in Enum.GetValues(typeof(AtmosGasses)))
                    {
                        // Divide the moles according to their percentage
                        float molePerGas = (inputContainer.GetGas(gas) / input.GetTotalMoles()) * transferMoles;
                        if (inputContainer.GetGas(gas) > 0f)
                        {
                            input.RemoveGas(gas, molePerGas);

                            // Determine output based on filtering setting
                            if (IsFiltered(gas))
                                outputFiltered.AddGas(gas, molePerGas);
                            else
                                outputOther.AddGas(gas, molePerGas);
                        }
                    }
                }
            }
        }



        public IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = filterActive ? "Stop filter" : "Start filter", Interact = FilterInteract, RangeCheck = true
                }
            };
        }
        private void FilterInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            SetActive(!filterActive);
        }
    }
}