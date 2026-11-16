using System.Collections.Generic;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Construction
{
    public enum ConstructionMaterialKind : byte
    {
        SteelSheet = 0,
        GlassSheet = 1,
        FloorTile = 2,
    }

    /// <summary>
    /// Marks a stack item as a construction material and offers its object steps (plate or glaze a
    /// girder). Cell placement with a ghost (plating, floor, girder) is <c>ConstructionPlacementSubSystem</c>'s.
    /// Plain MonoBehaviour like <see cref="ConstructionTool"/>; added by <c>ConstructionItemRecipe</c>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Item))]
    public sealed class ConstructionMaterial : MonoBehaviour, IInteractionSourceExtension, IOutlineSourceExtension
    {
        [SerializeField] private ConstructionMaterialKind _kind;

        public ConstructionMaterialKind Kind => _kind;

        public ConstructionInput Input =>
            _kind switch
            {
                ConstructionMaterialKind.GlassSheet => ConstructionInput.GlassSheet,
                ConstructionMaterialKind.FloorTile => ConstructionInput.FloorTile,
                _ => ConstructionInput.SteelSheet,
            };

        public void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions, InteractionEvent context) =>
            ConstructionInteractions.AddSourceInteractions(Input, targets, interactions, context);

        public void CollectOutlineInteractions(InteractionEvent interactionEvent, List<IInteraction> into) =>
            ConstructionInteractions.CollectFor(Input, interactionEvent, into);
    }
}
