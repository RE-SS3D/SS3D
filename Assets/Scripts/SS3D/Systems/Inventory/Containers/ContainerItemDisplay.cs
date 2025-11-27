using Coimbra;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// This allows control over the position of displayed items inside the container.
    /// It also allows to define multiple points where items can be displayed inside the container,
    /// and items placed in the container appears at those different points in the order defined.
    /// Take for example a battery compartment, battery should appear side by side when placed inside the compartment container.
    /// Without this they would pile up in the same spot.
    /// </summary>
    [RequireComponent(typeof(AttachedContainer))]
    public class ContainerItemDisplay : MonoBehaviour
    {
        [Tooltip(" The list of transforms defining where the items are displayed.")]
        [SerializeField]
        private Transform[] _displays;

        private AttachedContainer _attachedContainer;

        /// <summary>
        /// The list of items displayed in the container;
        /// </summary>
        private Item[] _displayedItems;

        private int NumberDisplay => _displays.Length;

        protected void Awake()
        {
            _attachedContainer = GetComponent<AttachedContainer>();
            _displayedItems = new Item[NumberDisplay];
            _attachedContainer.OnClientContentsChanged += HandleContainerContentChanged;
        }

        protected void OnDestroy()
        {
            _attachedContainer.OnClientContentsChanged -= HandleContainerContentChanged;
        }

        private void HandleContainerContentChanged(AttachedContainer container, Item olditem, Item newitem, ContainerChangeType type)
        {
            if (type == ContainerChangeType.Add)
            {
                ContainerOnItemAttached(newitem);
            }

            if (type == ContainerChangeType.Remove)
            {
                ContainerOnItemDetached(olditem);
            }
        }

        private void ContainerOnItemAttached(Item item)
        {
            // Defines the transform of the item to be the first available position.
            int index = -1;
            for (int i = 0; i < NumberDisplay; i++)
            {
                if (_displayedItems[i] == null)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                return;
            }

            Transform itemTransform = item.transform;
            itemTransform.SetParent(_displays[index].transform, false);
            itemTransform.localPosition = Vector3.zero;
            itemTransform.localRotation = Quaternion.identity;
        }

        private void ContainerOnItemDetached(Item item)
        {
            int index = -1;
            for (int i = 0; i < NumberDisplay; i++)
            {
                if (_displayedItems[i] == item)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                return;
            }

            Transform itemParent = item.transform.parent;
            if (itemParent != null && itemParent != _displays[index])
            {
                item.transform.SetParent(null, true);

                // It's currently deleting the game object containing the item, why is this here ?
                itemParent.gameObject.Dispose(true);
            }

            _displayedItems[index] = null;
        }
    }
}
