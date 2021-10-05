using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using SS3D.Engine.Inventory;

/// <summary>
/// Handles creating the items going on the janitorial cart at runtime.
/// This is necessary as Mirror doesn't support nested Network Identities.
/// </summary>
public class JanitorialCart : NetworkBehaviour
{
    public GameObject boxOne;
    public GameObject boxTwo;
    public GameObject mop;
    public GameObject mopBucket;
    public GameObject ragOne;
    public GameObject ragTwo;
    public GameObject ragThree;
    public GameObject spaceCleaner;
    public GameObject soap;



    void Start()
    {

        GameObject boxOneInstance = null;
        GameObject boxTwoInstance = null;
        GameObject mopInstance = null;
        GameObject mopBucketInstance = null;
        GameObject ragOneInstance = null;
        GameObject ragTwoInstance = null;
        GameObject ragThreeInstance = null;
        GameObject spaceCleanerInstance = null;
        GameObject soapInstance = null;

        // Go through each transform on the cart that must have an item and instantiate the correct item at the correct position.
        Transform[] transforms = this.transform.GetComponentsInChildren<Transform>();
        foreach(Transform transform in transforms)
        {
            GameObject cartItem;
            switch (transform.name)
            {
                case "JanitorialCart_Spot_Box1":
                    cartItem = Instantiate(boxOne, transform.position, transform.rotation);
                    boxOneInstance = cartItem;
                    break;
                case "JanitorialCart_Spot_Box2":
                    cartItem = Instantiate(boxTwo, transform.position, transform.rotation);
                    boxTwoInstance = cartItem;
                    break;
                case "JanitorialCart_Spot_Mop":
                    cartItem = Instantiate(mop, transform.position, transform.rotation);
                    mopInstance = cartItem;
                    break;
                case "JanitorialCart_Spot_MopBucket":
                    cartItem = Instantiate(mopBucket, transform.position, transform.rotation);
                    mopBucketInstance = cartItem;
                    break;
                case "JanitorialCart_Spot_Rag1":
                    cartItem = Instantiate(ragOne, transform.position, transform.rotation);
                    ragOneInstance = cartItem;
                    break;
                case "JanitorialCart_Spot_Rag2":
                    cartItem = Instantiate(ragTwo, transform.position, transform.rotation);
                    ragTwoInstance = cartItem;
                    break;
                case "JanitorialCart_Spot_Rag3":
                    cartItem = Instantiate(ragThree, transform.position, transform.rotation);
                    ragThreeInstance = cartItem;
                    break;
                case "JanitorialCart_Spot_Tray1":
                    cartItem = Instantiate(spaceCleaner, transform.position, transform.rotation);
                    spaceCleanerInstance = cartItem;
                    break;
                case "JanitorialCart_Spot_Tray2":
                    cartItem = Instantiate(soap, transform.position, transform.rotation);
                    soapInstance = cartItem;
                    break;
                default:
                    cartItem = null;
                    break;
            }

            // Set the item tranforms to be child of the transforms on the cart. 
            // Spawn the item for all clients
            if(cartItem != null)
            {
                cartItem.transform.parent = transform;
                NetworkServer.Spawn(cartItem);
            } 
        }

        var containerDescriptors = GetComponentsInChildren<ContainerDescriptor>();
        foreach(ContainerDescriptor containerDescriptor in containerDescriptors)
        {
            switch (containerDescriptor.containerName)
            {
                case "box spot":
                    Debug.Log("add boxes to box spot");
                    if(boxOneInstance != null)
                        containerDescriptor.attachedContainer.Container.AddItem(boxOneInstance.gameObject.GetComponent<Item>());
                    if (boxTwoInstance != null)
                        containerDescriptor.attachedContainer.Container.AddItem(boxTwoInstance.gameObject.GetComponent<Item>());
                    break;
                case "rag spot":
                    if (ragOneInstance != null)
                        containerDescriptor.attachedContainer.Container.AddItem(ragOneInstance.gameObject.GetComponent<Item>());
                    if (ragTwoInstance != null)
                        containerDescriptor.attachedContainer.Container.AddItem(ragTwoInstance.gameObject.GetComponent<Item>());
                    if (ragThreeInstance != null)
                        containerDescriptor.attachedContainer.Container.AddItem(ragThreeInstance.gameObject.GetComponent<Item>());
                    break;
            }
        }
    }
}
