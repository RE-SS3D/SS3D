using Interaction.Core;
using Inventory.Custom;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Interaction
{
    [RequireComponent(typeof(Hands))]
    public class PlayerInteractor : NetworkBehaviour
    {
        [Tooltip("The interaction events to be sent to the object that is clicked")]
        [SerializeField] private InteractionKind[] interactionsOnClick = new InteractionKind[0];
        [Tooltip("The interaction events to be sent to the held object when anything is clicked")]
        [SerializeField] private InteractionKind[] heldInteractionsOnClick = new InteractionKind[0];
        
        private Camera mainCamera;
        private Hands hands;
        
        private void Start()
        {
            if (!isLocalPlayer) Destroy(this);
            hands = GetComponent<Hands>();
            if (!mainCamera) mainCamera = Camera.main;
        }

        protected virtual void Update()
        {
            if (!mainCamera) return;
            
            if (Input.GetMouseButtonDown(0))
            {
                var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                // Ensure that user did not click the UI and that we hit something
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
                if (heldInteractionReceiver)
                {
                    foreach (var kind in heldInteractionsOnClick)
                    {
                        heldInteractionReceiver.Trigger(new InteractionEvent(kind, hands.gameObject)
                            .WorldPosition(position).WorldNormal(normal).ForwardTo(interactable)
                            .RunWhile(e => transform.position.x < 0));
                    }
                }
            }
            
            if (interactable)
            {
                foreach (var kind in interactionsOnClick)
                {
                    interactable.Trigger(new InteractionEvent(kind, hands.gameObject)
                        .WorldPosition(position).WorldNormal(normal).WaitFor(heldInteractionReceiver));
                }
            }
        }

        private static InteractionReceiver FindInteractable(Transform target)
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