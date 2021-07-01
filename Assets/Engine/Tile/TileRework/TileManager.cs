using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        
        public static TileManager Instance { get; private set; }
        private static bool isInitialized;
        public static bool IsInitialized()
        {
            return isInitialized;
        }

        private static TileObjectSO[] tileObjectSOs;
        private string saveFileName = "tilemaps";

        private List<TileMap> mapList;

        [ContextMenu("Initialize tilemap")]
        private void Init()
        {
            Instance = this;
            mapList = new List<TileMap>();

            Scene scene = SceneManager.GetActiveScene();
            saveFileName = scene.name;

            // We have to ensure that all objects used are loaded beforehand
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(TileObjectSO)));

            List<TileObjectSO> listTileObjectSO = new List<TileObjectSO>();
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                listTileObjectSO.Add(AssetDatabase.LoadAssetAtPath<TileObjectSO>(assetPath));
            }
            // tileObjectSOs = Resources.FindObjectsOfTypeAll<TileObjectSO>();
            tileObjectSOs = listTileObjectSO.ToArray();

            LoadAll();
            isInitialized = true;
        }

        private void Awake()
        {
            Init();
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

        public TileMap AddTileMap(string name)
        {
            TileMap map = Create(name);
            map.transform.SetParent(transform);

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

            TileMap emptyMap = AddTileMap("Empty map (" + emptyMapNumber + ")");
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

        public void SetTileObject(TileMap map, TileLayerType layer, TileObjectSO tileObjectSO, Vector3 position, Direction dir)
        {
            map.SetTileObject(layer, tileObjectSO, position, dir);
        }

        public void SetTileObject(TileMap map, TileLayerType layer, string tileObjectSOName, Vector3 position, Direction dir)
        {
            TileObjectSO tileObjectSO = tileObjectSOs.FirstOrDefault(tileObject => tileObject.nameString == tileObjectSOName);
            SetTileObject(map, layer, tileObjectSO, position, dir);
        }

        public void ClearTileObject(TileMap map, TileLayerType layer, Vector3 position)
        {
            map.ClearTileObject(layer, position);
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

            SaveSystem.SaveObject(saveFileName, saveMapObject, true);
            Debug.Log("Tilemaps saved");
        }

        public void LoadAll()
        {
            if (tileObjectSOs.Length == 0)
            {
                tileObjectSOs = Resources.FindObjectsOfTypeAll<TileObjectSO>();
            }

            ClearMaps();
            SaveMapObject saveMapObject = SaveSystem.LoadObject<SaveMapObject>(saveFileName);

            if (saveMapObject == null)
            {
                Debug.Log("No saved maps found. Creating default one.");
                CreateEmptyMap();
                SaveAll();
                return;
            }

            foreach (SaveObject s in saveMapObject.saveObjectList)
            {
                TileMap newMap = AddTileMap(s.mapName);

                // TileMap newMap = AddTileMap(s.name, s.width, s.height, s.tileSize, s.originPosition);
                newMap.Load(s);
                mapList.Add(newMap);
            }

            Debug.Log("Tilemaps loaded");
        }

        /*
        public void ChangeGrid(TileChunk map, string name, int xSize, int ySize, Vector3 origin)
        {
            if (map.GetWidth() > xSize || map.GetHeight() > ySize)
                Debug.LogWarning("Resizing the tilemap smaller than the original. You may lose stored objects!");

            SaveObject saveObject = map.Save();

            TileChunk newMap = AddTileMap(name, xSize, ySize, map.GetTileSize(), origin);
            // TileMap newMap = new TileMap(name, xSize, ySize, map.GetTileSize(), origin);
            newMap.Load(saveObject);

            chunkList.Insert(chunkList.IndexOf(map), newMap);
            //mapList.Add(newMap);
            RemoveMap(map);
        }
        */

        public void RemoveMap(TileMap map)
        {
            map.Clear();
            EditorAndRuntime.Destroy(map.gameObject);

            mapList.Remove(map);
        }

        private void ClearMaps()
        {
            foreach (TileMap map in mapList)
            {
                map.Clear();
            }
        }
    }
}