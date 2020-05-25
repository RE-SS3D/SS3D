using SS3D.Engine.Interactions;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class SimpleCraftingTarget : InteractionTargetBehaviour
    {
        /// <summary>
        /// The name shown in the context menu
        /// </summary>
        public string Name;
        /// <summary>
        /// The id of the necessary item
        /// </summary>
        public string ItemId;
        /// <summary>
        /// Should the source be consumed
        /// </summary>
        public bool Consume;
        /// <summary>
        /// The objects which are created
        /// </summary>
        public SimpleCraftingInteraction.CraftingResult[] ResultingObjects;

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new SimpleCraftingInteraction
            {
                Name = Name, ItemId = ItemId, Consume = Consume, ResultingObjects = ResultingObjects
            } };
        }
    }
}