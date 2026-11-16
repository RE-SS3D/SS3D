using System.Collections.Generic;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Construction
{
    public enum ConstructionToolKind : byte
    {
        Welder = 0,
        Crowbar = 1,
    }

    /// <summary>
    /// Marks an item as a construction tool and offers its tool-side steps (weld, pry, cut) on the
    /// hovered structure. Plain MonoBehaviour: it carries no network state, so adding it to an existing
    /// item prefab does not change the NetworkObject's behaviour cache. Added by
    /// <c>ConstructionItemRecipe</c> (D5 of 2026-11_construction-mvp1).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Item))]
    public sealed class ConstructionTool : MonoBehaviour, IInteractionSourceExtension, IOutlineSourceExtension
    {
        [SerializeField] private ConstructionToolKind _kind;

        public ConstructionToolKind Kind => _kind;

        public ConstructionInput Input => _kind == ConstructionToolKind.Welder ? ConstructionInput.Welder : ConstructionInput.Crowbar;

        /// <summary>Shared "is this a crowbar" check for construction and later doors and cables.</summary>
        public static bool Is(Item item, ConstructionToolKind kind) =>
            item != null && item.TryGetComponent(out ConstructionTool tool) && tool._kind == kind;

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions, InteractionEvent context) =>
            ConstructionInteractions.AddSourceInteractions(Input, targets, interactions, context);

        public void CollectOutlineInteractions(InteractionEvent interactionEvent, List<IInteraction> into) =>
            ConstructionInteractions.CollectFor(Input, interactionEvent, into);
    }
}
