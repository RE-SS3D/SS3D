using System;
using FishNet.Object;
using UnityEngine;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// An interaction whose result the animation alone doesn't explain (action-messages.md §2). After
    /// its server-side <see cref="IInteraction.Start"/>, the runtime asks it for a narration and
    /// raises <see cref="InteractionNarration.ServerNarrated"/>; the action message system sends it.
    /// Return false when nothing worth a line happened (refused, no effect).
    /// </summary>
    public interface INarratedInteraction
    {
        bool TryGetNarration(InteractionEvent interactionEvent, out InteractionNarration narration);
    }

    /// <summary>
    /// A line to narrate: a key in the Examine table's <c>action.</c> range, its visibility level
    /// (the values of <c>ActionVisibility</c>: 0 public, 1 close, 2 target-only) and who/what it is about.
    /// </summary>
    public readonly struct InteractionNarration
    {
        public InteractionNarration(string key, byte level, NetworkObject actor, NetworkObject target, NetworkObject item)
        {
            Key = key;
            Level = level;
            Actor = actor;
            Target = target;
            Item = item;
        }

        public string Key { get; }

        public byte Level { get; }

        public NetworkObject Actor { get; }

        public NetworkObject Target { get; }

        public NetworkObject Item { get; }

        /// <summary>Server: raised for every narration an interaction produced.</summary>
        public static event Action<InteractionNarration> ServerNarrated;

        /// <summary>Asks <paramref name="interaction"/> for a narration and raises it. Server only.</summary>
        public static bool TryRaise(IInteraction interaction, InteractionEvent interactionEvent)
        {
            if (interaction is not INarratedInteraction narrated
                || !narrated.TryGetNarration(interactionEvent, out InteractionNarration narration)
                || string.IsNullOrEmpty(narration.Key))
            {
                return false;
            }

            ServerNarrated?.Invoke(narration);
            return true;
        }

        /// <summary>The character doing it: the root NetworkObject above the interaction source (a hand, a held tool).</summary>
        public static NetworkObject ActorOf(InteractionEvent interactionEvent) =>
            interactionEvent?.Source is Component source && source != null
                ? source.transform.root.GetComponent<NetworkObject>()
                : null;

        /// <summary>The NetworkObject behind the interaction target, if any.</summary>
        public static NetworkObject TargetOf(InteractionEvent interactionEvent) =>
            interactionEvent?.Target is Component target && target != null
                ? target.GetComponentInParent<NetworkObject>()
                : null;
    }
}
