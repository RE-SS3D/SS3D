namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Localization keys for dynamic structural integrity examine lines.
    /// </summary>
    public static class ExamineStructuralIntegrityKeys
    {
        public const string Damaged = "examine.structural.damaged";
        public const string DentedDevice = "examine.device.dented";
        public const string DentedDeviceFallback = "Its casing is <k=warn>dented</k> and scorched.";
        public const string BrokenDevice = "examine.device.broken";
        public const string BrokenDeviceFallback = "It's <k=danger>smashed</k> and dead. Sparks spit from the broken casing.";
        public const string ChippedGlass = "examine.structural.chipped_glass";
        public const string ChippedGlassFallback = "The glass is <k=warn>chipped</k>, with a small star of cracks.";
        public const string FracturedGlass = "examine.structural.fractured_glass";
        public const string FracturedGlassFallback = "Branching <k=danger>fractures</k> spread across the glass.";

        public const string GougedWood = "examine.structural.gouged_wood";
        public const string GougedWoodFallback = "The wood is <k=warn>gouged</k> and chipped.";
        public const string SplitWood = "examine.structural.split_wood";
        public const string SplitWoodFallback = "Deep <k=danger>splits</k> run along the wood grain, exposing splintered fibres.";

        public const string TornMetal = "examine.structural.torn_metal";
        public const string TornMetalFallback = "The metal is deeply dented and <k=danger>torn</k>.";

        public const string FracturedFloor = "examine.structural.fractured_floor";
        public const string FracturedFloorFallback = "The flooring is badly <k=danger>cracked</k> and scorched.";
        public const string DamagedPlating = "examine.structural.damaged_plating";
        public const string DamagedPlatingFallback = "The hull plating is badly <k=warn>damaged</k>, but remains <k=good>sealed</k>.";

        public const string Cracked = "examine.structural.cracked";

        public const string DamagedFallback = "It looks <k=warn>dented</k> and scorched.";
        public const string CrackedFallback = "It is <k=danger>cracked</k> and hissing — atmosphere is leaking through.";
    }
}
