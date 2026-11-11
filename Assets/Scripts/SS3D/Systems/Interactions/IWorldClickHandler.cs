namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// A client-side handler that gets the primary click and the Cancel key before target interactions, such
    /// as the construction hull-hole ghost. Registered on <see cref="InteractionController.WorldClickHandler"/>
    /// so the interactions folder does not depend on the systems that use it.
    /// </summary>
    public interface IWorldClickHandler
    {
        /// <summary>True when the click was used (a ghost is up).</summary>
        bool TryHandlePrimaryClick();

        /// <summary>True when the Cancel key stopped something the handler was running.</summary>
        bool TryCancelLocal();
    }
}
