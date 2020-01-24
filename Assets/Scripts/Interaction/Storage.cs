using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction
{
    [RequireComponent(typeof(InteractionReceiver))]
    [RequireComponent(typeof(Container))]
    public class Storage : MonoBehaviour, IInteraction
    {
        [SerializeField] private InteractionKind kind = null;
        [SerializeField] private InteractionKind block = null;
        
        public bool onlyIfOpen;

        private Container container;
        private Openable openable;
        
        private void Start()
        {
            container = GetComponent<Container>();
            openable = GetComponent<Openable>();
        }

        public void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(kind);
            blocks(block);
        }

        public bool Handle(InteractionEvent e)
        {
            if (onlyIfOpen)
            {
                if (openable == null) Debug.LogWarning("Trying to check for a Openable that has not been added");
                else if (!openable.open) return false;
            }

            if (e.sender == null)
            {
                Debug.LogWarning($"Trying to store null in storage ({name})");
                return false;
            }

            var item = e.sender.GetComponent<Item>();
            if (item == null)
            {
                Debug.LogWarning(
                    $"Object ({e.sender.name}) getting stored in storage ({name}) has no item component");
                return false;
            }

            item.container.RemoveItem(item.gameObject);
            container.AddItem(item.gameObject);

            return true;
        }
    }
}
