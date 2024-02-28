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
    
    public Transform rightHandHoldTransform;

    public Transform leftHandHoldTransform;
    
    public Transform GunHoldTransform;

    public GameObject ItemInSelectedHand
    { 
        get => selectedHand == Hand.LeftHand ? itemInLeftHand : itemInRightHand;
        private set
        {
            if (selectedHand == Hand.LeftHand)
            {
                itemInLeftHand = value;

                if (value is not null)
                {
                    value.transform.parent = rightHandHoldTransform;
                    value.transform.parent = GunHoldTransform;
                }

                leftHandFull = itemInLeftHand is not null;

            }
            else
            {
                itemInRightHand = value;

                if (value is not null)
                {
                    value.transform.parent = leftHandHoldTransform;
                    value.transform.parent = GunHoldTransform;
                }

                rightHandFull = itemInRightHand is not null;
            }
            

        } 

    } 

    public bool IsSelectedHandEmpty => selectedHand == Hand.LeftHand ? !leftHandFull : !rightHandFull;
    

    // Update is called once per frame
    private void Update()
    {
        if (!Input.GetKey(KeyCode.X))
            return;

        selectedHand = selectedHand == Hand.LeftHand ? Hand.RightHand : Hand.LeftHand;
        
        Debug.Log($"Selected hand is {selectedHand}");
    }

    public void RemoveItemFromSelectedHand()
    {
        ItemInSelectedHand.transform.parent = null;
        ItemInSelectedHand.gameObject.GetComponent<Rigidbody>().isKinematic = false;
        ItemInSelectedHand.gameObject.GetComponent<Collider>().enabled = true;
        ItemInSelectedHand = null;
    }

    public void AddItemToSelectedHand(GameObject item)
    {
        ItemInSelectedHand = item;
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        item.GetComponent<Rigidbody>().isKinematic = true;
        item.GetComponent<Collider>().enabled = false;
    }
}
