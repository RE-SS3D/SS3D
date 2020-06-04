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
    public class Canister : InteractionTargetBehaviour
    {
        AtmosObject currentAtmosObject;
        [SerializeField] AtmosGasses gas = AtmosGasses.Oxygen;

        [Range(0f, 20f)]
        [SerializeField] float valvePressure = 1;
        [SerializeField] bool valveOpen = true;

        [SerializeField] float content;
        [SerializeField] float maxContent;

        [SerializeField] GameObject menuUIPrefab;
        [SerializeField] CanisterUI canisterUI;

        private void Start()
        {
            currentAtmosObject = transform.GetComponentInParent<TileObject>().atmos;
            canisterUI = Instantiate(menuUIPrefab).GetComponent<CanisterUI>();

            //TODO: replace with singleton variable sometime 
            canisterUI.Init(GameObject.FindGameObjectWithTag("MainCanvas").GetComponent<Canvas>());
            canisterUI.gameObject.SetActive(false);

            canisterUI.label.text = transform.name;
        }

        private void FixedUpdate()
        {
            if (currentAtmosObject != null && content - valvePressure > 0 && valvePressure > 0 && valveOpen)
            {
                currentAtmosObject.AddGas(gas, valvePressure);
                canisterUI.releasePressure.text = valvePressure.ToString();
                canisterUI.pressure.text = content.ToString();
                
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
            canisterUI.gameObject.SetActive(true);
        }
    }
}
