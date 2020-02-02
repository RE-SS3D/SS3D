using Interaction.Core;
using Inventory;
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
            mainCamera = Camera.main;
        }

        protected virtual void Update()
        {
            if (!mainCamera) return;
            if (!Input.GetButtonDown("Click")) return;
            
            if (GetWorldData(out var hit))
            {
                Interact(hit.transform, hit.point, hit.normal);
            }
        }

        /// <summary>
        /// Get the current interaction point data
        /// </summary>
        /// <param name="hit">Result of the raycast</param>
        /// <returns></returns>
        public bool GetWorldData(out RaycastHit hit)
        {
            var raycastHit = new RaycastHit();
            // Ensure that user did not click the UI
            if (EventSystem.current.IsPointerOverGameObject())
            {
                hit = raycastHit;
                return false;
            }
            
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            //Ignore "Ignore Raycast" layer.
            LayerMask layerMask = ~(1 << LayerMask.NameToLayer ("Ignore Raycast"));
            return Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask);
        }

        /// <summary>
        /// Decrease the remaining supply for an item. Will most commonly happen during an interaction.
        /// </summary>
        /// <param name="itemWithSupply"></param>
        [Command]
        public void CmdDecreaseSupplyOfItem(GameObject itemWithSupply)
        {
            RpcDecreaseSupplyOfItem(itemWithSupply);
        }

        [ClientRpc]
        private void RpcDecreaseSupplyOfItem(GameObject itemWithSupply)
        {
            //Item probably already used up
            if (itemWithSupply == null) return;
            
            IItemWithSupply itemSupplyComponent = itemWithSupply.GetComponent<IItemWithSupply>();
            if (itemSupplyComponent == null)
            {
                Debug.LogError($"Could not find a IItemWithSupply component when attempting to decrease supply on {itemWithSupply}");
                return;
            }

            itemSupplyComponent.ChangeSupply(-itemSupplyComponent.GetSupplyDrainRate());
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
                        heldInteractionReceiver.Trigger(new InteractionEvent(kind, hands.gameObject, gameObject)
                            .WorldPosition(position).WorldNormal(normal).ForwardTo(interactable)
                            .RunWhile(e => Input.GetButton("Click")));
                    }
                }
            }
            
            if (interactable)
            {
                foreach (var kind in interactionsOnClick)
                {
                    interactable.Trigger(new InteractionEvent(kind, hands.gameObject, gameObject)
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