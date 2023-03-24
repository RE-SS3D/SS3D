using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    public readonly struct StoredItem : IEquatable<StoredItem>
    {
        public readonly ItemActor Item;
        public readonly Vector2Int Position;
        public readonly bool IsExcludedOfFreeAreaComputation;

        public StoredItem(ItemActor item, Vector2Int position, bool isExcludedOfFreeAreaComputation = false)
        {
            Item = item;
            Position = position;
            IsExcludedOfFreeAreaComputation = isExcludedOfFreeAreaComputation;
        }

        public bool Equals(StoredItem other)
        {
            return Equals(Item, other.Item) && Position.Equals(other.Position);
        }

        public override bool Equals(object obj)
        {
            return obj is StoredItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Item != null ? Item.GetHashCode() : 0) * 397) ^ Position.GetHashCode();
            }
        }
    }
}