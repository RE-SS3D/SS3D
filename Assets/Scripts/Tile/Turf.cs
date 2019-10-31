using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turf : MonoBehaviour
{
    public Tile.TileTypes turfDescriptor;

    Dictionary<string, byte> directions = new Dictionary<string, byte>(){
        { "N", 8 },
        { "E", 4 },
        { "S", 2 },
        { "W", 1 },
        { "NE", 128},
        { "SE", 64 },
        { "SW", 32 },
        { "NW", 16 }
    };

    public bool connectiveUpper = false; //adaptive models yes/no like walls
    public bool connectiveLower = false;
    public byte connectiveUpper_NESW = 0;
    public byte connectiveLower_NESW = 0;

    public int lowerState = 2; // 0, 1, 2 (phases of 'completeness')
    public int upperState = 2;
    
    
    public GameObject lowerTurf = null;
    public GameObject upperTurf = null;
    
    public void InitTurf()
    {
        UpdateTurf();
    }

    public void BuildTurf(){
        BuildUpper();
        BuildLower();
        UpdateNeighbourTurf();
    }
    public void BuildUpper(){
        UpdateUpper();
    }

    public void BuildLower(){
        UpdateLower();
    }

    public void UpdateNeighbourTurf(){
        Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
        Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
        Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
        Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
        // Transform tileNE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z + 1));
        // Transform tileSE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z - 1));
        // Transform tileSW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z - 1));
        // Transform tileNW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z + 1));
        if(tileN != null){
            //Debug.Log("updating: "+tileN.name);
            tileN.gameObject.GetComponent<Turf>().UpdateTurf();
        }
        if(tileE != null){
            //Debug.Log("updating: "+tileE.name);
            tileE.gameObject.GetComponent<Turf>().UpdateTurf();
        }
        if(tileS != null){
            //Debug.Log("updating: "+tileS.name);
            tileS.gameObject.GetComponent<Turf>().UpdateTurf();
        }
        if(tileW != null){
            //Debug.Log("updating: "+tileW.name);
            tileW.gameObject.GetComponent<Turf>().UpdateTurf();
        }
        // if(tileNE != null){
        //     tileNE.gameObject.GetComponent<Turf>().updateTurf();
        // }
        // if(tileSE != null){
        //     tileSE.gameObject.GetComponent<Turf>().updateTurf();
        // }
        // if(tileSW != null){
        //     tileSW.gameObject.GetComponent<Turf>().updateTurf();
        // }
        // if(tileNW != null){
        //     tileNW.gameObject.GetComponent<Turf>().updateTurf();
        // }
    }
    public void UpdateTurf(){
        Tile.TileTypes new_turfDescriptor = gameObject.GetComponent<Tile>().TileDescriptor;
        if (new_turfDescriptor != turfDescriptor){
            turfDescriptor = new_turfDescriptor;
            BuildTurf();
        }else{
            UpdateUpper();
            UpdateLower();
        }
        
    }

    void UpdateUpper(){

        string modelName = "";
        switch(turfDescriptor){
            case Tile.TileTypes.station_wall:
                modelName = "station_wall";
                if (upperState == 2){
                    connectiveUpper = true;
                }else{
                    connectiveUpper = false;
                }
                break;
            case Tile.TileTypes.station_wall_reinforced:
                modelName = "station_wall_reinforced";
                if (upperState == 2){
                    connectiveUpper = true;
                }else{
                    connectiveUpper = false;
                }
                break;
            case Tile.TileTypes.station_wall_glass:
                modelName = "station_wall_glass";
                if (upperState == 2){
                    connectiveUpper = true;
                }else{
                    connectiveUpper = false;
                }
                break;
            case Tile.TileTypes.station_wall_glass_reinforced:
                modelName = "station_wall_glass_reinforced";
                if (upperState == 2){
                    connectiveUpper = true;
                }else{
                    connectiveUpper = false;
                }
                break;
            case Tile.TileTypes.station_tile:
                modelName = "station_floor";
                connectiveLower = false;
                break;
        }

        if (connectiveUpper){
            connectiveUpper = true;
            connectiveUpper_NESW = 0;
            Transform tileN = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z + 1));
            Transform tileE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z));
            Transform tileS = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x, gameObject.transform.position.z - 1));
            Transform tileW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z));
            Transform tileNE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z + 1));
            Transform tileSE = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x + 1, gameObject.transform.position.z - 1));
            Transform tileSW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z - 1));
            Transform tileNW = transform.parent.Find(string.Format("tile_{0}_{1}",gameObject.transform.position.x - 1, gameObject.transform.position.z + 1));
            if(tileN != null){
                if (tileN.gameObject.GetComponent<Turf>().connectiveUpper){
                    connectiveUpper_NESW ^= directions["N"];
                }
            }
            if(tileE != null){
                if (tileE.gameObject.GetComponent<Turf>().connectiveUpper){
                    connectiveUpper_NESW ^= directions["E"];
                }
            }
            if(tileS != null){
                if (tileS.gameObject.GetComponent<Turf>().connectiveUpper){
                    connectiveUpper_NESW ^= directions["S"];
                }
            }
            if(tileW != null){
                if (tileW.gameObject.GetComponent<Turf>().connectiveUpper){
                    connectiveUpper_NESW ^= directions["W"];
                }
            }
            // if(tileNE != null){
            //     if (tileNE.gameObject.GetComponent<Turf>().connectiveUpper){
            //         connectiveUpper_NESW ^= directions["NE"];
            //     }
            // }
            // if(tileSE != null){
            //     if (tileSE.gameObject.GetComponent<Turf>().connectiveUpper){
            //         connectiveUpper_NESW ^= directions["SE"];
            //     }
            // }
            // if(tileSW != null){
            //     if (tileSW.gameObject.GetComponent<Turf>().connectiveUpper){
            //         connectiveUpper_NESW ^= directions["SW"];
            //     }
            // }
            // if(tileNW != null){
            //     if (tileNW.gameObject.GetComponent<Turf>().connectiveUpper){
            //         connectiveUpper_NESW ^= directions["NW"];
            //     }
            // }
            
        }else{
            connectiveUpper = false;
        }

        //Debug.Log(modelName);
        UpdateUpper_model(modelName);

    }
    
    void UpdateLower(){
        string modelName = "";
        switch(turfDescriptor){
            case Tile.TileTypes.station_wall:
                modelName = "station";
                connectiveLower = false;
                break;
            case Tile.TileTypes.station_wall_reinforced:
                modelName = "station";
                connectiveLower = false;
                break;
            case Tile.TileTypes.station_wall_glass:
                modelName = "station";
                connectiveLower = false;
                break;
            case Tile.TileTypes.station_wall_glass_reinforced:
                modelName = "station";
                connectiveLower = false;
                break;
            case Tile.TileTypes.station_tile:
                modelName = "station";
                connectiveLower = false;
                break;
        }
        UpdateLower_model(modelName);
    }
   
    void UpdateUpper_model(string modelName){
        //Debug.Log("uMODEL: " + modelName);
        if (upperTurf != null)
        {
            #if UNITY_EDITOR
            DestroyImmediate(upperTurf);
            #else
            Destroy(upperTurf);
            #endif
        }

        if(!connectiveUpper){
            upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}", upperState)), transform.position, Quaternion.identity, transform) as GameObject;
            upperTurf.name = "upperTurf";
        }else{
            switch(connectiveUpper_NESW & 0x0f){
                case 0: //None?
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}", upperState)), transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                    break;
                case 1: //W
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_N", upperState)), transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                    break;
                case 2: //S
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_N", upperState)), transform.position, Quaternion.Euler(0,180,0), transform) as GameObject;
                    break;
                case 3: //SW
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_NE", upperState)), transform.position, Quaternion.Euler(0,180,0), transform) as GameObject;
                    break;
                case 4: //E
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_N", upperState)), transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                    break;
                case 5: //EW
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_NS", upperState)), transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                    break;
                case 6: //ES
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_NE", upperState)), transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                    break;
                case 7: //ESW
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_NES", upperState)), transform.position, Quaternion.Euler(0,90,0), transform) as GameObject;
                    break;
                case 8: //N
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_N", upperState)), transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                    break;
                case 9: //NW
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_NE", upperState)), transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                    break;
                case 10: //NS
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_NS", upperState)), transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                    break;
                case 11: //NSW
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_NES", upperState)), transform.position, Quaternion.Euler(0,180,0), transform) as GameObject;
                    break;
                case 12: //NE
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_NE", upperState)), transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                    break;
                case 13: //NEW
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_NES", upperState)), transform.position, Quaternion.Euler(0,-90,0), transform) as GameObject;
                    break;
                case 14: //NES
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_NES", upperState)), transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                    break;
                case 15: //NESW
                    upperTurf = Instantiate(Resources.Load("Turf/Upper/"+modelName+string.Format("/upper_{0}_NESW", upperState)), transform.position, Quaternion.Euler(0,0,0), transform) as GameObject;
                    break;
                default:
                    Debug.Log(connectiveUpper_NESW);
                    //this should not happen
                    break;
            }
            upperTurf.name = "upperTurf";
        }
    }

    void UpdateLower_model(string modelName){
        //Debug.Log("lMODEL: " + modelName);
        if (lowerTurf != null)
        {
            #if UNITY_EDITOR
            DestroyImmediate(lowerTurf);
            #else
            Destroy(lowerTurf);
            #endif
        }

        if(connectiveLower){
            //todo
        }else{
            lowerTurf = Instantiate(Resources.Load("Turf/Lower/"+modelName+string.Format("/lower_{0}", lowerState)), transform.position, Quaternion.identity, transform) as GameObject;
            lowerTurf.name = "lowerTurf";
        }
    }
}