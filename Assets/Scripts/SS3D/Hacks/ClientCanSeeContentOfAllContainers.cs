using SS3D.Data;
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Systems.Inventory.Containers
{
    public class ClientCanSeeContentOfAllContainers : MonoBehaviour
    {
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log("See all containers");
                var containers = FindObjectsOfType<ContainerInteractive>();
                foreach (var container in containers)
                {
                    var items = container.attachedContainer.Container.StoredItems;
                    foreach (var item in items)
                    {
                        Debug.Log(item.Item + " in container " + container.name + " at position " + container.gameObject.transform.position);
                    }
                }
            }
        }
    }

}

