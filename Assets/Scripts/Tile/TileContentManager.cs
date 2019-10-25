using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileContentManager : MonoBehaviour
{
    public GameObject mainObject;
    public List<GameObject> accessories;

    public GameObject TargetBuild;

    bool buildable = true;


    public void InitTileContentManager(){

    }

 
    public int BuildContent(GameObject target, int orientation){
        //Check if the content building fits on the current tiletype wall/floor/etc.
        switch(target.GetComponent<FurnitureDescriptor>().furnitureType){
            case FurnitureDescriptor.FurnitureType.floor_furniture:
                return BuildFloorContent(target, orientation);

            case FurnitureDescriptor.FurnitureType.door_furniture:
                return BuildDoorContent(target, orientation);


            default:
                break;
        }
        return 0;
    }

    public int BuildContent_target(int orientation){
        //Check if the content building fits on the current tiletype wall/floor/etc.
        switch(TargetBuild.GetComponent<FurnitureDescriptor>().furnitureType){
            case FurnitureDescriptor.FurnitureType.floor_furniture:
                return BuildFloorContent(TargetBuild, orientation);

            case FurnitureDescriptor.FurnitureType.door_furniture:
                return BuildDoorContent(TargetBuild, orientation);


            default:
                break;
        }
        return 0;
    }

    int BuildFloorContent(GameObject target, int orientation){
        //Check if the tile is already occupied:
        if (!buildable){
            //Update object if accessory
            //or prompt 'no'?
            return 0;
        }
        else{
            //check if building target is allowed on this tile type
            if(gameObject.GetComponent<Tile>().TileDescriptor == Tile.TileTypes.station_tile){
                //Start building
                mainObject = Instantiate(target, transform.position, Quaternion.Euler(0, orientation*90, 0), transform);
                mainObject.name = target.name;
                buildable = false;
                return 1;
            }else{
                return 0;
            }
            
        }
    }

    int BuildDoorContent(GameObject target, int orientation){
        //Check if the tile is already occupied:
        if (!buildable){
            //Update object if accessory
            //or prompt 'no'?
            return 0;
        }
        else{
            //check if building target is allowed on this tile type
            if(gameObject.GetComponent<Tile>().TileDescriptor == Tile.TileTypes.station_tile){
                //Start building
                mainObject = Instantiate(target, transform.position, Quaternion.Euler(0, orientation*90, 0), transform);
                mainObject.name = target.name;
                buildable = false;
                return 1;
            }else{
                return 0;
            }
        }
    }

    public void DeleteContents(){
        foreach(GameObject accessory in accessories)
        {
            Debug.Log("DELETING TILE");
            #if UNITY_EDITOR
            DestroyImmediate(accessory);
            #else
            Destroy(content);
            #endif
        }
        accessories.RemoveAll((o)=>o == null);

        #if UNITY_EDITOR
        DestroyImmediate(mainObject);
        #else
        Destroy(mainObject);
        #endif

        buildable = true;
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
