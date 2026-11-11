namespace SS3D.Systems.Combat.Interactions
{
    /// <summary>
    /// What the HUD may read from a held weapon (tech-debt P2.1). Every member is a pure function of state and time:
    /// reading changes nothing, so how often the HUD polls can't change gameplay. Reload completion and recoil decay are
    /// the weapon's own business.
    /// </summary>
    public interface ICombatReadout
    {
        /// <summary>0 just after a shot or reload started, 1 when ready.</summary>
        float ReadyProgress01 { get; }

        /// <summary>Reloading or on the fire cooldown.</summary>
        bool IsBusy { get; }

        /// <summary>The accuracy cone a shot fired now would use.</summary>
        float CurrentSpreadDegrees(float horizontalSpeed, float aimDistanceMeters, float exertionPenalty = 0f);
    }
}
