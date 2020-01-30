using System;
using Interaction.Core;
using Inventory.Custom;
using UnityEngine;

namespace Interaction
{
    [RequireComponent(typeof(InteractionReceiver))]
    public class PlaceableSurface : MonoBehaviour, ISingularInteraction
    {
        [SerializeField] private InteractionKind kind = null;
        
        public void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(kind);
        }

        public void Reset() { }

        public bool Handle(InteractionEvent e)
        {
            if (e.sender == null)
            {
                Debug.LogWarning($"Tried to place null on a surface ({name})");
                return false;
            }
            var itemCollider = e.sender.GetComponent<Collider>();
            if (itemCollider == null)
            {
                Debug.LogWarning($"Object ({e.sender.name}) being placed on a surface ({name}) does not have a collider component");
                return false;
            }

            var playerHands = e.player.GetComponent<Hands>();
            if (playerHands == null)
            {
                Debug.LogWarning($"Player holding object ({e.sender.name}) being placed on a surface ({name}) does not have a hands component");
                return false;
            }
            
            Vector3 position = e.worldPosition + Vector3.up * itemCollider.bounds.max.y;
            playerHands.Place(position, Quaternion.identity);

            return true;
        }
    }
}