using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Atmospherics;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SS3D.Content.Furniture.Machines.Atmospherics {
    // This handles the gas canister process, where we can put only Atmos stuff.
    // This can only store gas (for now), and we have a maximum ammount of gas that can be stored.
    // You can open the valve to release gas and you can also set the release pressure
    public class Canister : InteractionTargetBehaviour, IAtmosLoop
    {
	// An AtmosObject is in the object's tile
 	// we have the atmos properties, and we run all atmos within them
        AtmosObject currentAtmosObject;

	// The gas that is stored in this object, currently it only stores one
	// TODO: When we have gas mixing, we need to update this to a list
        [SerializeField] AtmosGasses gas = AtmosGasses.Oxygen;

	// The pressure the gas leaks when the valve is open
        [Range(0f, 20f)]
        [SerializeField] float valvePressure = 1;
        [SerializeField] bool valveOpen = false;

	// The gas ammount that can be stored
        [SerializeField] float content;
        [SerializeField] float maxContent;

	// The UI that will open once we interact with it
	// Currently unused
	// TODO: Remove the "Open Valve" interaction from the list
	// and put an "Open Menu" or "Manage" interaction
        [SerializeField] GameObject menuUIPrefab;
	// This is the UI that is currently open
        [SerializeField] CanisterUI canisterUI;

        private void Start()
        {
            // currentAtmosObject = transform.GetComponentInParent<TileObject>().atmos;
           
            // Big TODO: work on the UI panel because it's weird
            
            //canisterUI = Instantiate(menuUIPrefab).GetComponent<CanisterUI>();

            //TODO: replace with singleton variable sometime 
            //canisterUI.Init(GameObject.FindGameObjectWithTag("MainCanvas").GetComponent<Canvas>());
            //canisterUI.gameObject.SetActive(false);

            //canisterUI.label.text = transform.name;
        }

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
	    // TODO: Update the interaction names and remove the increase/decrease pressure
	    // Create a custom interaction for opening the menu
	    // put them in the UI
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = valveOpen ? "Close valve" : "Open valve", Interact = ValveInteract, RangeCheck = true
                },
                new SimpleInteraction
                {
                    Name = "Increase pressure", Interact = IncreasePressure, RangeCheck = true
                },
                new SimpleInteraction
                {
                    Name = "Decrease pressure", Interact = DecreasePressure, RangeCheck = true
                }
            };
        }

        private void IncreasePressure(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            valvePressure += 20;
        }
        private void DecreasePressure(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            if (valvePressure - 20 > 0)
            valvePressure -= 20;
        }

        private void ValveInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            valveOpen = !valveOpen;

            //canisterUI.gameObject.SetActive(true);
        }

        public void Initialize()
        {
            return;
        }

	// This handles the gas releasing
        public void Step()
        {
	    // if there's an atmos object, if we still have gas, and the valve is open
            if (currentAtmosObject != null && content - valvePressure > 0 && valvePressure > 0 && valveOpen)
            {
		// we add gas into the AtmosObject
                // currentAtmosObject.AddGas(gas, valvePressure);

                //canisterUI.releasePressure.text = valvePressure.ToString();
                //canisterUI.pressure.text = content.ToString();

                content -= valvePressure / 10;
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
