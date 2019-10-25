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
    public Turf turf = null;
    public TileContentManager contentManager = null;

    //Initialize the tile, add the turf component and tell the turf the tiletype
    public void initTile()
    {
        if (turf == null){
            turf = (Turf) gameObject.AddComponent(typeof(Turf));
            //Debug.Log("Adding Component: turf"); 
        }
        gameObject.GetComponent<Turf>().turfDescriptor = TileDescriptor;
        gameObject.GetComponent<Turf>().InitTurf();

        if (contentManager == null){
            contentManager = (TileContentManager) gameObject.AddComponent(typeof(TileContentManager));
            //Debug.Log("Adding Component: turf"); 
        }
        gameObject.GetComponent<TileContentManager>().InitTileContentManager();
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
