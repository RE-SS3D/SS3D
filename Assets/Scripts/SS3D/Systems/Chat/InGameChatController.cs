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
        [SerializeField] private ChatChannels _chatChannels;
        [SerializeField] private HumanInventory _humanInventory;
        
        [SyncObject]
        public readonly SyncList<string> AvailableChannels = new SyncList<string>();

        protected override void OnAwake()
        {
            List<string> initialAvailableChannels = _chatChannels
                .GetChannels()
                .Where(x => 
                    !x.Hidable
                    || x.RequiredTraitInHeadset == null
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
            inGameChatWindow.AvailableChannels = AvailableChannels.ToList();
            inGameChatWindow.Initialize();
            
            AvailableChannels.OnChange += (_, _, _, _, _) => { inGameChatWindow.AvailableChannels = AvailableChannels.ToList(); };
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            _humanInventory.OnContainerContentChanged += OnInventoryItemsUpdated;
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
            _chatChannels
                .GetChannels()
                .Where(x => 
                    !x.Hidable
                    || x.RequiredTraitInHeadset == null
                    || HasHeadsetForChatChannel(x))
                .Select(x => x.name)
                .ToList();

        [Server]
        private bool HasHeadsetForChatChannel(ChatChannel chatChannel)
        {
            if (_humanInventory == null)
            {
                return false;
            }

            if (_humanInventory.TryGetTypeContainer(ContainerType.EarLeft, 0, out AttachedContainer earLeftContainer)
                && earLeftContainer.Items.Any(x => x.HasTrait(chatChannel.RequiredTraitInHeadset)))
            {
                return true;
            }

            if (_humanInventory.TryGetTypeContainer(ContainerType.EarRight, 0, out AttachedContainer earRightContainer)
                && earRightContainer.Items.Any(x => x.HasTrait(chatChannel.RequiredTraitInHeadset)))
            {
                return true;
            }

            return false;
        }
    }
}