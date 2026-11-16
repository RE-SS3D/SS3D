namespace SS3D.Systems.Substances
{
    public static class SubstanceConstants
    {
        /// <summary>Default pour / transfer chunk in milliliters.</summary>
        public const float DefaultTransferVolumeMl = 25f;

        /// <summary>~20 °C room temperature when atmos is unavailable.</summary>
        public const float DefaultAmbientKelvin = 293.15f;

        /// <summary>How fast container temperature returns toward ambient (fraction per second).</summary>
        public const float TemperatureDecayPerSecond = 0.05f;

        /// <summary>Volumes below this are treated as empty for a reagent entry.</summary>
        public const float VolumeEpsilonMl = 0.001f;

        /// <summary>Relative ratio tolerance for exact recipe match.</summary>
        public const float RecipeRatioEpsilon = 0.05f;

        /// <summary>Near-miss: same reagents present, ratio within this relative error of recipe.</summary>
        public const float NearMissRatioEpsilon = 0.35f;
    }
}
