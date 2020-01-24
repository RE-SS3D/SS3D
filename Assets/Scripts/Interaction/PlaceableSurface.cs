using System;
using Interaction.Core;
using Inventory.Custom;
using UnityEngine;

namespace Interaction
{
    [RequireComponent(typeof(InteractionReceiver))]
    public class PlaceableSurface : MonoBehaviour, IInteraction
    {
        [SerializeField] private InteractionKind kind = null;
        
        public void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(kind);
        }

        public bool Handle(InteractionEvent e)
        {
            if (e.sender == null)
            {
                Debug.LogWarning($"Tried to place null on a surface ({name})");
                return false;
            }

            var item = e.sender.GetComponent<Item>();
            if (item == null)
            {
                Debug.LogWarning($"Object ({e.sender.name}) being placed on a surface ({name}) does not have an item component");
                return false;
            }

            item.container.RemoveItem(item.gameObject);
            item.transform.position = e.worldPosition;
            item.gameObject.SetActive(true);

            return true;
        }
    }
}