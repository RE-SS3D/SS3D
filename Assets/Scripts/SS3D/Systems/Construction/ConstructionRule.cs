using SS3D.Systems.Tile;

namespace SS3D.Systems.Construction
{
    /// <summary>One construction step. Cell actions run through <c>ConstructionPlacementSubSystem</c>; the rest are interactions.</summary>
    public enum ConstructionStep : byte
    {
        PlacePlating = 0,
        PlaceFloorTile = 1,
        PryFloorTile = 2,
        RepairPlating = 3,
        RepairStructure = 4,
        PlaceGirder = 5,
        PlateGirder = 6,
        WeldPlated = 7,
        GlazeGirder = 8,
        ClearWreckage = 9,
    }

    /// <summary>What a rule acts on. Name-matched kinds also read <see cref="ConstructionRule.TargetName"/>.</summary>
    public enum ConstructionTargetKind : byte
    {
        /// <summary>A cell inside the station with no plating (a hull hole).</summary>
        HullHole = 0,
        /// <summary>A cell with plating and nothing on the Turf layer.</summary>
        BarePlating = 1,
        /// <summary>Any walkable floor turf.</summary>
        FloorTurf = 2,
        /// <summary>Damaged or Cracked plating.</summary>
        DamagedPlenum = 3,
        /// <summary>A Damaged or Cracked wall, window or reinforced wall.</summary>
        DamagedStructure = 4,
        /// <summary>Plating with nothing on Turf or a walkable floor, and no furniture (construction.md §6).</summary>
        ClearFloor = 5,
        /// <summary>A cell with steel wreckage on Turf.</summary>
        Wreckage = 6,
        /// <summary>The tile named <see cref="ConstructionRule.TargetName"/>.</summary>
        NamedTile = 7,
    }

    public enum ConstructionOutcome : byte
    {
        /// <summary>Place <see cref="ConstructionRule.ResultName"/> on <see cref="ConstructionRule.Layer"/>.</summary>
        Place = 0,
        /// <summary>Clear the target from <see cref="ConstructionRule.Layer"/>.</summary>
        Remove = 1,
        /// <summary>Set the target's integrity back to Intact.</summary>
        Repair = 2,
        /// <summary>Replace the target with <see cref="ConstructionRule.ResultName"/> (a stage step).</summary>
        Replace = 3,
        /// <summary>Remove the cell's wreckage.</summary>
        ClearWreckage = 4,
    }

    /// <summary>
    /// A row of <see cref="ConstructionRecipes"/>: input, cost, time, result and returns
    /// (D4 of 2026-11_construction-mvp1). Names are catalog names (<see cref="ConstructionNames"/>).
    /// </summary>
    public sealed class ConstructionRule
    {
        public ConstructionStep Step { get; init; }

        /// <summary>Stable interaction / RPC id. Never a display string.</summary>
        public string GenericName { get; init; }

        /// <summary>Radial verb, e.g. "Repair".</summary>
        public string Verb { get; init; }

        public ConstructionInput Input { get; init; }

        public ConstructionTargetKind Target { get; init; }

        /// <summary>Tile name the target must be, for name-matched targets; null otherwise.</summary>
        public string TargetName { get; init; }

        /// <summary>The layer the target sits on and the result goes to.</summary>
        public TileLayer Layer { get; init; }

        /// <summary>Units of a material spent at completion. Tools cost 0.</summary>
        public int Cost { get; init; }

        public float Seconds { get; init; }

        public ConstructionOutcome Outcome { get; init; }

        /// <summary>Tile placed by a <see cref="ConstructionOutcome.Place"/> rule.</summary>
        public string ResultName { get; init; }

        /// <summary>Item handed back on completion (sheets from a pulled plate), or null.</summary>
        public string ReturnItemName { get; init; }

        public int ReturnCount { get; init; }

        /// <summary>True for steps chosen by clicking a cell under a placement ghost.</summary>
        public bool IsCellAction { get; init; }

        /// <summary>Deconstruction runs from Intact only (construction.md §9).</summary>
        public bool RequiresIntact { get; init; }

        /// <summary>Reinforced walls and windows can be repaired but not taken apart (D10).</summary>
        public bool RefusesReinforced { get; init; }
    }
}
