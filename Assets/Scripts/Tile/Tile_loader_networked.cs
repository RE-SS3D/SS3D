using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile_loader_networked : MonoBehaviour
{
    // Start is called before the first frame update
    public Texture2D map;
    public GameObject empty_tile;
    public List<GameObject> tile_list;

    private void Awake() {
        Mirror.ClientScene.RegisterPrefab(empty_tile);
    }

    public void DeleteLevel (){
        foreach(GameObject tileobj in tile_list)
        {
            //Debug.Log("DELETING TILE");
            #if UNITY_EDITOR
            DestroyImmediate(tileobj);
            #else
            Destroy(tileobj);
            #endif
        }
        CleanList();
    }

    public void updateTurfs(){
        foreach(GameObject tileobj in tile_list)
        {
            Turf tile_turf = tileobj.GetComponent<Turf>();
            if(tile_turf != null){
                tile_turf.UpdateTurf();
            }
        }
    }

    public void updateTiles(){
        foreach(GameObject tileobj in tile_list)
        {
            Tile tile_tile = tileobj.GetComponent<Tile>();
            if(tile_tile != null){
                tile_tile.UpdateTile();
            }
        }
    }

    public void GenerateLevel (){
        DeleteLevel();
        for (int x = 0; x < map.width; x++){
            for (int y = 0; y < map.height; y++){
                GenerateTile(x,y);
            }
        }

        updateTurfs();
    }

    void GenerateTile(int x, int y)
    {   
        Color pixelColor = map.GetPixel(x,y);
        Vector3 pos = new Vector3(x-map.width/2,0,y-map.height/2);
        if (pixelColor.a == 0)
        {
            return;
            //transparancy is nothing
        }
        GameObject new_obj = Instantiate(Resources.Load("empty_tile"), pos, Quaternion.identity, transform) as GameObject;
        Mirror.NetworkServer.Spawn(new_obj);

        if (pixelColor == Color.black){
            new_obj.GetComponent<Tile>().TileDescriptor = Tile.TileTypes.station_tile;
        }else if (pixelColor == Color.blue){
            new_obj.GetComponent<Tile>().TileDescriptor = Tile.TileTypes.station_wall;
        }else if (pixelColor == Color.red){
            new_obj.GetComponent<Tile>().TileDescriptor = Tile.TileTypes.station_wall_reinforced;
        }else if (pixelColor == Color.green){
            new_obj.GetComponent<Tile>().TileDescriptor = Tile.TileTypes.station_wall_glass;
        }else if (pixelColor == Color.magenta){
            new_obj.GetComponent<Tile>().TileDescriptor = Tile.TileTypes.station_wall_glass_reinforced;
        }else{
            Debug.Log(pixelColor);
            return;
        }
        new_obj.GetComponent<Tile>().initTile();
        new_obj.name = string.Format("tile_{0}_{1}", pos.x, pos.z);
        tile_list.Add(new_obj);
    }

    public void CleanList(){
        tile_list.RemoveAll((o)=>o == null);
    }
}