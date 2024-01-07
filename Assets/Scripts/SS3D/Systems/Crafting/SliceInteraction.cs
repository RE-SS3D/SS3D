using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public class SliceInteraction : CraftingInteraction
    {
        
        public SliceInteraction(float delay, Transform characterTransform) : base(delay, characterTransform, CraftingInteractionType.Slice) { }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Take;
        }

        public override string GetGenericName()
        {
            return "Slice";
        }
    }
}

