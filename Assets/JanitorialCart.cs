using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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
        
        Transform[] transforms = this.transform.GetComponentsInChildren<Transform>();
        foreach(Transform transform in transforms)
        {
            GameObject cartItem;
            switch (transform.name)
            {
                case "JanitorialCart_Spot_Box1":
                    cartItem = Instantiate(boxOne, transform.position, transform.rotation);                 
                    break;
                case "JanitorialCart_Spot_Box2":
                    cartItem = Instantiate(boxTwo, transform.position, transform.rotation); 
                    break;
                case "JanitorialCart_Spot_Mop":
                    cartItem = Instantiate(mop, transform.position, transform.rotation);
                    break;
                case "JanitorialCart_Spot_MopBucket":
                    cartItem = Instantiate(mopBucket, transform.position, transform.rotation);
                    break;
                case "JanitorialCart_Spot_Rag1":
                    cartItem = Instantiate(ragOne, transform.position, transform.rotation);
                    break;
                case "JanitorialCart_Spot_Rag2":
                    cartItem = Instantiate(ragTwo, transform.position, transform.rotation);
                    break;
                case "JanitorialCart_Spot_Rag3":
                    cartItem = Instantiate(ragThree, transform.position, transform.rotation);
                    break;
                case "JanitorialCart_Spot_Tray1":
                    cartItem = Instantiate(spaceCleaner, transform.position, transform.rotation);
                    break;
                case "JanitorialCart_Spot_Tray2":
                    cartItem = Instantiate(soap, transform.position, transform.rotation);
                    break;
                default:
                    cartItem = null;
                    break;
            }

            if(cartItem != null)
            {
                cartItem.transform.parent = transform;
                NetworkServer.Spawn(cartItem);
            } 
        }
    }
}
