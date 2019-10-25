using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile_loader : MonoBehaviour
{
    // Start is called before the first frame update
    public Texture2D map;

    public List<GameObject> tile_list;

    public void DeleteLevel (){
        foreach(GameObject tileobj in tile_list)
        {
            Debug.Log("DELETING TILE");
            #if UNITY_EDITOR
            DestroyImmediate(tileobj);
            #else
            Destroy(tileobj);
            #endif
        }
        CleanList();
    }
    public void GenerateLevel (){
        DeleteLevel();

        for (int x = 0; x < map.width; x++){
            for (int y = 0; y < map.height; y++){
                GenerateTile(x,y);
            }
        }
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
        if (pixelColor == Color.black)
        {
            GameObject new_obj = Instantiate(Resources.Load("empty_tile"), pos, Quaternion.identity, transform) as GameObject;
            new_obj.GetComponent<Tile>().TileDescriptor = Tile.TileTypes.station_tile;
            new_obj.GetComponent<Tile>().initTile();
            new_obj.name = string.Format("tile_{0}_{1}", pos.x, pos.z);
            tile_list.Add(new_obj);
        }else if (pixelColor == Color.blue)
        {
            GameObject new_obj = Instantiate(Resources.Load("empty_tile"), pos, Quaternion.identity, transform) as GameObject;
            new_obj.GetComponent<Tile>().TileDescriptor = Tile.TileTypes.station_wall;
            new_obj.GetComponent<Tile>().initTile();
            new_obj.name = string.Format("tile_{0}_{1}", pos.x, pos.z);
            tile_list.Add(new_obj);
        }
        else{
            Debug.Log(pixelColor);
        }
    }

    public void CleanList(){
        tile_list.RemoveAll((o)=>o == null);
    }
}
