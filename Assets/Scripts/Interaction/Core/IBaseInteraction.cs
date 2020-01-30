using System;

namespace Interaction.Core
{
    /// <summary>
    /// This interface defines everything needed to be able to receive interaction events from an `InteractionReceiver`.<br/>
    /// `MonoBehaviour`s that implement this interface will need a sibling `InteractionReceiver` to function as intended.<br/>
    /// You may however manually choose to not use a `MonoBehaviour` and implement this interface whereever you want can can call `Subscribe` on the `InteractionReceiver` yourself.<br/>
    /// <br/>
    /// For a lot of interactions, you may want to extend the `Interaction` Scriptable Object instead though.
    /// </summary>
    public interface IBaseInteraction
    {
        /// <summary>
        /// This method is called by `InteractionReceiver` when it initializes.<br/>
        /// You should use the callbacks to specify the requirements for this listener.
        /// </summary>
        /// <param name="listen">Call this action with the event kind you want to listen for</param>
        /// <param name="blocks">Call this action with any event types that will be blocked when this interaction succeeds. Can be called any number of times.</param>
        void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks);

        /// <summary>
        /// This method is called by `InteractionReceiver` after it is done handling events for this frame.<br/>
        /// You may use this to reset your interaction if it depends on several incoming events.
        /// </summary>
        void Reset();
    }
}