using System.Collections.Generic;

namespace SS3D.Systems.Gamemodes.Uplink
{
    /// <summary>
    /// The Nuclear Operative uplink's representative catalog (antagonist-content.md §4, §11: a content and
    /// balancing pass, not a fixed list). Only items that already exist in the game are sold; categories with
    /// nothing to sell yet (ammo, deception) are left out until their items land.
    /// </summary>
    public static class OperativeUplinkCatalog
    {
        /// <summary>Telecrystals each operative's uplink starts the round with.</summary>
        public const int StartingBalance = 40;

        public const string Weapons = "Weapons";
        public const string Explosives = "Explosives";
        public const string Wearables = "Wearables";
        public const string Medical = "Medical";

        /// <summary>Tab order.</summary>
        public static readonly IReadOnlyList<string> Categories = new[] { Weapons, Explosives, Wearables, Medical };

        public static readonly IReadOnlyList<UplinkListing> Listings = new[]
        {
            new UplinkListing("c20r", Weapons, "C-20r SMG", 14, "Ranged · SMG",
                "Compact Syndicate submachine gun. Fires fast, short bursts; deadly at close range.",
                new[] { "C20R" }),
            new UplinkListing("stechkin", Weapons, "Stechkin pistol", 6, "Ranged · pistol · one-handed",
                "Small Syndicate sidearm. Fits a bag or pocket, and leaves a hand free.",
                new[] { "Stechkin" }),

            new UplinkListing("c4", Explosives, "C4 charge", 2, "Placed · timed",
                "Plastic explosive. Stick it to a wall or door, it arms with a fifteen-second fuse. Breaches one tile.",
                new[] { "C4" }),
            new UplinkListing("x4", Explosives, "X4 charge", 4, "Placed · timed · directional",
                "Directional breaching charge. Put on a wall or door, it blasts through to the far side.",
                new[] { "X4" }),
            new UplinkListing("minibomb", Explosives, "Syndicate minibomb", 6, "Thrown · short fuse",
                "Throwable bomb on a five-second fuse. Tears open hull and floor, so expect vacuum afterwards. Cannot be defused.",
                new[] { "SyndicateMiniBomb" }),
            new UplinkListing("synbomb", Explosives, "Syndicate bomb", 11, "Deployed · long countdown",
                "Large bomb with a long, audible countdown. Comes as a beacon: use it in hand where the bomb should land. Bolt it down and it levels a department if nobody defuses it.",
                new[] { "SyndicateBombBeacon" },
                includes: "Delivery beacon",
                squadStock: 1,
                restriction: "One per squad"),

            new UplinkListing("hardsuit", Wearables, "Syndicate hardsuit", 7, "Outer suit · spaceproof",
                "Armoured spacesuit. Protects in vacuum and against most damage.",
                new[] { "SyndicateHardsuit", "SyndicateHardsuitHelmet" },
                includes: "Suit + helmet"),
            new UplinkListing("magboots", Wearables, "Magboots", 2, "Shoes",
                "Keep you on your feet in zero gravity and when a breach starts pulling the room out.",
                new[] { "SyndicateMagboots" }),
            new UplinkListing("vest", Wearables, "Syndicate armour vest", 3, "Armour · chest",
                "Light armour vest. Takes the edge off bullets and blades without slowing you down.",
                new[] { "ArmorVestSyndicate" }),

            new UplinkListing("gauze", Medical, "Gauze", 1, "Medical · bandage",
                "Stops bleeding from a wound.",
                new[] { "Gauze" }),
            new UplinkListing("brutepatch", Medical, "Brute patch", 1, "Medical · patch",
                "Heals bruises and cuts.",
                new[] { "BrutePatch" }),
            new UplinkListing("burnpatch", Medical, "Burn patch", 1, "Medical · patch",
                "Heals burns.",
                new[] { "BurnPatch" }),
            new UplinkListing("defib", Medical, "Defibrillator", 5, "Medical · tool",
                "Restarts a stopped heart, if the body is still in one piece.",
                new[] { "PortableDefibrillator" }),
        };

        /// <summary>The listing at <paramref name="index"/> of <see cref="Listings"/>, or null.</summary>
        public static UplinkListing At(int index) =>
            index >= 0 && index < Listings.Count ? Listings[index] : null;
    }
}
