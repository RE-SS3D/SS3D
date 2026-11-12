namespace SS3D.Systems.Examine
{
    /// <summary>
    /// String-table keys for substance container lines on examine (chemistry: free diegetic
    /// properties only — rough volume, temperature by touch, visible colour). Every line is fine print.
    /// </summary>
    public static class ExamineSubstanceKeys
    {
        public const string Empty = "examine.substance.empty";
        public const string EmptyFallback = "Empty.";

        public const string Contains = "examine.substance.contains";
        public const string ContainsFallback = "Contains about {0} of liquid.";

        public const string VolumeFew = "examine.substance.volume.few";
        public const string VolumeFewFallback = "a few milliliters";
        public const string VolumeSmall = "examine.substance.volume.small";
        public const string VolumeSmallFallback = "a small amount";
        public const string VolumeModerate = "examine.substance.volume.moderate";
        public const string VolumeModerateFallback = "a moderate amount";
        public const string VolumeLarge = "examine.substance.volume.large";
        public const string VolumeLargeFallback = "a large amount";

        public const string Feels = "examine.substance.feels";
        public const string FeelsFallback = "Feels {0}.";

        public const string TempFrozen = "examine.substance.temp.frozen";
        public const string TempFrozenFallback = "frosted / bitterly cold";
        public const string TempCold = "examine.substance.temp.cold";
        public const string TempColdFallback = "cold";
        public const string TempCool = "examine.substance.temp.cool";
        public const string TempCoolFallback = "cool";
        public const string TempWarm = "examine.substance.temp.warm";
        public const string TempWarmFallback = "warm";
        public const string TempHot = "examine.substance.temp.hot";
        public const string TempHotFallback = "hot";
        public const string TempScalding = "examine.substance.temp.scalding";
        public const string TempScaldingFallback = "<k=danger>scalding</k>";

        public const string Looks = "examine.substance.looks";
        public const string LooksFallback = "The liquid looks {0}.";

        public const string ColorClear = "examine.substance.color.clear";
        public const string ColorClearFallback = "clear";
        public const string ColorMurky = "examine.substance.color.murky";
        public const string ColorMurkyFallback = "murky";
        public const string ColorRed = "examine.substance.color.red";
        public const string ColorRedFallback = "reddish";
        public const string ColorOrange = "examine.substance.color.orange";
        public const string ColorOrangeFallback = "orange";
        public const string ColorYellow = "examine.substance.color.yellow";
        public const string ColorYellowFallback = "yellow";
        public const string ColorGreen = "examine.substance.color.green";
        public const string ColorGreenFallback = "green";
        public const string ColorBlue = "examine.substance.color.blue";
        public const string ColorBlueFallback = "blue";
        public const string ColorPurple = "examine.substance.color.purple";
        public const string ColorPurpleFallback = "purple";

        /// <summary>Every key with its English text, for the table seed.</summary>
        public static readonly (string Key, string English)[] All =
        {
            (Empty, EmptyFallback), (Contains, ContainsFallback),
            (VolumeFew, VolumeFewFallback), (VolumeSmall, VolumeSmallFallback),
            (VolumeModerate, VolumeModerateFallback), (VolumeLarge, VolumeLargeFallback),
            (Feels, FeelsFallback),
            (TempFrozen, TempFrozenFallback), (TempCold, TempColdFallback), (TempCool, TempCoolFallback),
            (TempWarm, TempWarmFallback), (TempHot, TempHotFallback), (TempScalding, TempScaldingFallback),
            (Looks, LooksFallback),
            (ColorClear, ColorClearFallback), (ColorMurky, ColorMurkyFallback), (ColorRed, ColorRedFallback),
            (ColorOrange, ColorOrangeFallback), (ColorYellow, ColorYellowFallback), (ColorGreen, ColorGreenFallback),
            (ColorBlue, ColorBlueFallback), (ColorPurple, ColorPurpleFallback),
        };
    }
}
