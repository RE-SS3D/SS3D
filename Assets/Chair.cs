using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chair : MonoBehaviour
{
    [SerializeField]
    GameObject seat_slot;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnMouseDown()
    {
        Sit();
    }

    void Sit(GameObject target)
    {
        target.transform.position = seat_slot.transform.position;
    }
}
