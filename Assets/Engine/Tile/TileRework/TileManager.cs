using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static SS3D.Engine.TilesRework.TileMap;

namespace SS3D.Engine.TilesRework
{
    public class TileManager : MonoBehaviour
    {
        public static TileManager Instance { get; private set; }
        private static TileObjectSO[] tileObjectSOs;

        private List<TileMap> mapList;

        private void Awake()
        {
            Instance = this;
            mapList = new List<TileMap>();
            tileObjectSOs = Resources.FindObjectsOfTypeAll<TileObjectSO>();

            
            int mapWidth = 10;
            int mapHeight = 10;

            AddTileMap("MainTileMap", mapWidth, mapHeight, 1f, new Vector3(0, 0, 0));
        }

        public void AddTileMap(string name, int width, int height, float tileSize, Vector3 origin)
        {
            TileMap map = new TileMap(name, width, height, tileSize, origin);
            mapList.Add(map);
        }

        public void SetTileObject(TileMap map, TileLayerType layer, TileObjectSO tileObjectSO, Vector3 position, TileObjectSO.Dir dir)
        {
            Vector2Int vector = map.GetXY(position);

            Vector2Int placedObjectOrigin = new Vector2Int(vector.x, vector.y);
            placedObjectOrigin = map.ValidateGridPosition(placedObjectOrigin);

            // Test Can Build
            List<Vector2Int> gridPositionList = tileObjectSO.GetGridPositionList(placedObjectOrigin, dir);
            bool canBuild = true;
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                if (!map.GetTileObject(layer, gridPosition.x, gridPosition.y).IsEmpty())
                {
                    canBuild = false;
                    break;
                }
            }

            if (canBuild)
            {
                Vector2Int rotationOffset = tileObjectSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = map.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * map.GetTileSize();

                PlacedTileObject placedObject = PlacedTileObject.Create(placedObjectWorldPosition, placedObjectOrigin, dir, tileObjectSO);
                placedObject.transform.SetParent(this.transform);

                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    map.GetTileObject(layer, gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
                }
            }
            else
            {
                Debug.LogWarning("Cannot build here");
            }
        }

        public void SetTileObject(TileMap map, TileLayerType layer, string tileObjectSOName, Vector3 position, TileObjectSO.Dir dir)
        {
            TileObjectSO tileObjectSO = tileObjectSOs.FirstOrDefault(tileObject => tileObject.nameString == tileObjectSOName);
            SetTileObject(map, layer, tileObjectSO, position, dir);
        }

        public void SaveAll()
        {
            foreach (TileMap map in mapList)
            {
                SaveObject saveObject = map.Save();
                SaveSystem.SaveObject(saveObject.name, saveObject, true);
            }
            Debug.Log("Tilemaps saved");
        }

        public void LoadAll()
        {
            ClearMaps();

            string[] filePathNames = SaveSystem.GetSaveFiles();

            foreach (string filePathName in filePathNames)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePathName);
                SaveObject s = SaveSystem.LoadObject<SaveObject>(fileName);

                TileMap newMap = new TileMap(s.name, s.width, s.height, s.tileSize, s.originPosition);
                mapList.Add(newMap);
            }

            Debug.Log("Tilemaps loaded");
        }

        public void ResizeGrid(TileMap map, int xSize, int ySize)
        {
            if (map.GetWidth() > xSize || map.GetHeight() > ySize)
                Debug.LogWarning("Resizing the tilemap smaller than the original. You may lose stored objects!");

            SaveObject saveObject = map.Save();

            AddTileMap(map.GetName(), xSize, ySize, map.GetTileSize(), map.GetOrigin());
        }

        private void ClearMaps()
        {
            foreach (TileMap map in mapList)
            {
                map.Clear();
            }
            mapList.Clear();
        }
    }
}