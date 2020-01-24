namespace Interaction.Core
{
    public interface ISingularInteraction : IBaseInteraction
    {
        /// <summary>
        /// This method is called by `InteractionReceiver` when it an event of the subscribed kind is received
        /// </summary>
        /// <param name="e">The event that was received</param>
        /// <returns>Whether or not the interaction succeeded. A success will block event kinds specified in `Setup`</returns>
        bool Handle(InteractionEvent e);
    }
}