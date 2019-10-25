using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FurnitureDescriptor : MonoBehaviour
{
    // Compontent that should be attached to every furniture prefab. It holds flags of what the furniture type is, where it can go, etc.
    public enum FurnitureType{
        wall_furniture,
        floor_furniture,
        pipe_furniture,
        wire_furniture,
        door_furniture
    }

    public FurnitureType furnitureType;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
