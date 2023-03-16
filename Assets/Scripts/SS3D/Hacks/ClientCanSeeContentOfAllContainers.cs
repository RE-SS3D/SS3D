using SS3D.Systems.Inventory.Containers;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Hacks
{
    public class ClientCanSeeContentOfAllContainers : MonoBehaviour
    {
        public void Update()
        {
            if (!Input.GetKeyDown(KeyCode.L))
            {
                return;
            }

            Debug.Log("See all containers");
            ContainerInteractive[] containers = FindObjectsOfType<ContainerInteractive>();
            foreach (ContainerInteractive container in containers)
            {
                List<StoredItem> items = container.attachedContainer.Container.StoredItems;
                
                foreach (StoredItem item in items)
                {
                    Debug.Log(item.Item + " in container " + container.name + " at position " + container.gameObject.transform.position);
                }
            }
        }
    }

}

