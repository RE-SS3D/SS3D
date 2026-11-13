using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Maps health vitals to body presentation intent. Health emits this; <see cref="Ragdoll"/> applies it.
    /// Critical and cardiac arrest collapse even while <see cref="HealthSnapshot.IsConscious"/> remains true.
    /// </summary>
    public static class BodyPresentationIntent
    {
        public static BodyPresentationState FromSnapshot(HealthSnapshot snapshot)
        {
            if (snapshot.State == HealthState.Dead)
            {
                return BodyPresentationState.Dead;
            }

            if (!snapshot.IsConscious
                || snapshot.IsCardiacArrest
                || snapshot.State == HealthState.Critical)
            {
                return BodyPresentationState.Collapsed;
            }

            if (snapshot.State == HealthState.Downed)
            {
                return BodyPresentationState.Downed;
            }

            return BodyPresentationState.Locomotion;
        }
    }
}
