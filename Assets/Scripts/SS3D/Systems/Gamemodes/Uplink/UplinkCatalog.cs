using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Uplink
{
    /// <summary>
    /// What an uplink sells and what it starts with (antagonist-content.md §4, §11: a content and balancing pass,
    /// not a fixed list). Edit the asset to change prices, stock or listings; no code change is needed. Each uplink
    /// device references its catalog, so a later traitor uplink can sell a different list. Listings go over the
    /// network by index, so server and clients must run the same build of the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "SS3D/Gamemodes/Uplink Catalog", fileName = "UplinkCatalog")]
    public sealed class UplinkCatalog : ScriptableObject
    {
        /// <summary>Telecrystals an uplink starts with when nothing sets another balance.</summary>
        public const int DefaultStartingBalance = 40;

        [SerializeField]
        [Min(0)]
        [Tooltip("Telecrystals each uplink using this catalog starts the round with.")]
        private int _startingBalance = DefaultStartingBalance;

        [SerializeField]
        [Tooltip("Tab order. Every listing's category must be one of these.")]
        private List<string> _categories = new();

        [SerializeField]
        private List<UplinkListing> _listings = new();

        public int StartingBalance => _startingBalance;

        public IReadOnlyList<string> Categories => _categories;

        public IReadOnlyList<UplinkListing> Listings => _listings;

        /// <summary>The listing at <paramref name="index"/>, or null.</summary>
        public UplinkListing At(int index) =>
            index >= 0 && index < _listings.Count ? _listings[index] : null;

        /// <summary>Index of the listing with <paramref name="id"/>, or -1.</summary>
        public int IndexOf(string id)
        {
            for (int i = 0; i < _listings.Count; i++)
            {
                if (_listings[i] != null && _listings[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Authoring mistakes in the asset, one line each; empty when the catalog is sound.</summary>
        public List<string> Validate(int maxListings, int maxCategories)
        {
            List<string> problems = new();
            if (_listings.Count > maxListings)
            {
                problems.Add($"{_listings.Count} listings; the panel sends at most {maxListings}");
            }

            if (_categories.Count > maxCategories)
            {
                problems.Add($"{_categories.Count} categories; the panel sends at most {maxCategories}");
            }

            HashSet<string> ids = new();
            HashSet<string> used = new();
            for (int i = 0; i < _listings.Count; i++)
            {
                UplinkListing listing = _listings[i];
                string where = listing == null || string.IsNullOrEmpty(listing.Id) ? $"listing {i}" : listing.Id;
                if (listing == null)
                {
                    problems.Add(where + ": empty entry");
                    continue;
                }

                if (string.IsNullOrEmpty(listing.Id)) problems.Add(where + ": no id");
                else if (!ids.Add(listing.Id)) problems.Add(where + ": duplicate id");
                if (string.IsNullOrEmpty(listing.Name)) problems.Add(where + ": no name");
                if (listing.Price <= 0) problems.Add(where + ": costs nothing");
                if (!_categories.Contains(listing.Category)) problems.Add($"{where}: unknown category '{listing.Category}'");
                if (listing.SquadStock < UplinkListing.Unlimited) problems.Add(where + ": squad stock below -1");
                if (listing.Items.Count == 0) problems.Add(where + ": delivers nothing");
                for (int j = 0; j < listing.Items.Count; j++)
                {
                    if (listing.Items[j] == null) problems.Add($"{where}: item {j} is empty");
                }

                used.Add(listing.Category);
            }

            foreach (string category in _categories)
            {
                if (!used.Contains(category)) problems.Add($"category '{category}' has no listings");
            }

            return problems;
        }

        /// <summary>A catalog built in code, for tests.</summary>
        public static UplinkCatalog Create(int startingBalance, IEnumerable<string> categories, IEnumerable<UplinkListing> listings)
        {
            UplinkCatalog catalog = CreateInstance<UplinkCatalog>();
            catalog._startingBalance = startingBalance;
            catalog._categories = new List<string>(categories);
            catalog._listings = new List<UplinkListing>(listings);
            return catalog;
        }
    }
}
