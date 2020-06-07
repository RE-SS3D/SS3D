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
    public class Canister : InteractionTargetBehaviour, IAtmosLoop
    {
        AtmosObject currentAtmosObject;
        [SerializeField] AtmosGasses gas = AtmosGasses.Oxygen;

        [Range(0f, 20f)]
        [SerializeField] float valvePressure = 1;
        [SerializeField] bool valveOpen = false;

        [SerializeField] float content;
        [SerializeField] float maxContent;

        [SerializeField] GameObject menuUIPrefab;
        [SerializeField] CanisterUI canisterUI;

        private void Start()
        {
            currentAtmosObject = transform.GetComponentInParent<TileObject>().atmos;
           
            // Big TODO: work on the UI panel because it's weird
            
            //canisterUI = Instantiate(menuUIPrefab).GetComponent<CanisterUI>();

           //TODO: replace with singleton variable sometime 
            //canisterUI.Init(GameObject.FindGameObjectWithTag("MainCanvas").GetComponent<Canvas>());
            //canisterUI.gameObject.SetActive(false);

            //canisterUI.label.text = transform.name;
        }

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
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

        public void Step()
        {
            if (currentAtmosObject != null && content - valvePressure > 0 && valvePressure > 0 && valveOpen)
            {
                currentAtmosObject.AddGas(gas, valvePressure);
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
