namespace SS3D.Systems.Examine
{
    /// <summary>String-table keys for a gun's loaded rounds or an energy weapon's charges (examine.md §7).</summary>
    public static class ExamineWeaponKeys
    {
        public const string Rounds = "examine.weapon.rounds";
        public const string RoundsFallback = "Rounds: {0}.";
        public const string RoundsEmpty = "examine.weapon.rounds_empty";
        public const string RoundsEmptyFallback = "Magazine <k=warn>empty</k>.";
        public const string Charge = "examine.weapon.charge";
        public const string ChargeFallback = "Charge: {0} shots.";
        public const string ChargeEmpty = "examine.weapon.charge_empty";
        public const string ChargeEmptyFallback = "Charge <k=warn>empty</k>.";

        public static readonly (string Key, string English)[] All =
        {
            (Rounds, RoundsFallback), (RoundsEmpty, RoundsEmptyFallback),
            (Charge, ChargeFallback), (ChargeEmpty, ChargeEmptyFallback),
        };
    }
}
