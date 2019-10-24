using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turf : MonoBehaviour
{
    public Tile.TileTypes turfDescriptor;

    public enum completeTypes{
        structural,
        plated,
        complete
    };
    public completeTypes completeness = completeTypes.complete;
    
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
        switch(completeness)
        {
            case completeTypes.structural:
            {
                upperTurf = Instantiate(Resources.Load("Walls/Wall I"), transform.position, Quaternion.Euler(new Vector3(-90, 0, 0)), transform) as GameObject;
                upperTurf.name = "upperTurf";
                break;
            }
            case completeTypes.plated:
            {
                upperTurf = Instantiate(Resources.Load("Walls/Wall I"), transform.position, Quaternion.Euler(new Vector3(-90, 0, 0)), transform) as GameObject;
                upperTurf.name = "upperTurf";
                break;
            }
            case completeTypes.complete:
            {
                upperTurf = Instantiate(Resources.Load("Walls/Wall I"), transform.position, Quaternion.Euler(new Vector3(-90, 0, 0)), transform) as GameObject;
                upperTurf.name = "upperTurf";
                break;
            }
        }
    }

    void SpawnTile(){
        switch(completeness)
        {
            case completeTypes.structural:
            {
                upperTurf = Instantiate(Resources.Load("Floors/Plating"), transform.position, Quaternion.identity, transform) as GameObject;
                upperTurf.name = "upperTurf";
                break;
            }
            case completeTypes.plated:
            {
                upperTurf = Instantiate(Resources.Load("Floors/Plating"), transform.position, Quaternion.identity, transform) as GameObject;
                upperTurf.name = "upperTurf";
                break;
            }
            case completeTypes.complete:
            {
                upperTurf = Instantiate(Resources.Load("Floors/BasicTile"), transform.position, Quaternion.identity, transform) as GameObject;
                upperTurf.name = "upperTurf";
                break;
            }
        }
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
