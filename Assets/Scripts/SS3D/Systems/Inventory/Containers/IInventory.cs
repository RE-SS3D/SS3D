using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.UI
{
    public interface IInventory
    {
        public delegate void ContainerContentsEventHandler(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type);

        public event ContainerContentsEventHandler OnContainerContentChanged;

        public List<AttachedContainer> Containers { get; }

        public void Init();

        public void ClientDropItem(Item item);

        public void AddContainer(AttachedContainer container);

        public void RemoveContainer(AttachedContainer container);

        public bool TryGetTypeContainer(ContainerType type, int position, out AttachedContainer typeContainer);

        public void ClientInteractWithContainerSlot(AttachedContainer container, Vector2Int position);

        public void ClientTransferItem(Item item, Vector2Int position, AttachedContainer targetContainer);
    }
}
