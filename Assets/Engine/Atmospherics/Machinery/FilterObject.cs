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




        // Start is called before the first frame update
        void Start()
        {

        }

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

        public void Step()
        {
            if (filterActive)
            {
                PipeObject input = atmosNeighbours[0];
                PipeObject outputFiltered = atmosNeighbours[3];
                PipeObject outputOther = atmosNeighbours[1];

                // Return when there is no gas
                if (input.GetTotalMoles() <= 1f)
                    return;

                //if (outputFiltered.GetPressure() <= _targetPressure && outputOther.GetPressure() <= _targetPressure)
                //{
                //    float totalMoles = Mathf.Max(outputFiltered.GetTotalMoles(), outputOther.GetTotalMoles());
                //    // Calculate necessary moles to transfer using PV=nRT
                //    float pressureDifference = _targetPressure - outputPressure;
                //    float transferMoles = pressureDifference * 1000 * output.volume / (output.GetAtmosContainer().GetTemperature() * Gas.gasConstant);
                //}
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