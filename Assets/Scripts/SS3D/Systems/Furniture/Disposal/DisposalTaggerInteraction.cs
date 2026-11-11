using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// Quick tagger interaction available at a chute (design doc §4/§9) — cycles the held item's
    /// disposal destination label through the department list, wrapping back to untagged. The current
    /// tag reads via examine (<see cref="DisposalTag"/> is its own provider), same manifest-on-the-object
    /// convention as a Cargo crate.
    /// </summary>
    public sealed class DisposalTaggerInteraction : IInteraction
    {
        private static readonly Department[] Cycle =
        {
            Department.None,
            Department.Command,
            Department.Security,
            Department.Engineering,
            Department.Medical,
            Department.Science,
            Department.Cargo,
            Department.Service,
            Department.Civilian,
        };

        public string GetName(InteractionEvent interactionEvent) => "Tag for disposal";

        public string GetGenericName() => "TagDisposal";

        public Sprite GetIcon(InteractionEvent interactionEvent) => null;

        /// <summary>
        /// Below Dispose (40) so primary-click dumps into the chute; above Drop (5) so tagging
        /// still beats floor-drop when chosen from the radial.
        /// </summary>
        public int Priority => 20;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            if (interactionEvent.Source is not IGameObjectProvider sourceProvider)
            {
                return false;
            }

            Hands hands = sourceProvider.GameObject.GetComponentInParent<Hands>();
            return hands != null && !hands.SelectedHand.IsEmpty();
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source is not IGameObjectProvider sourceProvider)
            {
                return false;
            }

            Hands hands = sourceProvider.GameObject.GetComponentInParent<Hands>();
            Item item = hands?.SelectedHand.ItemInHand;
            if (item == null)
            {
                return false;
            }

            DisposalTag tag = item.GetComponent<DisposalTag>();
            if (tag == null)
            {
                tag = item.gameObject.AddComponent<DisposalTag>();
            }

            int currentIndex = System.Array.IndexOf(Cycle, tag.Destination);
            Department next = Cycle[(currentIndex + 1) % Cycle.Length];
            tag.SetDestination(next);

            return false;
        }
    }
}
