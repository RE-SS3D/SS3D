using System;

namespace SS3D.Interactions
{
    /// <summary>
    /// Server: an optional wrapper around each step of a running interaction (its <c>Start</c>, then each <c>Update</c> of
    /// a delayed one), which is when the interaction actually changes the world, a frame or more after the RPC that queued
    /// it. The map editor uses it to turn an editor user's in-world setup changes into undo steps
    /// (Documents/architecture/2026-11_object-setup-persistence.md Q14). Null in normal play.
    /// </summary>
    public static class ServerInteractionSteps
    {
        /// <summary>Runs the step it is given and returns its result: true while the interaction keeps running.</summary>
        public static Func<InteractionInstance, Func<bool>, bool> Wrap { get; set; }

        internal static bool Run(InteractionInstance instance, Func<bool> step) => Wrap == null ? step() : Wrap(instance, step);
    }
}
