namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Interface for a class that can hold an item
    /// </summary>
    public interface IToolHolder
    {
        /// <summary>
        /// Gets the InteractionSource of the item currently in the active hand
        /// </summary>
        IInteractionSource GetActiveTool();
    }
}