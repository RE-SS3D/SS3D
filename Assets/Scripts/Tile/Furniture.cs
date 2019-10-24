using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Furniture : MonoBehaviour
{
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    public void InitFurniture(Mesh input_mesh)
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = input_mesh as Mesh;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
