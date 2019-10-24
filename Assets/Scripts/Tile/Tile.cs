using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum TileTypes {
        station_tile,
        station_wall
    };
    public TileTypes TileDescriptor;

    public Component turf = null;


    public void initTile()
    {
        if (turf == null){
            turf = (Turf) gameObject.AddComponent(typeof(Turf));
            //Debug.Log("Adding Component: turf"); 
        }
        gameObject.GetComponent<Turf>().turfDescriptor = TileDescriptor;
        gameObject.GetComponent<Turf>().InitTurf();
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
