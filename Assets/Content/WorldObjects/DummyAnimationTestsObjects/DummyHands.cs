using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyHands : MonoBehaviour
{

    public enum Hand
    {
        LeftHand = 0,
        RightHand = 1,
    }

    public bool leftHandFull;
    public bool rightHandFull;

    public Hand selectedHand = Hand.LeftHand;

    public GameObject itemInLeftHand;
    public GameObject itemInRightHand;

    public Hand UnselectedHand => 
        selectedHand == Hand.LeftHand ? Hand.RightHand : Hand.LeftHand;

    public GameObject ItemInSelectedHand
    { 
        get => selectedHand == Hand.LeftHand ? itemInLeftHand : itemInRightHand;
        private set
        {
            if (selectedHand == Hand.LeftHand)
            {
                itemInLeftHand = value;
                leftHandFull = itemInLeftHand is not null;

            }
            else
            {
                itemInRightHand = value;
                rightHandFull = itemInRightHand is not null;
            }
            
        } 

    } 
    
    public bool IsNonSelectedHandEmpty => selectedHand == Hand.LeftHand ? !rightHandFull : !leftHandFull;

    public bool IsSelectedHandEmpty => selectedHand == Hand.LeftHand ? !leftHandFull : !rightHandFull;

    public bool BothHandEmpty => !leftHandFull && !rightHandFull;
    

    // Update is called once per frame
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.X))
            return;

        selectedHand = selectedHand == Hand.LeftHand ? Hand.RightHand : Hand.LeftHand;
        
        Debug.Log($"Selected hand is {selectedHand}");
    }

    public DummyItem RemoveItemFromSelectedHand()
    {
        GameObject item = ItemInSelectedHand;
        ItemInSelectedHand.transform.parent = null;
        ItemInSelectedHand.gameObject.GetComponent<Rigidbody>().isKinematic = false;
        ItemInSelectedHand.gameObject.GetComponent<Collider>().enabled = true;
        ItemInSelectedHand = null;

        return item.GetComponent<DummyItem>();
    }

    public void AddItemToSelectedHand(GameObject item)
    {
        ItemInSelectedHand = item;
        item.GetComponent<Rigidbody>().isKinematic = true;
        item.GetComponent<Collider>().enabled = false;
    }
}
