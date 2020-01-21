using System;
using Interaction.Core;
using Inventory.Custom;
using UnityEngine;

namespace Interaction
{
    [RequireComponent(typeof(Interactable))]
    public class Placeable : MonoBehaviour, IInteractable
    {
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen("use");
        }

        public bool Handle(InteractionEvent e)
        {
            if (!e.forwardTo) return false;
            e.forwardTo.Trigger(e.Forward("place", transform));

            return true;
        }
    }
}