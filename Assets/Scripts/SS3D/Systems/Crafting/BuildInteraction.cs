using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Interaction to turn something into a placed tile object. 
    /// </summary>
    public class BuildInteraction : CraftingInteraction
    {

        public BuildInteraction(float delay, Transform characterTransform) : base(delay, characterTransform) { }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Take;
        }

        public override string GetGenericName()
        {
            return "Build";
        }
    }
}

