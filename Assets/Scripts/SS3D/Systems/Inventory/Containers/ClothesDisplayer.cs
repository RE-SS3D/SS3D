
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
    public class ClothesDisplayer : NetworkActor
    {
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

        [SyncVar(OnChange = nameof(OnChange))]
        private ClothDisplayData jumpsuitData;


        private void OnChange(ClothDisplayData oldValue, ClothDisplayData newValue, bool asServer)
        {

            if (asServer) return;
            Debug.Log("jumpsuit changed");
            Debug.Log("display ? " + newValue._display + "item : " + newValue._clothToDisplay + "on body part : " + newValue._bodyPart );


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
            _inventory.OnContainerContentChanged += ContainerContentChanged;
        }

        public void ContainerContentChanged(Container container, IEnumerable<Item> oldItems, IEnumerable<Item> newItems, ContainerChangeType type)
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

        // TODO complete with missing stuff (mask, etc..)
        public void ShowCloth(Container container, Item item, bool display)
        {
            switch (container.ContainerType)
            {
                case ContainerType.Identification:
                    DisplayCloth(Identification, item, display);
                    break;

                case ContainerType.Glasses:
                    DisplayCloth(Eyes, item, display);
                    break;

                case ContainerType.Mask:
                    break;

                case ContainerType.Head:
                    DisplayCloth(Hat, item, display);
                    break;

                case ContainerType.ExoSuit:
                    
                    break;

                case ContainerType.Jumpsuit:
                    DisplayCloth(Jumpsuit, item, display);
                    break;

                case ContainerType.ShoeLeft:
                    DisplayCloth(FootLeft, item, display);
                    break;

                case ContainerType.ShoeRight:
                    DisplayCloth(FootRight, item, display);
                    break;

                case ContainerType.GloveLeft:
                    DisplayCloth(HandLeft, item, display);
                    break;

                case ContainerType.GloveRight:
                    DisplayCloth(HandRight, item, display);
                    break;
            }
        }

        private void DisplayCloth(NetworkObject bodyPart, Item item, bool display)
        {
            jumpsuitData = new ClothDisplayData(bodyPart, display, item);
        }
    }
}