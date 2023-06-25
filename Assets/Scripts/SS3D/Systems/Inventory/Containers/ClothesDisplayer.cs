
using System.Collections.Generic;
using System.Linq;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using FishNet.Object.Synchronizing;
using UnityEditor;
using FishNet.Object;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Display cloth on player for all clients.
    /// </summary>
    public class ClothesDisplayer : NetworkActor
    {
        /// <summary>
        /// A small structure containing information regarding clothes on player, to help syncing them.
        /// For each bodypart that can have clothing, it also contains information on the item to display, if it should show or not.
        /// </summary>
        private struct ClothDisplayData
        {
            public ClothDisplayData(NetworkObject bodyPart, bool display, Item clothToDisplay)
            {
                _bodyPart= bodyPart;
                _display= display;
                _clothToDisplay= clothToDisplay;
            }
            public NetworkObject _bodyPart;
            public bool _display;
            public Item _clothToDisplay;
        }

        public HumanInventory _inventory;

        // The root game objects for clothes
        public Transform ClothesRoot;

        // Game objects on the human prefab to display clothes.
        public NetworkObject Hat;
        public NetworkObject Eyes;
        public NetworkObject Jumpsuit;
        public NetworkObject HandLeft;
        public NetworkObject HandRight;
        public NetworkObject FootLeft;
        public NetworkObject FootRight;
        public NetworkObject Identification;
        public NetworkObject Backpack;
	    public NetworkObject Face;
        public NetworkObject EarLeft;
        public NetworkObject EarRight;

        // Syncvar to sync meshes and cloth display between clients
        [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _hatData;

        [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _eyesData;

        [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _jumpsuitData;

        [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _handLeftData;

        [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _handRightData;

        [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _footLeftData;

        [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _footRightData;

        [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _identificationData;

        [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _backpackData;

	    [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _faceData;

        [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _earLeftData;

        [SyncVar(OnChange = nameof(SyncCloth))]
        private ClothDisplayData _earRightData;


        /// <summary>
        /// Sync cloth on client.
        /// </summary>
        private void SyncCloth(ClothDisplayData oldValue, ClothDisplayData newValue, bool asServer)
        {

            if (asServer) return;

            bool display = newValue._display;
            var bodyPart = newValue._bodyPart;
            var item = newValue._clothToDisplay;

            if (!bodyPart.TryGetComponent(out SkinnedMeshRenderer renderer))
            {
                Punpun.Warning(this, $"no skinned mesh renderer on game object {bodyPart}, can't display cloth");
                return;
            }

            if (display)
            {
                bodyPart.gameObject.SetActive(true);
                renderer.sharedMesh = item.gameObject.GetComponentInChildren<MeshFilter>().sharedMesh;
            }
            else
            {
                bodyPart.gameObject.SetActive(false);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
        }

        protected override void OnStart()
        {
            base.OnStart();
            _inventory.OnContainerContentChanged += HandleContainerContentChanged;
        }

        /// <summary>
        /// When the content of a container change, check if it should display or remove display of some clothes.
        /// </summary>
        public void HandleContainerContentChanged(Container container, IEnumerable<Item> oldItems, IEnumerable<Item> newItems, ContainerChangeType type)
        {
            // If it's not a cloth type container.
            // It'd be probably better to just create "cloth container" inheriting from container to easily test that.
            if(container.ContainerType < ContainerType.Bag)
            {
                return;
            }

            // TODO : check that the change include a single item.
            // Also maybe don't check only new item for remove ?
            var item = newItems.FirstOrDefault();

            switch(type)
            {
                case ContainerChangeType.Add:
                    ShowCloth(container, item, true);
                    break;
                case ContainerChangeType.Remove:
                    ShowCloth(container, item, false);
                    break;
            }
        }

        /// <summary>
        /// Show the right cloth depending on which container type has been modified.
        /// </summary>
        public void ShowCloth(Container container, Item item, bool display)
        {
            switch (container.ContainerType)
            {
                case ContainerType.Identification:
                    _identificationData = new ClothDisplayData(Identification, display, item);
                    break;

                case ContainerType.Glasses:
                    _eyesData= new ClothDisplayData(Eyes, display, item);
                    break;

                case ContainerType.Mask:
		            _faceData= new ClothDisplayData(Face, display, item);
                    break;

                case ContainerType.Head:
                    _hatData = new ClothDisplayData(Hat, display, item);
                    break;

		        case ContainerType.Bag:
                    _backpackData = new ClothDisplayData(Backpack, display, item);
                    break;

                case ContainerType.ExoSuit:
                    break;

                case ContainerType.Jumpsuit:
                    _jumpsuitData = new ClothDisplayData(Jumpsuit, display, item);
                    break;

                case ContainerType.ShoeLeft:
                    _footLeftData = new ClothDisplayData(FootLeft, display, item);
                    break;

                case ContainerType.ShoeRight:
                    _footRightData = new ClothDisplayData(FootRight, display, item);
                    break;

                case ContainerType.GloveLeft:
                    _handLeftData = new ClothDisplayData(HandLeft, display, item);
                    break;

                case ContainerType.GloveRight:
                    _handRightData = new ClothDisplayData(HandRight, display, item);
                    break;

                case ContainerType.EarLeft:
                    _earLeftData = new ClothDisplayData(EarLeft, display, item);
                    break;

                case ContainerType.EarRight:
                    _earRightData = new ClothDisplayData(EarRight, display, item);
                    break;
            }
        }
    }
}