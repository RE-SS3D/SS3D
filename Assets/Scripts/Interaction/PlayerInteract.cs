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
                    var activeSender = hands.GetItemInHand();
                    
                    var interactable = hit.transform.GetComponent<Interactable>();
                    if (interactable) interactable.Trigger(new InteractionEvent(
                        InteractionKind.Click, 
                        activeSender ? activeSender.transform : transform, 
                        activeSender ? hands.transform : null)
                    {
                        worldPosition = hit.point,
                        worldNormal = hit.normal,
                    });
                }
            }
        }
    }
}