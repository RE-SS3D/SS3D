#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Single source of truth for item size tier and weight (plans/item-size-weight-balance.plan.md).
    /// Edit this table, not the prefabs: <see cref="ItemSizeWeightRecipe"/> reapplies it on every run
    /// and <c>ItemSizeWeightTableTests</c> fails when a prefab drifts. Weight is roughly kilograms,
    /// always a multiple of 0.1 (the storage panel shows one decimal), and is per unit for stacks.
    /// Tiers follow Documents/design/inventory-storage.md §4.
    /// </summary>
    public static class ItemSizeWeightTable
    {
        public const string ItemsRoot = "Assets/Content/WorldObjects/Items/";

        public readonly struct Entry
        {
            public readonly string Path;
            public readonly SizeClass Size;
            public readonly float Weight;

            public Entry(string path, SizeClass size, float weight)
            {
                Path = path;
                Size = size;
                Weight = weight;
            }

            /// <summary>Asset path of the prefab, e.g. <c>Assets/Content/WorldObjects/Items/Clothing/Backpack.prefab</c>.</summary>
            public string PrefabPath => ItemsRoot + Path + ".prefab";
        }

        /// <summary>Grid, ceiling and readout for a container that is not owned by <see cref="StorageContainerPrefabSetup"/>
        /// because its open/close flags must stay as authored (medkit, toolbox).</summary>
        public readonly struct ContainerEntry
        {
            public readonly string Path;
            public readonly Vector2Int Size;
            public readonly SizeClass MaxSizeClass;
            public readonly float MaxWeight;

            public ContainerEntry(string path, int width, int height, SizeClass maxSizeClass, float maxWeight)
            {
                Path = path;
                Size = new Vector2Int(width, height);
                MaxSizeClass = maxSizeClass;
                MaxWeight = maxWeight;
            }

            public string PrefabPath => ItemsRoot + Path + ".prefab";
        }

        /// <summary>Item prefabs under <see cref="ItemsRoot"/> that deliberately have no entry.</summary>
        public static readonly IReadOnlyList<string> Exempt = new[]
        {
            "Doodad",
        };

        public static readonly IReadOnlyList<Entry> Entries = new Entry[]
        {
            new("Functional/Generic/Identification/IDCard", SizeClass.Tiny, 0.1f),
            new("Functional/Generic/Identification/SecurityIDCard", SizeClass.Tiny, 0.1f),
            new("Functional/Tools/Command/NuclearAuthenticationDisk", SizeClass.Tiny, 0.1f),
            new("Consumable/Chemical/Medications/BrutePatch", SizeClass.Tiny, 0.1f),
            new("Consumable/Chemical/Medications/BurnPatch", SizeClass.Tiny, 0.1f),
            new("Consumable/Chemical/Medications/MedicalPatch", SizeClass.Tiny, 0.1f),
            new("Consumable/Food/General/Snacks/DonkPocket", SizeClass.Tiny, 0.2f),
            new("Functional/Materials/GlassShard", SizeClass.Tiny, 0.2f),
            new("Functional/Generic/Poster", SizeClass.Small, 0.1f),
            new("Clothing/Sunglasses", SizeClass.Small, 0.1f),
            new("Cosmetic/BikeHorn/BikeHorn", SizeClass.Small, 0.2f),
            new("Clothing/Headset", SizeClass.Small, 0.2f),
            new("Clothing/TruckerCap", SizeClass.Small, 0.2f),
            new("Clothing/GloveInsulated", SizeClass.Small, 0.3f),
            new("Functional/Storage/Substances/Mug", SizeClass.Small, 0.3f),
            new("Consumable/Food/Drinks/Soda/SodaCanGeneric", SizeClass.Small, 0.4f),
            new("Functional/Generic/Identification/PDA", SizeClass.Small, 0.4f),
            new("Functional/Generic/Identification/SecurityPDA", SizeClass.Small, 0.4f),
            new("Clothing/FacewearGasMask", SizeClass.Small, 0.5f),
            new("Functional/Generic/Tanks/EmergencyOxygenTank", SizeClass.Small, 0.8f),
            new("Functional/Tools/Engineering/ScrewdriverBlue", SizeClass.Small, 0.2f),
            new("Functional/Tools/Engineering/Wirecutters", SizeClass.Small, 0.3f),
            new("Functional/Tools/Engineering/Multitool", SizeClass.Small, 0.3f),
            new("Functional/Tools/Generic/FlashlightBlue", SizeClass.Small, 0.4f),
            new("Functional/Tools/Medical/HealthScanner", SizeClass.Small, 0.4f),
            new("Functional/Tools/Engineering/Wrench", SizeClass.Small, 0.5f),
            new("Functional/Tools/Engineering/Welder", SizeClass.Small, 0.5f),
            new("Functional/Tools/Kitchen/KitchenKnife", SizeClass.Small, 0.5f),
            new("Functional/Materials/ElectricalCable", SizeClass.Small, 0.3f),
            new("Functional/Materials/ElectricalWire", SizeClass.Small, 0.3f),
            new("Functional/Materials/SteelRod", SizeClass.Small, 0.5f),
            new("Functional/Parts/MachineParts/MicroLaser", SizeClass.Small, 0.2f),
            new("Functional/Parts/MachineParts/MatterBin", SizeClass.Small, 0.4f),
            new("Functional/Parts/PowerCells/PowerCell", SizeClass.Small, 0.7f),
            new("Functional/Parts/Lights/LightBulb", SizeClass.Tiny, 0.1f),
            new("Functional/Parts/Lights/LightBulbBroken", SizeClass.Tiny, 0.1f),
            new("Functional/Parts/Lights/LightTube", SizeClass.Small, 0.2f),
            new("Functional/Parts/Lights/LightTubeBroken", SizeClass.Small, 0.1f),
            new("Functional/Tools/Engineering/Crowbar", SizeClass.Normal, 1.0f),
            new("Functional/Tools/Botany/Hatchet", SizeClass.Normal, 1.2f),
            new("Functional/Storage/Objects/Medkits/Medkit", SizeClass.Normal, 0.6f),
            new("Clothing/Backpack", SizeClass.Bulky, 0.6f),
            new("Clothing/Toolbelt", SizeClass.Bulky, 0.5f),
            new("Clothing/JumpsuitGrey", SizeClass.Normal, 1.0f),
            new("Clothing/ShoeHiTops", SizeClass.Normal, 0.8f),
            new("Clothing/ShoeJackboots", SizeClass.Normal, 1.2f),
            new("Clothing/SyndicateMagboots", SizeClass.Normal, 2.0f),
            new("Clothing/SyndicateHardsuitHelmet", SizeClass.Normal, 1.5f),
            new("Functional/Generic/FloorTile", SizeClass.Normal, 0.6f),
            new("Functional/Materials/GlassSheet", SizeClass.Normal, 1.0f),
            new("Functional/Materials/GlassReinforcedSheet", SizeClass.Normal, 1.5f),
            new("Functional/Materials/SteelSheet", SizeClass.Normal, 1.5f),
            new("Functional/Materials/SteelReinforcedSheet", SizeClass.Normal, 2.0f),
            new("Functional/Materials/WoodPlank", SizeClass.Normal, 1.0f),
            // Every weapon is at most Normal so it fits a backpack (TileMapTests.EveryWeaponFitsTheBackpack, #174);
            // rifles are the one Normal exception to the 2 kg band.
            new("Weapons/SyndicateMiniBomb", SizeClass.Normal, 0.5f),
            new("Weapons/Taser", SizeClass.Normal, 0.6f),
            new("Weapons/C4", SizeClass.Normal, 0.8f),
            new("Weapons/X4", SizeClass.Normal, 1.2f),
            new("Weapons/IonRifle", SizeClass.Normal, 2.5f),
            new("Weapons/LaserRifle", SizeClass.Normal, 2.5f),
            new("Weapons/M4", SizeClass.Normal, 3.5f),
            new("Functional/Storage/Objects/ToolBoxes/ToolboxBlue", SizeClass.Bulky, 2.0f),
            new("Functional/Generic/Boombox", SizeClass.Bulky, 3.0f),
            new("Functional/Materials/WoodLog", SizeClass.Bulky, 4.0f),
            new("Clothing/SyndicateHardsuit", SizeClass.Bulky, 6.0f),
            new("Functional/Generic/Tanks/OxygenTank", SizeClass.Huge, 5.0f),
            new("Functional/Generic/Tanks/PlasmaTank", SizeClass.Huge, 5.0f),
        };

        public static readonly IReadOnlyList<ContainerEntry> Containers = new ContainerEntry[]
        {
            new("Functional/Storage/Objects/Medkits/Medkit", 3, 2, SizeClass.Normal, 6f),
            new("Functional/Storage/Objects/ToolBoxes/ToolboxBlue", 4, 2, SizeClass.Bulky, 12f),
        };

        public static Entry Get(string path)
        {
            foreach (Entry entry in Entries)
            {
                if (entry.Path == path)
                {
                    return entry;
                }
            }

            throw new ArgumentException($"No size/weight entry for '{path}'. Add it to ItemSizeWeightTable.", nameof(path));
        }

        public static float WeightOf(string path) => Get(path).Weight;

        public static SizeClass SizeOf(string path) => Get(path).Size;
    }
}
#endif
