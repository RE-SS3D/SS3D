using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

public interface ICraftable : INetworkObjectProvider, IGameObjectProvider
{
    public int StepAmount { get; }

    /// <summary>
    /// Stuff happening at the end of the crafting process, typically spawning a game object at the point of interaction.
    /// </summary>
    /// <param name="interaction"> The specific crafting interaction used. </param>
    /// <param name="interactionEvent"> the event linked to the crafting interaction</param>
    public void Craft(IInteraction interaction, InteractionEvent interactionEvent);

    public void Modify(IInteraction interaction, InteractionEvent interactionEvent);

    /// <summary>
    /// for multi step crafting, at which step of the crafting process we are.
    /// </summary>
    public int CurrentStepNumber { get; }

    /// <summary>
    /// If we are at the last step.
    /// </summary>
    public bool IsLastStep { get; }
}
