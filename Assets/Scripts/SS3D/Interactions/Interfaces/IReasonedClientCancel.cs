namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// A client interaction that wants to know why the server cancelled it. Called instead of
    /// <see cref="IClientInteraction.ClientCancel"/>.
    /// </summary>
    public interface IReasonedClientCancel
    {
        void ClientCancel(InteractionEvent interactionEvent, DelayedCancelReason reason);
    }
}
