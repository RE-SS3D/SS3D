using FishNet.Broadcast;

namespace SS3D.Systems.Rounds.Messages
{
    /// <summary>
    /// Client → server, before a round: the sender will watch the next round as a ghost instead of
    /// taking a body (<c>observer.md</c> §2). Mid-round, observing goes through
    /// <see cref="SS3D.Systems.Entities.EntitySubSystem.CmdObserve"/> instead.
    /// </summary>
    public struct ChangePlayerObservingMessage : IBroadcast
    {
        public readonly bool Observing;

        public ChangePlayerObservingMessage(bool observing)
        {
            Observing = observing;
        }
    }
}
