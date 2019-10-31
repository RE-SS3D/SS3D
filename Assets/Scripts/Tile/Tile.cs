using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum TileTypes {
        station_tile,
        station_wall,
        station_wall_reinforced,
        station_wall_glass,
        station_wall_glass_reinforced
        };

    public TileTypes TileDescriptor;
    public Turf turf = null;
    public TileContentManager contentManager = null;
    public TilePipeManager pipeManager = null;
    public Mirror.TileNetworkManager tileNetworkManager = null;

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

        if (pipeManager == null){
            pipeManager = (TilePipeManager) gameObject.AddComponent(typeof(TilePipeManager));
            //Debug.Log("Adding Component: turf"); 
        }
        gameObject.GetComponent<TilePipeManager>().InitTilePipeManager();

        if (tileNetworkManager == null){
            tileNetworkManager = (Mirror.TileNetworkManager) gameObject.AddComponent(typeof(Mirror.TileNetworkManager));
        }
    }



    public void UpdateTile(){
        //call to update all models of tile
        this.turf.UpdateTurf();
        //this.contentManager.UpdateMultipart();
        //this.pipeManager.UpdatePipes();
    } 
}