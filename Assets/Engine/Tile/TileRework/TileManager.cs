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
        public class ManagerSaveObject
        {
            public MapSaveObject[] saveObjectList;
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

        [ContextMenu("Reinitialize")]
        private void Init()
        {
            if (!isInitialized)
            {
                Instance = this;
                mapList = new List<TileMap>();

                Scene scene = SceneLoaderManager.singleton.GetSelectedScene();
                saveFileName = scene.name;

#if UNITY_EDITOR
                // We have to ensure that all objects used are loaded beforehand
                string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(TileObjectSO)));

                List<TileObjectSO> listTileObjectSO = new List<TileObjectSO>();
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    listTileObjectSO.Add(AssetDatabase.LoadAssetAtPath<TileObjectSO>(assetPath));
                }
                tileObjectSOs = listTileObjectSO.ToArray();
#else
            tileObjectSOs = Resources.FindObjectsOfTypeAll<TileObjectSO>();
#endif

                LoadAll();
                UpdateAllAdjacencies();
                isInitialized = true;
            }
        }

        private void Awake()
        {
            Init();
            UpdateAllAdjacencies();
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

        public void SetTileObject(TileMap map, TileLayer layer, int subLayerIndex, TileObjectSO tileObjectSO, Vector3 position, Direction dir)
        {
            map.SetTileObject(layer, subLayerIndex, tileObjectSO, position, dir);
        }

        public void SetTileObject(TileMap map, TileLayer layer, int subLayerIndex, string tileObjectSOName, Vector3 position, Direction dir)
        {
            TileObjectSO tileObjectSO = tileObjectSOs.FirstOrDefault(tileObject => tileObject.nameString == tileObjectSOName);
            SetTileObject(map, layer, subLayerIndex, tileObjectSO, position, dir);
        }

        public void ClearTileObject(TileMap map, TileLayer layer, int subLayerIndex, Vector3 position)
        {
            map.ClearTileObject(layer, subLayerIndex, position);
        }

        public void SaveAll()
        {
            List<MapSaveObject> saveObjectList = new List<MapSaveObject>();

            foreach (TileMap map in mapList)
            {
                MapSaveObject saveObject = map.Save();
                saveObjectList.Add(saveObject);
            }

            ManagerSaveObject saveMapObject = new ManagerSaveObject
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

            DestroyMaps();
            ManagerSaveObject saveMapObject = SaveSystem.LoadObject<ManagerSaveObject>(saveFileName);

            if (saveMapObject == null)
            {
                Debug.Log("No saved maps found. Creating default one.");
                CreateEmptyMap();
                SaveAll();
                return;
            }

            foreach (MapSaveObject s in saveMapObject.saveObjectList)
            {
                TileMap newMap = AddTileMap(s.mapName);
                newMap.Load(s);
                mapList.Add(newMap);
            }

            Debug.Log("Tilemaps loaded");
        }

        public void RemoveMap(TileMap map)
        {
            map.Clear();
            EditorAndRuntime.Destroy(map.gameObject);

            mapList.Remove(map);
        }

        private void DestroyMaps()
        {
            foreach (TileMap map in mapList)
            {
                map.Clear();
            }

            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                EditorAndRuntime.Destroy(transform.GetChild(i).gameObject);
            }

            mapList.Clear();
        }

        [ContextMenu("Reset")]
        private void Reset()
        {
#if UNITY_EDITOR
            if (EditorUtility.DisplayDialog("Resetting TileMap",
                        "Are you sure that you want to reset? This will DESTROY the currently saved map"
                        , "Ok", "Cancel"))
            {
                return;
            }
#endif
            DestroyMaps();
            CreateEmptyMap();
            SaveAll();
        }

        [ContextMenu("Force adjacency update")]
        private void UpdateAllAdjacencies()
        {
            foreach (TileMap map in mapList)
            {
                map.UpdateAllAdjacencies();
            }
        }
    }
}