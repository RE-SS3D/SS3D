using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileContentManager : MonoBehaviour
{
    public GameObject mainObject;
    public GameObject originalMultipart;
    public List<GameObject> accessories = new List<GameObject>();

    public GameObject TargetBuild;

    bool buildable = true;
    public bool hasConnectable = false;
    public byte hasConnectableNeighbours = 0;


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

                if (target.GetComponent<FurnitureDescriptor>().connectable){
                    hasConnectable = true;
                    originalMultipart = target;
                    UpdateMultipart();
                    UpdateNeighbours();
                }
                
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
            Debug.Log("DELETING Contents");
            #if UNITY_EDITOR
            DestroyImmediate(accessory);
            #else
            Destroy(accessory);
            #endif
        }
        accessories.RemoveAll((o)=>o == null);

        #if UNITY_EDITOR
        DestroyImmediate(mainObject);
        #else
        Destroy(mainObject);
        #endif

        buildable = true;
        hasConnectable = false;
        hasConnectableNeighbours = 0;
        UpdateNeighbours();
    }

    public void UpdateMultipart(){
        hasConnectableNeighbours = 0;
        if (hasConnectable){ // only update if you're also a connectable 
                // NE SE SW NW N  E  S  W
                // 0  0  0  0  0  0  0  0
                //128 64  32 16 8  4  2  1
            GameObject tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1)).gameObject;
            GameObject tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z)).gameObject;
            GameObject tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1)).gameObject;
            GameObject tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z)).gameObject;
            GameObject tileNE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z + 1)).gameObject;
            GameObject tileSE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z - 1)).gameObject;
            GameObject tileSW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z - 1)).gameObject;
            GameObject tileNW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z + 1)).gameObject;
            if(tileN != null){
                if (tileN.GetComponent<TileContentManager>().hasConnectable){
                    if (tileN.GetComponent<TileContentManager>().mainObject.name == mainObject.name)
                        hasConnectableNeighbours ^= 8;
                }
            }
            if(tileE != null){
                if (tileE.GetComponent<TileContentManager>().hasConnectable){
                    if (tileE.GetComponent<TileContentManager>().mainObject.name == mainObject.name)
                        hasConnectableNeighbours ^= 4;
                }
            }
            if(tileS != null){
                if (tileS.GetComponent<TileContentManager>().hasConnectable){
                    if (tileS.GetComponent<TileContentManager>().mainObject.name == mainObject.name)
                        hasConnectableNeighbours ^= 2;
                }
            }
            if(tileW != null){
                if (tileW.GetComponent<TileContentManager>().hasConnectable){
                    if (tileW.GetComponent<TileContentManager>().mainObject.name == mainObject.name)
                        hasConnectableNeighbours ^= 1;
                }
            }
            if(tileNE != null){
                if (tileNE.GetComponent<TileContentManager>().hasConnectable){
                    if (tileNE.GetComponent<TileContentManager>().mainObject.name == mainObject.name)
                        hasConnectableNeighbours ^= 128;
                }
            }
            if(tileSE != null){
                if (tileSE.GetComponent<TileContentManager>().hasConnectable){
                    if (tileSE.GetComponent<TileContentManager>().mainObject.name == mainObject.name)
                        hasConnectableNeighbours ^= 64;
                }
            }
            if(tileSW != null){
                if (tileSW.GetComponent<TileContentManager>().hasConnectable){
                    if (tileSW.GetComponent<TileContentManager>().mainObject.name == mainObject.name)
                        hasConnectableNeighbours ^= 32;
                }
            }
            if(tileNW != null){
                if (tileNW.GetComponent<TileContentManager>().hasConnectable){
                    if (tileNW.GetComponent<TileContentManager>().mainObject.name == mainObject.name)
                        hasConnectableNeighbours ^= 16;
                } 
            }

            UpdateMultipartModel();      
        }
    }

    public void UpdateNeighbours(){
        GameObject tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1)).gameObject;
        GameObject tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z)).gameObject;
        GameObject tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1)).gameObject;
        GameObject tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z)).gameObject;
        GameObject tileNE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z + 1)).gameObject;
        GameObject tileSE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z - 1)).gameObject;
        GameObject tileSW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z - 1)).gameObject;
        GameObject tileNW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z + 1)).gameObject;
        if(tileN != null)
            tileN.GetComponent<TileContentManager>().UpdateMultipart();
        if(tileE != null)
            tileE.GetComponent<TileContentManager>().UpdateMultipart();
        if(tileS != null)
            tileS.GetComponent<TileContentManager>().UpdateMultipart();
        if(tileW != null)
            tileW.GetComponent<TileContentManager>().UpdateMultipart();
        if(tileNE != null)
            tileNE.GetComponent<TileContentManager>().UpdateMultipart();
        if(tileSE != null)
            tileSE.GetComponent<TileContentManager>().UpdateMultipart();
        if(tileSW != null)
            tileSW.GetComponent<TileContentManager>().UpdateMultipart();
        if(tileNW != null)
            tileNW.GetComponent<TileContentManager>().UpdateMultipart();
        
    }
    

    public void UpdateMultipartModel(){
        // update model (with new prefab for now)

        //get name of model you want:
        string targetName = mainObject.name;
        // delete current model
        #if UNITY_EDITOR
        DestroyImmediate(mainObject);
        #else
        Destroy(mainObject);
        #endif
    
        // update model
        // 1 = W
        // 2 = S
        // 3 = SW
        // 4 = E
        // 5 = EW
        // 6 = ES
        // 7 = ESW
        // 8 = N
        // 9 = NW
        // 10 = NS
        // 11 = NSW
        // 12 = NE
        // 13 = NEW
        // 14 = NES
        // 15 = NESW

        // NE SE SW NW N  E  S  W
        // 0  0  0  0  0  0  0  0
        //128 64 32 16 8  4  2  1
        switch( hasConnectableNeighbours & 0x0F ){
            case 0:
                mainObject = Instantiate(originalMultipart, transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                mainObject.name = targetName;
                break;
            case 1:
                mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-N"), transform.position, Quaternion.Euler(0, -90, 0), transform) as GameObject;
                mainObject.name = targetName;
                break;
            case 2:
                mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-N"), transform.position, Quaternion.Euler(0, 180, 0), transform) as GameObject;
                mainObject.name = targetName;
                break;
            case 3:
                //Check ordinal SW
                if ((hasConnectableNeighbours & 32) == 32){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NE-ordinal"), transform.position, Quaternion.Euler(0, 180, 0), transform) as GameObject;
                }else{
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NE"), transform.position, Quaternion.Euler(0, 180, 0), transform) as GameObject;
                }
                mainObject.name = targetName;
                break;
            case 4:
                mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-N"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                mainObject.name = targetName;
                break;
            case 5:
                mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NS"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                mainObject.name = targetName;
                break;
            case 6:
                //check Ordinal SE
                if ((hasConnectableNeighbours & 64) == 64){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NE-ordinal"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                }else{
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NE"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                }
                mainObject.name = targetName;
                break;
            case 7:
                //Ordinal check ESW (SE / SW)  //clockwise direction
                if ((hasConnectableNeighbours & (64 ^ 32)) == (64 ^ 32)){ //2 ordinals
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal2"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (64)) == (64)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal1"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (32)) == (32)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal1_2"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                }else{
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                }
                mainObject.name = targetName;
                break;
            case 8:
                mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-N"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                mainObject.name = targetName;
                break;
            case 9:
                //check ordinal NW
                if ((hasConnectableNeighbours & 16) == 16){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NE-ordinal"), transform.position, Quaternion.Euler(0, -90, 0), transform) as GameObject;
                }else{
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NE"), transform.position, Quaternion.Euler(0, -90, 0), transform) as GameObject;
                }
                mainObject.name = targetName;
                break;
            case 10:
                mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NS"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                mainObject.name = targetName;
                break;
            case 11:
                //Ordinal check NSW (SW / NW)  //clockwise direction
                if ((hasConnectableNeighbours & (32 ^ 16)) == (32 ^ 16)){ //2 ordinals
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal2"), transform.position, Quaternion.Euler(0, 180, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (32)) == (32)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal1"), transform.position, Quaternion.Euler(0, 180, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (16)) == (16)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal1_2"), transform.position, Quaternion.Euler(0, 180, 0), transform) as GameObject;
                }else{
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES"), transform.position, Quaternion.Euler(0, 180, 0), transform) as GameObject;
                }
                mainObject.name = targetName;
                break;
            case 12:
                //check ordinal NE
                if ((hasConnectableNeighbours & 128) == 128){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NE-ordinal"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                }else{
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NE"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                }
                mainObject.name = targetName;
                break;
            case 13:
                //Ordinal check NEW (NW / NE)  //clockwise direction
                if ((hasConnectableNeighbours & (16 ^ 128)) == (16 ^ 128)){ //2 ordinals
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal2"), transform.position, Quaternion.Euler(0, -90, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (16)) == (16)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal1"), transform.position, Quaternion.Euler(0, -90, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (128)) == (128)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal1_2"), transform.position, Quaternion.Euler(0, -90, 0), transform) as GameObject;
                }else{
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES"), transform.position, Quaternion.Euler(0, -90, 0), transform) as GameObject;
                }
                mainObject.name = targetName;
                break;
            case 14:
                //Ordinal check NES (NE / SE)  //clockwise direction
                if ((hasConnectableNeighbours & (128 ^ 64)) == (128 ^ 64)){ //2 ordinals
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal2"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (128)) == (128)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal1"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (64)) == (64)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES-ordinal1_2"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                }else{
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NES"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                }
                mainObject.name = targetName;
                break;
            case 15:
            //Debug.Log((hasConnectableNeighbours & (64 ^ 16)));
            //Debug.Log((64^16));
                if ((hasConnectableNeighbours & (128 ^ 64 ^ 32 ^ 16)) == (128 ^ 64 ^ 32 ^ 16)){ //4 ordinals
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal4"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                }
                // 3 ordinals
                else if((hasConnectableNeighbours & (128 ^ 64 ^ 32)) == (128 ^ 64 ^ 32)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal3"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (128 ^ 64 ^ 16)) == (128 ^ 64 ^ 16)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal3"), transform.position, Quaternion.Euler(0, 270, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (128 ^ 32 ^ 16)) == (128 ^ 32 ^ 16)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal3"), transform.position, Quaternion.Euler(0, 180, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (64 ^ 32 ^ 16)) == (64 ^ 32 ^ 16)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal3"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                }
                // 2 ordinals
                else if((hasConnectableNeighbours & (128 ^ 32)) == (128 ^ 32)){
                    //Debug.Log("1-3");
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal2-opposites"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (64 ^ 16)) == (64 ^ 16)){
                    //Debug.Log("2-4");
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal2-opposites"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (128 ^ 64)) == (128 ^ 64)){
                    //Debug.Log("1-2");
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal2"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (64 ^ 32)) == (64 ^ 32)){
                    //Debug.Log("2-3");
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal2"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (32 ^ 16)) == (32 ^ 16)){
                    //Debug.Log("3-4");
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal2"), transform.position, Quaternion.Euler(0, 180, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (16 ^ 8)) == (16 ^ 8)){
                    //Debug.Log("4-1");
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal2"), transform.position, Quaternion.Euler(0, 270, 0), transform) as GameObject;
                }
                // 1 ordinal
                else if((hasConnectableNeighbours & (64)) == (64)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal"), transform.position, Quaternion.Euler(0, 90, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (32)) == (32)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal"), transform.position, Quaternion.Euler(0, 180, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (16)) == (16)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal"), transform.position, Quaternion.Euler(0, 270, 0), transform) as GameObject;
                }else if((hasConnectableNeighbours & (128)) == (128)){
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW-ordinal"), transform.position, Quaternion.Euler(0, -90, 0), transform) as GameObject;
                }
                // no ordinals
                else{
                    mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW"), transform.position, Quaternion.Euler(0, -90, 0), transform) as GameObject;
                }
                //mainObject = Instantiate(Resources.Load("Multipart/"+targetName+"/"+targetName+"-NESW"), transform.position, Quaternion.Euler(0, 0, 0), transform) as GameObject;
                mainObject.name = targetName;
                break;
            
        }
    }
}
