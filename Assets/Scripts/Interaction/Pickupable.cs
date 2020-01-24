using System;
using Interaction.Core;
using Inventory.Custom;
using UnityEngine;

namespace Interaction
{
    public class Pickupable : MonoBehaviour, ISingularInteraction
    {
        [SerializeField] private InteractionKind kind = null;
        
        public void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(kind);
        }

        public bool Handle(InteractionEvent e)
        {
            e.sender.GetComponent<Hands>().Pickup(gameObject);

            return true;
        }
    }
}