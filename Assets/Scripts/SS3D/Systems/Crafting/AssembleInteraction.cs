using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Use this interaction to assemble items into other things. 
    /// </summary>
    public class AssembleInteraction : CraftingInteraction
    {

        public AssembleInteraction(float delay, Transform characterTransform) : base(delay, characterTransform) { }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Take;
        }

        public override string GetGenericName()
        {
            return "Assemble";
        }
    }
}
