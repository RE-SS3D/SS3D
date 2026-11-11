using SS3D.Core;
using SS3D.Systems.Comms;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Warns a conscious character the moment someone starts taking from them (inventory-storage.md
    /// §9, examine.md §5). Interim channel: a private line in their HUD feed. Phase 5 of the examine
    /// feedback effort replaces it with a target-only action message.
    /// </summary>
    public static class TakeFromCharacterWarning
    {
        public const string Header = "Sense";
        public const string BlindKey = "examine.take.warn_blind";
        public const string BlindFallback = "Someone is reaching into your pockets!";
        public const string OpenKey = "examine.take.warn_open";
        public const string OpenFallback = "Someone is trying to take your {0}!";

        public static readonly (string Key, string English)[] TableEntries =
        {
            (BlindKey, BlindFallback), (OpenKey, OpenFallback),
        };

        public static string Text(bool blind, Item item) => blind || item == null
            ? ExamineText.Get(BlindKey, BlindFallback)
            : ExamineText.Get(OpenKey, OpenFallback, item.Name.ToLowerInvariant());

        /// <summary>Server: warn the victim's player, if any. Dead, unconscious and unowned bodies get nothing.</summary>
        public static void ServerWarn(HumanInventory victim, bool blind, Item item)
        {
            if (victim == null || CharacterLootUtility.IsLootable(victim))
            {
                return;
            }

            FishNet.Connection.NetworkConnection owner = victim.Owner;
            if (owner == null || !owner.IsValid || !SubSystems.TryGet(out CommsSubSystem comms))
            {
                return;
            }

            comms.SendPrivateFeed(owner, Header, Text(blind, item));
        }
    }
}
