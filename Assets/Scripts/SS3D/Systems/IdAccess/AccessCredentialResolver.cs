using System.Collections.Generic;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;

namespace SS3D.Systems.IdAccess
{
    /// <summary>
    /// Resolves the on-person credential from any container on the character's inventory or hands.
    /// </summary>
    public static class AccessCredentialResolver
    {
        public static bool TryResolveBoundRecord(
            HumanInventory inventory,
            out CrewRecordId recordId,
            out IDCard idCard)
        {
            recordId = default;
            idCard = null;

            if (inventory == null)
            {
                return false;
            }

            foreach (AttachedContainer container in GetSearchContainers(inventory))
            {
                if (!TryGetIdCardFromAttachedContainer(container, out idCard))
                {
                    continue;
                }

                recordId = idCard.BoundRecordId;
                if (!recordId.IsNone)
                {
                    return true;
                }
            }

            recordId = default;
            idCard = null;
            return false;
        }

        private static IEnumerable<AttachedContainer> GetSearchContainers(HumanInventory inventory)
        {
            var seen = new HashSet<AttachedContainer>();

            List<AttachedContainer> containers = inventory.Containers;
            if (containers != null)
            {
                foreach (AttachedContainer container in containers)
                {
                    if (container != null && seen.Add(container))
                    {
                        yield return container;
                    }
                }
            }

            Hands hands = inventory.Hands;
            if (hands == null)
            {
                yield break;
            }

            foreach (AttachedContainer container in hands.HandContainers)
            {
                if (container != null && seen.Add(container))
                {
                    yield return container;
                }
            }
        }

        private static bool TryGetIdCardFromAttachedContainer(AttachedContainer container, out IDCard idCard)
        {
            idCard = null;

            foreach (Item item in container.Items)
            {
                if (item is IDCard directCard && !directCard.BoundRecordId.IsNone)
                {
                    idCard = directCard;
                    return true;
                }

                if (item is PDA pda
                    && pda.GetInsertedIdCard() is IDCard insertedCard
                    && !insertedCard.BoundRecordId.IsNone)
                {
                    idCard = insertedCard;
                    return true;
                }
            }

            return false;
        }
    }
}
