using FishNet.Object;
using JetBrains.Annotations;
using QuikGraph;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Animations;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Crafting interactions are at the core of the crafting system. They should be mostly created by the crafting menu interaction,
    /// and they are the bridge between player inputs and the crafting system.
    /// </summary>
    public class CraftingInteraction : DelayedInteraction
    {
        /// <summary>
        /// The recipe link associated to this interaction. Crafting interactions are always associated to a recipe link.
        /// </summary>
        private readonly TaggedEdge<RecipeStep, RecipeStepLink> _chosenLink;

        /// <summary>
        /// The transform of the game object executing the crafting interaction, useful to check if the source moved
        /// during the interaction, for example.
        /// </summary>
        private readonly Transform _characterTransform;

        /// <summary>
        /// Type of this interaction, defines which recipe will be available.
        /// </summary>
        private readonly InteractionType _type;

        /// <summary>
        /// The start position of the source of the interaction, when the interaction begins.
        /// </summary>
        private Vector3 _startPosition;

        public CraftingInteraction(float delay, Transform characterTransform, InteractionType type, TaggedEdge<RecipeStep, RecipeStepLink> link)
        {
            _characterTransform = characterTransform;
            _startPosition = characterTransform.position;
            Delay = delay;
            _type = type;
            _chosenLink = link;
        }

        /// <summary>
        /// The start position of the source of the interaction, when the interaction begins.
        /// </summary>
        public Vector3 StartPosition => _startPosition;

        /// <summary>
        /// Type of this interaction, defines which recipe will be available.
        /// </summary>
        public override InteractionType InteractionType => _type;

        /// <summary>
        /// The transform of the game object executing the crafting interaction, useful to check if the source moved
        /// during the interaction, for example.
        /// </summary>
        public Transform CharacterTransform => _characterTransform;

        /// <summary>
        /// The recipe link associated to this interaction. Crafting interactions are always associated to a recipe link.
        /// </summary>
        public TaggedEdge<RecipeStep, RecipeStepLink> ChosenLink => _chosenLink;

        public override string GetGenericName() => "Craft";

        public override Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Take;

        /// <summary>
        /// Check if the crafting can occur.
        /// TODO : Add more conditions, as they are more things, such as obstacles, that can prevent a crafting interaction to occur.
        /// </summary>
        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // Check for movement once the interaction started.
            if (HasStarted && !InteractionExtensions.CharacterMoveCheck(_startPosition, _characterTransform.position))
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        [NotNull]
        public override string GetName(InteractionEvent interactionEvent)
        {
            return GetGenericName() + " " + interactionEvent.Target.GetGameObject().name.Split("(")[0];
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Subsystems.TryGet(out CraftingSubSystem craftingSystem);
            craftingSystem.CancelMoveAllObjectsToCraftPoint(reference);

            if (interactionEvent.Source.GetRootSource() is IItemHolder itemHolder
                && interactionEvent.Source.GetRootSource() is IInteractionSourceAnimate animatedSource
                && itemHolder.ItemHeld.TryGetComponent(out IInteractiveTool tool))
            {
                animatedSource.CancelSourceAnimation(InteractionType, tool.NetworkObject, Delay);
            }
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (Subsystems.TryGet(out CraftingSubSystem craftingSystem))
            {
                craftingSystem.CancelMoveAllObjectsToCraftPoint(reference);
                craftingSystem.Craft(this, interactionEvent);
            }
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            _startPosition = _characterTransform.position;
            Subsystems.TryGet(out CraftingSubSystem craftingSystem);
            craftingSystem.MoveAllObjectsToCraftPoint(this, interactionEvent, reference);
            ViewLocator.Get<CraftingMenu>()[0].HideMenu();

            Vector3 point = interactionEvent.Point;
            if (interactionEvent.Target.TryGetInteractionPoint(interactionEvent.Source, out Vector3 customPoint))
            {
                point = customPoint;
            }

            if (interactionEvent.Source.GetRootSource() is IItemHolder itemHolder
                && interactionEvent.Source.GetRootSource() is IInteractionSourceAnimate animatedSource
                && itemHolder.ItemHeld.TryGetComponent(out IInteractiveTool tool))
            {
                animatedSource.PlaySourceAnimation(InteractionType, tool.NetworkObject, point, Delay);
            }

            return true;
        }
    }
}
