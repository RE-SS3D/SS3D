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

        public const string Girder = "examine.construction.girder";
        public const string GirderFallback = "A bare steel girder. Steel sheets would plate it, glass sheets would glaze it; a welder would cut it apart.";

        public const string GirderPlated = "examine.construction.girder_plated";
        public const string GirderPlatedFallback = "Plated girder. Seams unwelded: a welder would seal it, a crowbar would pull the plate off.";

        public const string WallCutSeams = "examine.construction.wall_cut_seams";
        public const string WallCutSeamsFallback = "A welder could cut its seams.";

        public const string WindowLiftPane = "examine.construction.window_lift_pane";
        public const string WindowLiftPaneFallback = "A crowbar could lift the pane out.";

        public const string Reinforced = "examine.construction.reinforced";
        public const string ReinforcedFallback = "Reinforced. A welder can patch it, but only explosives will take it apart.";

        /// <summary>Every key with its English fallback, for seeding.</summary>
        public static readonly (string Key, string English)[] All =
        {
            (FloorPry, FloorPryFallback),
            (BarePlating, BarePlatingFallback),
            (RepairWelder, RepairWelderFallback),
            (Girder, GirderFallback),
            (GirderPlated, GirderPlatedFallback),
            (WallCutSeams, WallCutSeamsFallback),
            (WindowLiftPane, WindowLiftPaneFallback),
            (Reinforced, ReinforcedFallback),
        };
    }
}
