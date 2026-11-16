using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    /// <summary>
    /// Opens another character's examine paperdoll (Search) — Tier Instant, Help-default.
    /// UI opens client-side via <see cref="SearchClientInteraction"/>; server <see cref="Start"/> only validates.
    /// </summary>
    public sealed class SearchInteraction : IInteraction, IClientInteractionSource, IInteractionTierProvider
    {
        public const string GenericName = "Search";

        public static readonly SearchInteraction Instance = new();

        public int Priority => 20;

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Instant;

        public string GetName(InteractionEvent interactionEvent) => "Search";

        public string GetGenericName() => GenericName;

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Search);
        }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => new SearchClientInteraction();

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source is not Hand)
            {
                return false;
            }

            if (!TryResolveVictim(interactionEvent, out HumanInventory victim, out _))
            {
                return false;
            }

            HumanInventory searcher = interactionEvent.Source.GetComponentInParent<HumanInventory>();
            if (!CharacterLootUtility.IsOtherCharacter(searcher, victim))
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent)
                || (interactionEvent.Source is Hand hand && hand.CanInteract(victim.gameObject));
        }

        /// <summary>
        /// Server: no replicated state — paperdoll is client UI opened by
        /// <see cref="SearchClientInteraction"/>.
        /// </summary>
        public bool Start(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public static bool TryResolveVictim(
            InteractionEvent interactionEvent,
            out HumanInventory inventory,
            out IExaminable examinable)
        {
            inventory = null;
            examinable = null;

            GameObject targetObject = null;
            if (interactionEvent.Target is IGameObjectProvider provider)
            {
                targetObject = provider.GameObject;
            }
            else if (interactionEvent.Target is Component component)
            {
                targetObject = component.gameObject;
            }

            if (targetObject == null)
            {
                return false;
            }

            return CharacterExamineTargetUtility.TryResolveFromTransform(
                targetObject.transform,
                out examinable,
                out inventory);
        }
    }

    /// <summary>
    /// Client one-shot that opens the character examine paperdoll window.
    /// </summary>
    public sealed class SearchClientInteraction : IClientInteraction
    {
        public bool ClientStart(InteractionEvent interactionEvent)
        {
            if (!SearchInteraction.TryResolveVictim(interactionEvent, out _, out IExaminable examinable))
            {
                return false;
            }

            if (!SubSystems.TryGet(out ExamineSubSystem examineSystem) || examineSystem == null)
            {
                return false;
            }

            examineSystem.RequestCharacterWindow(examinable);
            return false;
        }

        public bool ClientUpdate(InteractionEvent interactionEvent) => false;

        public void ClientCancel(InteractionEvent interactionEvent)
        {
        }
    }
}
