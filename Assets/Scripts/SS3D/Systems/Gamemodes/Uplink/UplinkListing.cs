using System;
using System.Collections.Generic;

namespace SS3D.Systems.Gamemodes.Uplink
{
    /// <summary>
    /// One thing the operative uplink sells (antagonist-content.md §4, §6 later pass). Pure data: the items are
    /// named by prefab name and resolved to asset ids on the server when bought.
    /// </summary>
    public sealed class UplinkListing
    {
        /// <summary>A <see cref="SquadStock"/> of this value means the listing never runs out.</summary>
        public const int Unlimited = -1;

        public UplinkListing(
            string id,
            string category,
            string name,
            int price,
            string kind,
            string description,
            IReadOnlyList<string> itemNames,
            string includes = null,
            int squadStock = Unlimited,
            string restriction = null,
            bool bundle = false)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A listing needs an id", nameof(id));
            if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price), "A listing costs at least 1 TC");
            if (itemNames == null || itemNames.Count == 0) throw new ArgumentException("A listing delivers at least one item", nameof(itemNames));

            Id = id;
            Category = category;
            Name = name;
            Price = price;
            Kind = kind ?? string.Empty;
            Description = description ?? string.Empty;
            ItemNames = itemNames;
            Includes = includes ?? string.Empty;
            SquadStock = squadStock;
            Restriction = restriction ?? string.Empty;
            Bundle = bundle;
        }

        public string Id { get; }

        public string Category { get; }

        public string Name { get; }

        /// <summary>Price in telecrystals.</summary>
        public int Price { get; }

        /// <summary>Short type line under the name, e.g. "Thrown · short fuse".</summary>
        public string Kind { get; }

        /// <summary>What it does in play.</summary>
        public string Description { get; }

        /// <summary>Prefab names of the items delivered, in delivery order (first goes to the free hand).</summary>
        public IReadOnlyList<string> ItemNames { get; }

        /// <summary>"Includes" fact line; empty when the listing is just its item.</summary>
        public string Includes { get; }

        /// <summary>How many the whole squad may buy this round, or <see cref="Unlimited"/>.</summary>
        public int SquadStock { get; }

        public bool HasSquadStock => SquadStock != Unlimited;

        /// <summary>Restriction badge text, e.g. "One per squad"; empty for none.</summary>
        public string Restriction { get; }

        public bool Bundle { get; }
    }
}
