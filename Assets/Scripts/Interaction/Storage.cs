using System;
using Interaction.Core;
using UnityEngine;
using Event = Interaction.Core.Event;

namespace Interaction
{
    [RequireComponent(typeof(InteractionReceiver))]
    [RequireComponent(typeof(Container))]
    public class Storage : MonoBehaviour, IInteraction
    {
        public bool onlyIfOpen;

        private Container container;
        private Openable openable;
        
        private void Start()
        {
            container = GetComponent<Container>();
            openable = GetComponent<Openable>();
        }

        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen("store");
            blocks("open");
        }

        public bool Handle(Event e)
        {
            if (onlyIfOpen)
            {
                if (openable == null) Debug.LogWarning("Trying to check for a Openable that has not been added");
                else if (!openable.open) return false;
            }
            
            var item = e.sender.GetComponent<Item>();
            item.container.RemoveItem(item.gameObject);
            container.AddItem(item.gameObject);

            return true;
        }
    }
}
