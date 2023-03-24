using SS3D.Data.Enums;
using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    public class ItemActor
    {
        public ItemId ItemId;

        /// <summary>
        /// The item's name in the UI
        /// </summary>
        public string Name;

        /// <summary>
        /// The item's relative weight in kilograms.
        /// </summary>
        public float Weight;

        /// <summary>
        /// The amount of slots the item will take in a container
        /// </summary>
        public Vector2Int Size;

        /// <summary>
        /// The sprite that is shown in the container slot
        /// </summary>
        public Sprite Sprite;

        /// <summary>
        /// The list of characteristics this Item has
        /// </summary>
        public List<Trait> Traits;

        /// <summary>
        /// The container the item is currently stored on
        /// </summary>
        public Container Container;

        public bool IsOnContainer()
        {
            return Container != null;
        }

        public bool HasTrait(Trait trait)
        {
            return Traits.Contains(trait);
        }
    }
}