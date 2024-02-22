using FishNet.Object;
using FishNet.Object.Synchronizing;
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
        
        [SyncObject]
        public readonly SyncList<string> AvailableChannels = new SyncList<string>();

        protected override void OnAwake()
        {
            List<string> initialAvailableChannels = chatChannels
                .GetChannels()
                .Where(x => 
                    !x.hidable
                    || x.requiredTraitInHeadset == null
                    || HasHeadsetForChatChannel(x))
                .Select(x => x.name)
                .ToList();

            foreach (string initialAvailableChannel in initialAvailableChannels)
            {
                AvailableChannels.Add(initialAvailableChannel);
            }
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                return;
            }
            
            InGameChatWindow inGameChatWindow = ViewLocator.Get<InGameChatWindow>().First();
            inGameChatWindow.availableChannels = AvailableChannels.ToList();
            inGameChatWindow.Initialize();
            
            AvailableChannels.OnChange += (_, _, _, _, _) => { inGameChatWindow.availableChannels = AvailableChannels.ToList(); };
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            humanInventory.OnContainerContentChanged += OnInventoryItemsUpdated;
        }
        
        [Server]
        private void OnInventoryItemsUpdated(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            if (container.ContainerType is not (ContainerType.EarLeft or ContainerType.EarRight))
            {
                return;
            }

            List<string> newAvailableChannels = GetListOfAvailableChannels();

            foreach (string currentAvailableChannel in AvailableChannels)
            {
                if (!newAvailableChannels.Contains(currentAvailableChannel))
                {
                    AvailableChannels.Remove(currentAvailableChannel);
                }
            }

            foreach (string newAvailableChannel in newAvailableChannels)
            {
                if (!AvailableChannels.Contains(newAvailableChannel))
                {
                    AvailableChannels.Add(newAvailableChannel);
                }
            }
        }

        [Server]
        private List<string> GetListOfAvailableChannels() => 
            chatChannels
                .GetChannels()
                .Where(x => 
                    !x.hidable
                    || x.requiredTraitInHeadset == null
                    || HasHeadsetForChatChannel(x))
                .Select(x => x.name)
                .ToList();

        [Server]
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