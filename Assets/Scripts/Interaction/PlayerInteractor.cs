using System;
using Interaction.Core;
using Inventory.Custom;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using Event = Interaction.Core.Event;

namespace Interaction
{
    [RequireComponent(typeof(Hands))]
    public class PlayerInteractor : NetworkBehaviour
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
                    Interact(hit.transform, hit.point, hit.normal);
            }
        }

        private void Interact(Transform target, Vector3 position, Vector3 normal)
        {
            var interactable = FindInteractable(target);
                    
            var held = hands.GetItemInHand();
            InteractionReceiver heldInteractionReceiver = null;
            if (held != null)
            {
                heldInteractionReceiver = FindInteractable(held.transform);
                if (heldInteractionReceiver) heldInteractionReceiver.Trigger(new Event("use", hands.gameObject)
                    .WorldPosition(position).WorldNormal(normal).ForwardTo(interactable));
            }
            
            if (interactable) interactable.Trigger(new Event("pickup", hands.gameObject)
                .WorldPosition(position).WorldNormal(normal).WaitFor(heldInteractionReceiver));
            if (interactable) interactable.Trigger(new Event("open", hands.gameObject)
                .WorldPosition(position).WorldNormal(normal).WaitFor(heldInteractionReceiver));
        }

        private InteractionReceiver FindInteractable(Transform target)
        {
            while (target)
            {
                var result = target.GetComponent<InteractionReceiver>();
                if (result) return result;
                target = target.parent;
            }

            return null;
        }
    }
}