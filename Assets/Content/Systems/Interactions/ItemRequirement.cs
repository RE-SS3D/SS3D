using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Systems.Interactions
{
    public class ItemRequirement : Requirement
    {
        /// <summary>
        /// The id of the required item
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The required amount of item
        /// </summary>
        public int Amount { get; set; } = 1;

        public ItemRequirement(IInteraction interaction, string id) : base(interaction)
        {
            Id = id;
        }
        
        public ItemRequirement(IInteraction interaction, string id, int amount) : this(interaction, id)
        {
            Amount = amount;
        }

        public override bool SatisfiesRequirement(InteractionEvent interactionEvent)
        {
            var item = interactionEvent.Source.GetComponent<Item>();
            if (item == null)
            {
                return false;
            }

            return item.ItemId == Id && item.HasQuantity(Amount);
        }

        protected override void ApplyRequirement(InteractionEvent interactionEvent)
        {
            interactionEvent.Source.GetComponent<Item>().ConsumeQuantity(Amount);
        }
    }
}