using SS3D.Systems.Inventory.Containers;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Pure armor absorption math — testable without Unity/FishNet. See Documents/design/armor.md §2.
    /// </summary>
    public static class ArmorSimulation
    {
        /// <summary>
        /// Resolves one hit against one armor piece: damage_applied = max(0, incoming - absorption),
        /// integrity depletes by the amount actually absorbed. Integrity is a single pool shared
        /// across brute/burn absorption on this piece — if a hit's combined absorption would exceed
        /// the piece's remaining integrity, absorption is scaled down proportionally so integrity
        /// never goes negative and the piece breaks cleanly on this hit rather than the next one.
        /// </summary>
        public static (float RemainingBrute, float RemainingBurn, float IntegrityLoss) ResolveAbsorption(
            ArmorProfile profile, float currentIntegrity, float incomingBrute, float incomingBurn)
        {
            if (currentIntegrity <= 0f)
            {
                return (incomingBrute, incomingBurn, 0f);
            }

            float bruteAbsorbed = Min(profile.BruteAbsorption, incomingBrute);
            float burnAbsorbed = Min(profile.BurnAbsorption, incomingBurn);
            float totalAbsorbed = bruteAbsorbed + burnAbsorbed;

            if (totalAbsorbed > currentIntegrity)
            {
                float scale = currentIntegrity / totalAbsorbed;
                bruteAbsorbed *= scale;
                burnAbsorbed *= scale;
                totalAbsorbed = currentIntegrity;
            }

            float remainingBrute = Max(0f, incomingBrute - bruteAbsorbed);
            float remainingBurn = Max(0f, incomingBurn - burnAbsorbed);

            return (remainingBrute, remainingBurn, totalAbsorbed);
        }

        /// <summary>
        /// Sort key for layering: a hit meets the outermost piece first (armor.md §2).
        /// </summary>
        public static int LayerRank(ContainerType slot) => slot switch
        {
            ContainerType.ExoSuit => 0,
            ContainerType.Jumpsuit => 2,
            _ => 1,
        };

        /// <summary>
        /// True when damage is left after every layer: the same event punctures a seal (armor.md §3).
        /// </summary>
        public static bool ReachesLimb(float remainingBrute, float remainingBurn) =>
            remainingBrute > 0f || remainingBurn > 0f;

        private static float Min(float a, float b) => a < b ? a : b;

        private static float Max(float a, float b) => a > b ? a : b;
    }
}
