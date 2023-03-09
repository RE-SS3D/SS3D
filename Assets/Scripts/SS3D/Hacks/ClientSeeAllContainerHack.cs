using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Put that on the Human prefab, while roaming in the station, press L to see the content of all containers on the station.
/// </summary>
public class ClientSeeAllContainerHack : MonoBehaviour
{

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("See all containers");
            var containers = FindObjectsOfType<AttachedContainer>();
            foreach(var container in containers)
            {
                var items = container.Container.StoredItems;
                foreach(var item in items)
                {
                    Debug.Log(item.Item + " in container " + container.name + " at position " + container.gameObject.transform.position);
                }
            }  
        }
    }
}
