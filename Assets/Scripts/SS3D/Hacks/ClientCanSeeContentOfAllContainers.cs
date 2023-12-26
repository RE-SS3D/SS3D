using SS3D.Core;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Hacks
{
    public class ClientCanSeeContentOfAllContainers : MonoBehaviour
    {
        public void Start()
        {
            Subsystems.Get<InputSystem>().Inputs.Other.SeeContainerContents.performed += SeeContents;
        }

        private void OnDestroy()
        {
            Subsystems.Get<InputSystem>().Inputs.Other.SeeContainerContents.performed -= SeeContents;
        }

        private void SeeContents(InputAction.CallbackContext callbackContext)
        {
            Debug.Log("See all containers");
            AttachedContainer[] containers = FindObjectsOfType<AttachedContainer>();
            foreach (AttachedContainer container in containers)
            {
                IEnumerable<Item> items = container.Items;
                foreach (Item item in items)
                {
                    Debug.Log(item + " in container " + container.name + " at position " + container.gameObject.transform.position);
                }
            }
        }
    }

}

