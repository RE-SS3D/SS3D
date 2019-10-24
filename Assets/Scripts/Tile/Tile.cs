using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum TileTypes {
        station_tile,
        station_wall,
        station_door
    };
    public TileTypes TileDescriptor;
    public Component turf = null;
    
    public List<GameObject> contents;

    //Initialize the tile, add the turf component and tell the turf the tiletype
    public void initTile()
    {
        if (turf == null){
            turf = (Turf) gameObject.AddComponent(typeof(Turf));
            //Debug.Log("Adding Component: turf"); 
        }
        gameObject.GetComponent<Turf>().turfDescriptor = TileDescriptor;
        gameObject.GetComponent<Turf>().InitTurf();
    }


    //Build an object on the tile
    public void buildContent(int orientation){
        //Hardcoded to always produce table
        GameObject content = new GameObject("content");
        content.transform.parent = transform;
        content.AddComponent(typeof(Furniture));
        content.GetComponent<Furniture>().InitFurniture(Resources.Load("TableWood-00") as Mesh);
        content.AddComponent<UnityEngine.MeshRenderer>();
        content.GetComponent<UnityEngine.MeshRenderer>().material = Resources.Load("Palette01") as Material;
        content.AddComponent<BoxCollider>();
        content.transform.localScale = (new Vector3(100f, 100f, 100f));
        content.transform.rotation = Quaternion.Euler(-90,0,0);

        contents.Add(content);
        Debug.Log("Table?");
        //Check if the content building fits on the current tiletype wall/floor/etc.

        //Check if the content building is an update to existing model 

        //Start building

    }


    public void DeleteContents(){
        foreach(GameObject content in contents)
        {
            Debug.Log("DELETING TILE");
            #if UNITY_EDITOR
            DestroyImmediate(content);
            #else
            Destroy(content);
            #endif
        }
        contents.RemoveAll((o)=>o == null);
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Editor causes this Update");
    }
}
