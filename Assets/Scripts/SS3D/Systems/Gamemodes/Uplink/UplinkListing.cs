using System;
using System.Collections.Generic;
using SS3D.Data.AssetDatabases;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Uplink
{
    /// <summary>
    /// One thing an uplink sells (antagonist-content.md §4, §6 later pass). Authored in an <see cref="UplinkCatalog"/>
    /// asset; the items are <see cref="ObjectAssetReference"/>s, so renaming a prefab can't break a listing.
    /// </summary>
    [Serializable]
    public sealed class UplinkListing
    {
        /// <summary>A <see cref="SquadStock"/> of this value means the listing never runs out.</summary>
        public const int Unlimited = -1;

        [SerializeField]
        [Tooltip("Stable key: squad stock and tests use it. Unique within the catalog.")]
        private string _id;

        [SerializeField]
        [Tooltip("Tab this listing appears under; one of the catalog's categories.")]
        private string _category;

        [SerializeField]
        private string _name;

        [SerializeField]
        [Min(1)]
        [Tooltip("Price in telecrystals.")]
        private int _price = 1;

        [SerializeField]
        [Tooltip("Short type line under the name, e.g. \"Thrown · short fuse\".")]
        private string _kind;

        [SerializeField]
        [TextArea(2, 5)]
        [Tooltip("What it does in play.")]
        private string _description;

        [SerializeField]
        [Tooltip("Items delivered, in order: the first goes to the free hand, the rest to the holder's feet if the hands are full.")]
        private List<ObjectAssetReference> _items = new();

        [SerializeField]
        [Tooltip("\"Includes\" fact line; empty when the listing is just its item.")]
        private string _includes;

        [SerializeField]
        [Tooltip("How many the whole squad may buy per round; -1 for unlimited.")]
        private int _squadStock = Unlimited;

        [SerializeField]
        [Tooltip("Restriction badge text, e.g. \"One per squad\"; empty for none.")]
        private string _restriction;

        [SerializeField]
        private bool _bundle;

        public UplinkListing(
            string id,
            string category,
            string name,
            int price,
            string kind,
            string description,
            IReadOnlyList<ObjectAssetReference> items,
            string includes = null,
            int squadStock = Unlimited,
            string restriction = null,
            bool bundle = false)
        {
            _id = id;
            _category = category;
            _name = name;
            _price = price;
            _kind = kind;
            _description = description;
            _items = items != null ? new List<ObjectAssetReference>(items) : new List<ObjectAssetReference>();
            _includes = includes;
            _squadStock = squadStock;
            _restriction = restriction;
            _bundle = bundle;
        }

        public string Id => _id ?? string.Empty;

        public string Category => _category ?? string.Empty;

        public string Name => _name ?? string.Empty;

        /// <summary>Price in telecrystals.</summary>
        public int Price => _price;

        public string Kind => _kind ?? string.Empty;

        public string Description => _description ?? string.Empty;

        /// <summary>The items delivered, in delivery order (first goes to the free hand).</summary>
        public IReadOnlyList<ObjectAssetReference> Items => _items;

        /// <summary>Asset id of the first item: the listing's icon. Empty when it has none.</summary>
        public string IconItemId => _items != null && _items.Count > 0 && _items[0] != null ? _items[0].Id : string.Empty;

        public string Includes => _includes ?? string.Empty;

        /// <summary>How many the whole squad may buy this round, or <see cref="Unlimited"/>.</summary>
        public int SquadStock => _squadStock;

        public bool HasSquadStock => _squadStock != Unlimited;

        public string Restriction => _restriction ?? string.Empty;

        public bool Bundle => _bundle;
    }
}
