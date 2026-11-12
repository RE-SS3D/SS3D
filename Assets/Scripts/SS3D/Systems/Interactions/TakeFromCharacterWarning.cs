using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.ActionMessages;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Warns a conscious character the moment someone starts taking from them (inventory-storage.md
    /// §9, examine.md §5): a target-only action line naming the taker (action-messages.md §4).
    /// </summary>
    public static class TakeFromCharacterWarning
    {
        /// <summary>Server: warn the victim. Dead, unconscious and unowned bodies get nothing.</summary>
        public static void ServerWarn(NetworkObject taker, HumanInventory victim, bool blind, Item item)
        {
            if (victim == null || CharacterLootUtility.IsLootable(victim)
                || !SubSystems.TryGet(out ActionMessageSubSystem actions))
            {
                return;
            }

            actions.Narrate(
                blind ? ActionMessageKeys.TakeBlind : ActionMessageKeys.TakeOpen,
                taker,
                victim.NetworkObject,
                item != null ? item.NetworkObject : null,
                ActionVisibility.TargetOnly);
        }
    }
}
