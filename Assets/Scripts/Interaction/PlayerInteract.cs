using System;
using Inventory.Custom;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Interaction
{
    [RequireComponent(typeof(Hands))]
    public class PlayerInteract : NetworkBehaviour
    {
        private new Camera camera;
        private Hands hands;
        
        private void Start()
        {
            if (!isLocalPlayer) Destroy(this);
            hands = GetComponent<Hands>();
        }

        protected virtual void Update()
        {
            if (!camera) camera = Camera.main;
            if (!camera) return;
            
            if (Input.GetMouseButtonDown(0))
            {
                var ray = camera.ScreenPointToRay(Input.mousePosition);

                // Ensure that user did not click the UI (fucking stupid that we need the event system to check this)
                if (!EventSystem.current.IsPointerOverGameObject() &&
                    Physics.Raycast(ray, out var hit, float.PositiveInfinity))
                {
                    var interactable = hit.transform.GetComponent<Interactable>();
                    if (interactable) interactable.Trigger(new InteractionEvent("pickup", hands.transform)
                        .WorldPosition(hit.point).WorldNormal(hit.normal));
                    
                    var held = hands.GetItemInHand();
                    if (held != null)
                    {
                        var heldInteractable = held.GetComponent<Interactable>();
                        if (heldInteractable) heldInteractable.Trigger(new InteractionEvent("use", hands.transform)
                            .WorldPosition(hit.point).WorldNormal(hit.normal).ForwardTo(interactable));
                    }
                }
            }
        }
    }
}