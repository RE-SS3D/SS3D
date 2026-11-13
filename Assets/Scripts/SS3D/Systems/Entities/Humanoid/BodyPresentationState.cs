namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Replicated body pose authority for humanoids. Health and combat write intent;
    /// <see cref="Ragdoll"/> is the sole applier (physics, animator suppress, movement gate).
    /// Values go on the wire: only ever append.
    /// </summary>
    public enum BodyPresentationState : byte
    {
        Locomotion = 0,
        Collapsed = 1,
        Dead = 2,
        /// <summary>On the floor but animated: crawls, keeps its hands (health.md §4 Downed). Not a ragdoll.</summary>
        Downed = 3,
    }

    public static class BodyPresentationStateExtensions
    {
        /// <summary>Physics ragdoll with the animator and movement off: Collapsed or Dead. Downed is animated.</summary>
        public static bool IsRagdolled(this BodyPresentationState state) =>
            state is BodyPresentationState.Collapsed or BodyPresentationState.Dead;
    }
}
