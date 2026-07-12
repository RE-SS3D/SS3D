using System.Linq;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items.Generic;

namespace SS3D.Systems.IdAccess
{
    /// <summary>
    /// Resolves the on-person credential from gear-strip slots per design/id-access.md §3.
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

            if (TryGetIdCardFromContainer(inventory, ContainerType.Identification, out idCard))
            {
                recordId = idCard.BoundRecordId;
                return !recordId.IsNone;
            }

            if (TryGetIdCardFromPdaContainer(inventory, ContainerType.Pda, out idCard))
            {
                recordId = idCard.BoundRecordId;
                return !recordId.IsNone;
            }

            // Backward compatibility: PDA currently lives in the Identification slot.
            if (TryGetIdCardFromPdaContainer(inventory, ContainerType.Identification, out idCard))
            {
                recordId = idCard.BoundRecordId;
                return !recordId.IsNone;
            }

            return false;
        }

        private static bool TryGetIdCardFromContainer(
            HumanInventory inventory,
            ContainerType containerType,
            out IDCard idCard)
        {
            idCard = null;

            if (!inventory.TryGetTypeContainer(containerType, 0, out AttachedContainer container))
            {
                return false;
            }

            idCard = container.Items.FirstOrDefault() as IDCard;
            return idCard != null && !idCard.BoundRecordId.IsNone;
        }

        private static bool TryGetIdCardFromPdaContainer(
            HumanInventory inventory,
            ContainerType containerType,
            out IDCard idCard)
        {
            idCard = null;

            if (!inventory.TryGetTypeContainer(containerType, 0, out AttachedContainer container))
            {
                return false;
            }

            if (container.Items.FirstOrDefault() is not PDA pda)
            {
                return false;
            }

            idCard = pda.GetInsertedIdCard();
            return idCard != null && !idCard.BoundRecordId.IsNone;
        }
    }
}
