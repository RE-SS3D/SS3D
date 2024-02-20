using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    public sealed class InGameChatController : NetworkActor
    {
        [SerializeField] private ChatChannels chatChannels;
        [SerializeField] private HumanInventory humanInventory;
        
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                return;
            }
            
            InGameChatWindow inGameChatWindow = ViewLocator.Get<InGameChatWindow>().First();
            inGameChatWindow.availableChannels = GetListOfAvailableChatChannels();
            inGameChatWindow.Initialize();

            humanInventory.OnContainerContentChanged += OnInventoryItemsUpdated;
        }

        private void OnInventoryItemsUpdated(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            if (container.ContainerType is not (ContainerType.EarLeft or ContainerType.EarRight))
            {
                return;
            }

            ViewLocator.Get<InGameChatWindow>().First().availableChannels = GetListOfAvailableChatChannels();
        }

        private List<string> GetListOfAvailableChatChannels() => 
            chatChannels
                .GetChannels()
                .Where(x => 
                    !x.hidable
                    || x.requiredTraitInHeadset == null
                    || HasHeadsetForChatChannel(x))
                .Select(x => x.name)
                .ToList();

        private bool HasHeadsetForChatChannel(ChatChannel chatChannel)
        {
            if (humanInventory == null)
            {
                return false;
            }

            if (humanInventory.TryGetTypeContainer(ContainerType.EarLeft, 0, out AttachedContainer earLeftContainer)
                && earLeftContainer.Items.Any(x => x.HasTrait(chatChannel.requiredTraitInHeadset)))
            {
                return true;
            }

            if (humanInventory.TryGetTypeContainer(ContainerType.EarRight, 0, out AttachedContainer earRightContainer)
                && earRightContainer.Items.Any(x => x.HasTrait(chatChannel.requiredTraitInHeadset)))
            {
                return true;
            }

            return false;
        }
    }
}