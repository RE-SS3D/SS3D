namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Localization keys for construction stage and next-step examine lines (construction.md §11,
    /// examine.md §5, §7). Seeded into the Examine table by the structural-damage content recipe.
    /// </summary>
    public static class ExamineConstructionKeys
    {
        public const string FloorPry = "examine.construction.floor_pry";
        public const string FloorPryFallback = "A crowbar would pry the tile up.";

        public const string BarePlating = "examine.construction.bare_plating";
        public const string BarePlatingFallback = "Bare hull plating. Floor tiles would cover it.";

        public const string RepairWelder = "examine.construction.repair_welder";
        public const string RepairWelderFallback = "A welder would repair it.";

        /// <summary>Every key with its English fallback, for seeding.</summary>
        public static readonly (string Key, string English)[] All =
        {
            (FloorPry, FloorPryFallback),
            (BarePlating, BarePlatingFallback),
            (RepairWelder, RepairWelderFallback),
        };
    }
}
