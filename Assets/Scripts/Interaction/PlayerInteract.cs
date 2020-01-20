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
                    Interact(hit.transform, hit.point, hit.normal);
            }
        }

        private void Interact(Transform target, Vector3 position, Vector3 normal)
        {
            var interactable = FindInteractable(target);
                    
            var held = hands.GetItemInHand();
            Interactable heldInteractable = null;
            if (held != null)
            {
                heldInteractable = FindInteractable(held.transform);
                if (heldInteractable) heldInteractable.Trigger(new InteractionEvent("use", hands.transform)
                    .WorldPosition(position).WorldNormal(normal).ForwardTo(interactable));
            }
            
            if (interactable) interactable.Trigger(new InteractionEvent("pickup", hands.transform)
                .WorldPosition(position).WorldNormal(normal).WaitFor(heldInteractable));
            if (interactable) interactable.Trigger(new InteractionEvent("open", hands.transform)
                .WorldPosition(position).WorldNormal(normal).WaitFor(heldInteractable));
        }

        private Interactable FindInteractable(Transform target)
        {
            while (target)
            {
                var result = target.GetComponent<Interactable>();
                if (result) return result;
                target = target.parent;
            }

            return null;
        }
    }
}