using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using System.Collections.Generic;
using System.Linq;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    public class ClothesDisplayer : Actor
    {

        public HumanInventory _inventory;

        public Transform ClothesRoot;

        // Game objects on the human prefab to display clothes.
        public GameObject Hat;
        public GameObject Eyes;
        public GameObject Jumpsuit;
        public GameObject HandLeft;
        public GameObject HandRight;
        public GameObject FootLeft;
        public GameObject FootRight;
        public GameObject Identification;
        public GameObject Backpack;



        protected override void OnStart()
        {
            base.OnStart();
            var renderers = ClothesRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }

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

        private void DisplayCloth(GameObject bodyPart, Item item, bool display)
        {
            if(!bodyPart.TryGetComponent(out SkinnedMeshRenderer renderer))       
            {
                Punpun.Warning(this, $"no skinned mesh renderer on game object {bodyPart}, can't display cloth");
                return;
            }

            if(display)
            {
                bodyPart.SetActive(true);
                renderer.sharedMesh = item.gameObject.GetComponentInChildren<MeshFilter>().mesh;
            }
            else
            {
                bodyPart.SetActive(false);
            }
        }
    }
}