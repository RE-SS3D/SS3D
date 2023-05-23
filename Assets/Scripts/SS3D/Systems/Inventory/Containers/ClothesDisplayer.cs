using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.UI;
using SS3D.Systems.Roles;
using UnityEngine;
using System.Collections;
using FishNet.Object.Synchronizing;
using System.ComponentModel;
using UnityEditor.Graphs;
using UnityEditor;

namespace SS3D.Systems.Inventory.Containers
{
    public class ClothesDisplayer : Actor
    {

        public HumanInventory _inventory;

        public GameObject Hat;
        public GameObject Eyes;
        public GameObject Jumpsuit;
        public GameObject Hands;
        public GameObject Feet;
        protected override void OnAwake()
        {
            base.OnAwake();
            _inventory.OnContainerContentChanged += ContainerContentChanged;
        }

        public void ContainerContentChanged(Container container, IEnumerable<Item> oldItems, IEnumerable<Item> newItems, ContainerChangeType type)
        {
            // If it's not a cloth type container. It'd be probably better to just create "cloth container" inheriting from container to easily test that.
            if(container.ContainerType < ContainerType.Shoes)
            {
                return;
            }

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

        public void ShowCloth(Container container, Item item, bool display)
        {
            switch (container.ContainerType)
            {
                case ContainerType.Shoes:
                    
                    break;

                case ContainerType.Gloves:
                    
                    break;

                case ContainerType.Glasses:
                    DisplayCloth(Eyes, item, display);
                    break;

                case ContainerType.Mask:
                    
                    break;

                case ContainerType.Ears:
                    
                    break;

                case ContainerType.Head:
                   
                    break;

                case ContainerType.ExoSuit:
                    
                    break;

                case ContainerType.Jumpsuit:
                    
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