using SS3D.Systems.Tile;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// One construction step. Only patching a hull hole is a cell action (<c>ConstructionPlacementSubSystem</c>,
    /// a ghost on the empty cell); the rest are interactions on a placed object, including the cell steps
    /// that act on the floor or plating you hover ("Construct girder", "Lay floor tile", "Clear wreckage").
    /// </summary>
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
        CutSeams = 10,
        PullPlate = 11,
        LiftPane = 12,
        DismantleGirder = 13,
    }

    /// <summary>What a rule acts on. Name-matched kinds also read <see cref="ConstructionRule.TargetName"/>.</summary>
    public enum ConstructionTargetKind : byte
    {
        /// <summary>A cell inside the station with no plating (a hull hole).</summary>
        HullHole = 0,
        /// <summary>Plating with nothing on the Turf layer. Targeted through the plating.</summary>
        BarePlating = 1,
        /// <summary>Any walkable floor turf.</summary>
        FloorTurf = 2,
        /// <summary>Damaged or Cracked plating.</summary>
        DamagedPlenum = 3,
        /// <summary>A Damaged or Cracked wall, window or reinforced wall.</summary>
        DamagedStructure = 4,
        /// <summary>
        /// Plating with nothing on Turf or a walkable floor, and no furniture (construction.md §6). Targeted
        /// through the floor tile or the bare plating.
        /// </summary>
        ClearFloor = 5,
        /// <summary>A cell with steel wreckage on Turf. Targeted through the plating or floor under it.</summary>
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

        /// <summary>True for steps chosen by clicking an empty cell under a placement ghost (a hull hole).</summary>
        public bool IsCellAction { get; init; }

        /// <summary>
        /// An object step whose target is the cell under the hovered floor or plating rather than the object
        /// itself (<see cref="ConstructionTargetKind.BarePlating"/>, <see cref="ConstructionTargetKind.ClearFloor"/>,
        /// <see cref="ConstructionTargetKind.Wreckage"/>). Checked with <c>ConstructionCellRules</c>.
        /// </summary>
        public bool ActsOnCell => !IsCellAction && Target is ConstructionTargetKind.BarePlating
            or ConstructionTargetKind.ClearFloor or ConstructionTargetKind.Wreckage;

        /// <summary>A Cracked target is spent by the step: nothing is handed back (a broken floor tile).</summary>
        public bool NoReturnWhenCracked { get; init; }

        /// <summary>Deconstruction runs from Intact only (construction.md §9).</summary>
        public bool RequiresIntact { get; init; }

        /// <summary>Reinforced walls and windows can be repaired but not taken apart (D10).</summary>
        public bool RefusesReinforced { get; init; }
    }
}
