using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ss13_basic_tile : MonoBehaviour
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
            turf = (ss13_turf_tile) gameObject.AddComponent(typeof(ss13_turf_tile));
            //Debug.Log("Adding Component: turf"); 
        }
        gameObject.GetComponent<ss13_turf_tile>().turfDescriptor = TileDescriptor;
        gameObject.GetComponent<ss13_turf_tile>().InitTurf();
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
