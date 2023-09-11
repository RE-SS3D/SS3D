
using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Hacks
{
    public class ClientCanSeeContentOfAllContainers : MonoBehaviour
    {
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log("See all containers");
                var containers = FindObjectsOfType<AttachedContainer>();
                foreach (var container in containers)
                {
                 
                    var items = container.Items;
                    foreach (var item in items)
                    {
                        Debug.Log(item + " in container " + container.name + " at position " + container.gameObject.transform.position);
                    }
                }
            }
        }
    }

}

