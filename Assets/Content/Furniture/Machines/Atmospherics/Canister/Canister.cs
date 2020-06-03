using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Atmospherics;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SS3D.Content.Furniture.Machines.Atmospherics {
    public class Canister : InteractionTargetBehaviour
    {
        AtmosObject currentAtmosObject;
        [SerializeField] AtmosGasses gas = AtmosGasses.Oxygen;

        [Range(0f, 20f)]
        [SerializeField] float valvePressure = 1;
        [SerializeField] bool valveOpen = true;

        [SerializeField] float content;
        [SerializeField] float maxContent;

        private void Start()
        {
            currentAtmosObject = transform.GetComponentInParent<TileObject>().atmos;
        }

        private void FixedUpdate()
        {
            if (currentAtmosObject != null && content - valvePressure > 0 && valvePressure > 0)
            {
                currentAtmosObject.AddGas(gas, valvePressure);
                content -= valvePressure / 10;
            }
        }

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = "Valve", Interact = ValveInteract, RangeCheck = true
                }
            };
        }


        private void ValveInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {

        }
    }
}
