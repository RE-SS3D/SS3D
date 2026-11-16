namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Server: confirms that a combat one-shot a client asked to play matches an action the server accepted (a swing it
    /// scheduled, a round it fired, a reload or throw it started). Implemented by the combat network component, so the
    /// body state machine doesn't depend on Combat.
    /// </summary>
    public interface IClientTriggerGate
    {
        bool ServerAcceptsClientTrigger(AnimationTriggerId trigger);
    }

    /// <summary>
    /// Which one-shots a client may start on its own body (tech-debt P1.6, networking §2). Emotes are free; combat
    /// one-shots need the server to have accepted the matching action; flinches only ever come from the server. The
    /// full move to server-fired one-shots is P4.2.
    /// </summary>
    public static class ClientTriggerRules
    {
        public static bool MayClientStart(AnimationTriggerId trigger, IClientTriggerGate combatGate) => trigger switch
        {
            AnimationTriggerId.Emote => true,
            AnimationTriggerId.AttackSwing
                or AnimationTriggerId.AttackStab
                or AnimationTriggerId.Throw
                or AnimationTriggerId.FireRifle
                or AnimationTriggerId.Reload => combatGate != null && combatGate.ServerAcceptsClientTrigger(trigger),
            _ => false,
        };
    }
}
