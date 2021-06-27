using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static SS3D.Engine.TilesRework.TileMap;

namespace SS3D.Engine.TilesRework
{
    [ExecuteAlways]
    public class TileManager : MonoBehaviour
    {
        [Serializable]
        public class SaveMapObject
        {
            public SaveObject[] saveObjectList;
        }

        private const string SAVE_FILENAME = "tilemaps";
        public static TileManager Instance { get; private set; }
        private static bool isInitialized;
        public static bool IsInitialized()
        {
            return isInitialized;
        }

        private static TileObjectSO[] tileObjectSOs;
        

        private List<TileMap> mapList;

        private void Awake()
        {
            Debug.Log("Awake called in tilemanager");
            Instance = this;
            mapList = new List<TileMap>();
            tileObjectSOs = Resources.FindObjectsOfTypeAll<TileObjectSO>();

            LoadAll();
            isInitialized = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Can't do most things in OnValidate, so wait a sec.
            UnityEditor.EditorApplication.delayCall += () => {
                if (this)
                    Awake();
            };
        }
#endif

        public TileMap AddTileMap(string name, int width, int height, float tileSize, Vector3 origin)
        {
            TileMap map = new TileMap(name, width, height, tileSize, origin);

            GameObject mapObject = new GameObject(name);
            mapObject.transform.SetParent(transform);

            return map;
        }

        public void CreateEmptyMap()
        {
            int emptyMapNumber = 1;
            foreach (TileMap map in mapList)
            {
                if (map.GetName() == "Empty map (" + emptyMapNumber + ")")
                {
                    emptyMapNumber++;
                }
            }

            TileMap emptyMap = AddTileMap("Empty map (" + emptyMapNumber + ")", 5, 5, 1.0f, new Vector3{ x = 0, y = 0, z = 0 });
            mapList.Add(emptyMap);
        }

        public List<TileMap> GetTileMaps()
        {
            return mapList;
        }

        public string[] GetTileMapNames()
        {
            string[] names = new string[mapList.Count];

            for (int i = 0; i < mapList.Count; i++)
            {
                names[i] = mapList[i].GetName();
            }

            return names;
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
                placedObject.transform.SetParent(transform);

                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name == map.GetName())
                    {
                        placedObject.transform.SetParent(child);
                        break;
                    }
                }
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

        public void ClearTileObject(TileMap map, TileLayerType layer, Vector3 position)
        {
            Vector2Int vector = map.GetXY(position);
            vector = map.ValidateGridPosition(vector);
            PlacedTileObject placedObject = map.GetTileObject(layer, vector.x, vector.y).GetPlacedObject();

            List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();
            foreach (Vector2Int gridPosition in gridPositionList)
            {
                map.GetTileObject(layer, gridPosition.x, gridPosition.y).ClearPlacedObject();
            }
        }

        public void SaveAll()
        {
            List<SaveObject> saveObjectList = new List<SaveObject>();

            foreach (TileMap map in mapList)
            {
                SaveObject saveObject = map.Save();
                saveObjectList.Add(saveObject);
            }

            SaveMapObject saveMapObject = new SaveMapObject
            {
                saveObjectList = saveObjectList.ToArray()
            };

            SaveSystem.SaveObject(SAVE_FILENAME, saveMapObject, true);
            Debug.Log("Tilemaps saved");
        }

        public void LoadAll()
        {
            ClearMaps();
            SaveMapObject saveMapObject = SaveSystem.LoadObject<SaveMapObject>(SAVE_FILENAME);

            if (saveMapObject == null)
            {
                Debug.Log("No saved maps found. Creating default one.");
                CreateEmptyMap();
                SaveAll();
                return;
            }

            foreach (SaveObject s in saveMapObject.saveObjectList)
            {
                TileMap newMap = AddTileMap(s.name, s.width, s.height, s.tileSize, s.originPosition);
                newMap.Load(s);
                mapList.Add(newMap);
            }

            Debug.Log("Tilemaps loaded");
        }

        public void ChangeGrid(TileMap map, string name, int xSize, int ySize, Vector3 origin)
        {
            if (map.GetWidth() > xSize || map.GetHeight() > ySize)
                Debug.LogWarning("Resizing the tilemap smaller than the original. You may lose stored objects!");

            SaveObject saveObject = map.Save();

            TileMap newMap = AddTileMap(name, xSize, ySize, map.GetTileSize(), origin);
            // TileMap newMap = new TileMap(name, xSize, ySize, map.GetTileSize(), origin);
            newMap.Load(saveObject);

            mapList.Insert(mapList.IndexOf(map), newMap);
            //mapList.Add(newMap);
            RemoveMap(map);
        }

        public void RemoveMap(TileMap map)
        {
            map.Clear();

            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                Transform child = transform.GetChild(i);
                if (child.name == map.GetName())
                {
                    EditorAndRuntime.Destroy(child.gameObject);
                    break;
                }
            }
            mapList.Remove(map);
        }

        private void ClearMaps()
        {
            foreach (TileMap map in mapList)
            {
                map.Clear();
            }

            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                Transform child = transform.GetChild(i);
                EditorAndRuntime.Destroy(child.gameObject);
            }
            mapList.Clear();
        }
    }
}