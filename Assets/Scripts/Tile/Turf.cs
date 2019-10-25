using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turf : MonoBehaviour
{
    public Tile.TileTypes turfDescriptor;

    //deprecated?
    public enum completeTypes{
        structural,
        plated,
        complete
    };

    public int lowerState = 2;
    public int upperState = 0;
    
    public GameObject lowerTurf = null;
    public GameObject upperTurf = null;
    
    public void InitTurf()
    {
        if (upperTurf != null)
        {
            #if UNITY_EDITOR
            DestroyImmediate(upperTurf);
            #else
            Destroy(upperTurf);
            #endif
        }
        if (lowerTurf != null)
        {
            #if UNITY_EDITOR
            DestroyImmediate(lowerTurf);
            #else
            Destroy(lowerTurf);
            #endif
        }

        switch(turfDescriptor)
        {
            case Tile.TileTypes.station_tile:
            {
                SpawnTile();
                break;
            }
            case Tile.TileTypes.station_wall:
            {
                SpawnWall();
                break;
            }
        };
    }

    void SpawnWall(){
        upperTurf = Instantiate(Resources.Load(string.Format("Walls/station_wall/Wall_upper_c{0}", upperState)), transform.position, Quaternion.identity, transform) as GameObject;
        upperTurf.name = "upperTurf";

        lowerTurf = Instantiate(Resources.Load(string.Format("Floors/station_floor/Floor_lower_c{0}", lowerState)), transform.position, Quaternion.identity, transform) as GameObject;
        lowerTurf.name = "lowerTurf";
    }

    void SpawnTile(){
        upperTurf = Instantiate(Resources.Load(string.Format("Floors/station_floor/Floor_upper_c{0}", upperState)), transform.position, Quaternion.identity, transform) as GameObject;
        upperTurf.name = "upperTurf";

        lowerTurf = Instantiate(Resources.Load(string.Format("Floors/station_floor/Floor_lower_c{0}", lowerState)), transform.position, Quaternion.identity, transform) as GameObject;
        lowerTurf.name = "lowerTurf";
    }

    void updateTurf(){
        InitTurf();
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
