namespace SS3D.Systems.Examine
{
    /// <summary>
    /// String-table keys for gas tank lines on examine (armor.md §6).
    /// </summary>
    public static class ExamineTankKeys
    {
        public const string Fill = "examine.tank.fill";

        public const string FillFallback = "Tank: {0}% full.";

        public const string FillLow = "examine.tank.fill_low";

        public const string FillLowFallback = "Tank: {0}% full, <k=warn>low</k>.";

        /// <summary>Fill fraction below which the tank line reads as low.</summary>
        public const float LowFraction = 0.25f;
    }
}
