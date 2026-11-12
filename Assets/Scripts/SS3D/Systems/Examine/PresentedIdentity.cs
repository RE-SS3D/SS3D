using System.Collections.Generic;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// The identity a character is presenting (examine.md §5, id-access.md §3): the card in their ID
    /// or PDA slot, bare or inside a worn PDA, or a card held in a hand. A card in a pocket or bag does
    /// not count. Examine and action messages both name characters through this, so a disguise reads
    /// the same everywhere.
    /// </summary>
    public readonly struct PresentedIdentity
    {
        private const string UnknownInOutfitKey = "character_examine.unknown_in_outfit";
        private const string UnknownInOutfitFallback = "Unknown crew member in {0}";
        private const string UnidentifiedKey = "character_examine.unidentified_crew_member";
        private const string UnidentifiedFallback = "Unidentified Crew Member";

        /// <summary>Slots whose card counts as presented, in priority order.</summary>
        private static readonly ContainerType[] WornIdSlots = { ContainerType.Identification, ContainerType.Pda };

        /// <summary>Outer layer first: what an onlooker sees of the outfit.</summary>
        private static readonly ContainerType[] OutfitSlots = { ContainerType.ExoSuit, ContainerType.Jumpsuit };

        public string Name { get; }

        public string Job { get; }

        public Department Department { get; }

        public PresentedIdentity(string name, string job, Department department)
        {
            Name = name ?? string.Empty;
            Job = job ?? string.Empty;
            Department = department;
        }

        /// <summary>The job wrapped in its department keyword tag (examine.md §3), or empty.</summary>
        public string TaggedJob => string.IsNullOrWhiteSpace(Job)
            ? string.Empty
            : Department == Department.None
                ? Job
                : $"<k={ExamineKeywordFormatter.DepartmentPrefix}{Department.ToString().ToLowerInvariant()}>{Job}</k>";

        /// <summary>Resolves the presented identity of <paramref name="inventory"/>.</summary>
        public static bool TryResolve(HumanInventory inventory, out PresentedIdentity identity)
        {
            identity = default;
            if (inventory == null)
            {
                return false;
            }

            List<Item> worn = new();
            foreach (ContainerType slot in WornIdSlots)
            {
                if (inventory.TryGetTypeContainer(slot, 0, out AttachedContainer container))
                {
                    worn.AddRange(container.Items);
                }
            }

            List<Item> held = new();
            if (inventory.Hands != null)
            {
                foreach (Hand hand in inventory.Hands.PlayerHands)
                {
                    if (hand != null && hand.ItemInHand != null)
                    {
                        held.Add(hand.ItemInHand);
                    }
                }
            }

            if (!TryPickCard(worn, held, out IDCard card))
            {
                return false;
            }

            identity = new PresentedIdentity(card.OwnerName, card.RoleName, card.Department);
            return true;
        }

        /// <summary>
        /// The card presented by these items: a named card or a PDA holding one in a worn ID slot, else a
        /// named card in a hand. A held PDA does not present its card.
        /// </summary>
        public static bool TryPickCard(IEnumerable<Item> wornIdSlotItems, IEnumerable<Item> heldItems, out IDCard card)
        {
            card = null;
            if (wornIdSlotItems != null)
            {
                foreach (Item item in wornIdSlotItems)
                {
                    IDCard candidate = item is PDA pda ? pda.GetInsertedIdCard() : item as IDCard;
                    if (IsNamed(candidate))
                    {
                        card = candidate;
                        return true;
                    }
                }
            }

            if (heldItems != null)
            {
                foreach (Item item in heldItems)
                {
                    if (item is IDCard candidate && IsNamed(candidate))
                    {
                        card = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// What examine calls a character: the presented name, else a short look at their outfit
        /// ("Unknown crew member in engineering jumpsuit").
        /// </summary>
        public static string DisplayName(HumanInventory inventory)
        {
            if (TryResolve(inventory, out PresentedIdentity identity))
            {
                return identity.Name;
            }

            return ShortLook(inventory);
        }

        /// <summary>Short look for a character presenting no ID.</summary>
        public static string ShortLook(HumanInventory inventory)
        {
            string outfit = OutfitName(inventory);
            if (string.IsNullOrWhiteSpace(outfit))
            {
                return ExamineText.Get(UnidentifiedKey, UnidentifiedFallback);
            }

            return ExamineText.Get(UnknownInOutfitKey, UnknownInOutfitFallback, outfit.ToLowerInvariant());
        }

        /// <summary>English seed for the Examine table.</summary>
        public static readonly (string Key, string English)[] TableEntries =
        {
            (UnknownInOutfitKey, UnknownInOutfitFallback),
            (UnidentifiedKey, UnidentifiedFallback),
        };

        private static string OutfitName(HumanInventory inventory)
        {
            if (inventory == null)
            {
                return null;
            }

            foreach (ContainerType slot in OutfitSlots)
            {
                if (inventory.TryGetTypeContainer(slot, 0, out AttachedContainer container))
                {
                    foreach (Item item in container.Items)
                    {
                        if (item != null)
                        {
                            return item.Name;
                        }
                    }
                }
            }

            return null;
        }

        private static bool IsNamed(IDCard card) => card != null && !string.IsNullOrWhiteSpace(card.OwnerName);
    }
}
