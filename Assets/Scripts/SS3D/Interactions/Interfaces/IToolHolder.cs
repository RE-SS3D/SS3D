﻿namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Represents a class that can hold items
    /// </summary>
    public interface IToolHolder
    {
        /// <summary>
        /// Gets the InteractionSource of the item currently in the active hand
        /// </summary>
        IInteractionSource GetActiveTool();
    }
}