using System;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Raised on the server when the body that owns an interaction source takes damage. A
    /// <see cref="DelayedInteraction"/> with <c>CancelOnDamage</c> listens through the root source's
    /// parents, so health stays out of the interactions assembly.
    /// </summary>
    public interface IDamageNotifier
    {
        event Action ServerDamaged;
    }
}
