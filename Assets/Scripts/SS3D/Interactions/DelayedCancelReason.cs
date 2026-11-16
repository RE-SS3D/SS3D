namespace SS3D.Interactions
{
    /// <summary>
    /// Why a timed step stopped before it finished. Sent to the owning client with the cancel and shown
    /// briefly on the progress ring (construction.md §11).
    /// </summary>
    public enum DelayedCancelReason : byte
    {
        /// <summary>Cancel key or a new interaction replaced this one.</summary>
        Manual = 0,
        Moved = 1,
        KnockedDown = 2,
        Hurt = 3,
        MissingMaterials = 4,
        /// <summary>The step stopped being possible for any other reason (target gone, out of reach).</summary>
        Interrupted = 5,
    }

    public static class DelayedCancelMessages
    {
        public static string Format(DelayedCancelReason reason) =>
            reason switch
            {
                DelayedCancelReason.Manual => "Cancelled",
                DelayedCancelReason.Moved => "Moved away",
                DelayedCancelReason.KnockedDown => "Knocked down",
                DelayedCancelReason.Hurt => "Hurt",
                DelayedCancelReason.MissingMaterials => "Not enough materials",
                _ => "Interrupted",
            };
    }
}
