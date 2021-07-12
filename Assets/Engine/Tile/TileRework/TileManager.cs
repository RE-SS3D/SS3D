using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SS3D.Engine.Tiles.TileMap;

namespace SS3D.Engine.Tiles
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
        public bool IsInitialized { get; private set; }
        public static event System.Action TileManagerLoaded;

        private static TileObjectSO[] tileObjectSOs;
        private string saveFileName = "tilemaps";

        private List<TileMap> mapList;

        private void Init()
        {
            if (!IsInitialized)
            {
                Instance = this;
                mapList = new List<TileMap>();

                Scene scene;
                if (SceneLoaderManager.singleton)
                    scene = SceneLoaderManager.singleton.GetSelectedScene();
                else
                    scene = SceneManager.GetActiveScene();
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
                Resources.LoadAll<TileObjectSO>("");
                tileObjectSOs = Resources.FindObjectsOfTypeAll<TileObjectSO>();
#endif

                LoadAll(true);
                // LoadAll();
                UpdateAllAdjacencies();
                IsInitialized = true;

                TileManagerLoaded?.Invoke();
            }
        }

        private void Awake()
        {
            IsInitialized = false;
            // tileObjectSOs = Resources.FindObjectsOfTypeAll<TileObjectSO>();
            Init();
            // Reinitialize();
            // UpdateAllAdjacencies();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Can't do most things in OnValidate, so wait a sec.
            UnityEditor.EditorApplication.delayCall += () => {
                if (this)
                {
                    IsInitialized = false;
                    Init();
                    // Reinitialize();
                    // UpdateAllAdjacencies();
                }
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

        private TileMap GetMainMap()
        {
            foreach (TileMap existingMap in mapList)
            {
                if (existingMap.IsMain)
                    return existingMap;
            }

            Debug.LogError("No tilemap was set as main");
            return null;
        }

        public void SetMainMap(TileMap map)
        {
            foreach (TileMap existingMap in mapList)
            {
                existingMap.IsMain = false;
            }
            map.IsMain = true;
        }

        public void SetTileObject(TileMap map, int subLayerIndex, TileObjectSO tileObjectSO, Vector3 position, Direction dir)
        {
            map.SetTileObject(subLayerIndex, tileObjectSO, position, dir);
        }

        public void SetTileObject(TileObjectSO tileObjectSO, Vector3 position, Direction dir)
        {
            GetMainMap().SetTileObject(0, tileObjectSO, position, dir);
        }

        public void SetTileObject(string tileObjectSOName, Vector3 position, Direction dir)
        {

            SetTileObject(GetTileObjectSO(tileObjectSOName), position, dir);
        }

        public void SetTileObject(TileMap map, int subLayerIndex, string tileObjectSOName, Vector3 position, Direction dir)
        {
            SetTileObject(map, subLayerIndex, GetTileObjectSO(tileObjectSOName), position, dir);
        }

        public bool CanBuild(TileMap map, int subLayerIndex, TileObjectSO tileObjectSO, Vector3 position, Direction dir)
        {
            return map.CanBuild(subLayerIndex, tileObjectSO, position, dir);
        }

        public bool CanBuild(TileObjectSO tileObjectSO, Vector3 position, Direction dir)
        {
            return CanBuild(GetMainMap(), 0, tileObjectSO, position, dir);
        }

        public void ClearTileObject(TileMap map, TileLayer layer, int subLayerIndex, Vector3 position)
        {
            map.ClearTileObject(layer, subLayerIndex, position);
        }

        public void ClearTileObject(TileLayer layer, Vector3 position)
        {
            ClearTileObject(GetMainMap(), layer, 0, position);
        }

        public TileObjectSO GetTileObjectSO(string tileObjectSOName)
        {
            TileObjectSO tileObjectSO = tileObjectSOs.FirstOrDefault(tileObject => tileObject.nameString == tileObjectSOName);
            if (tileObjectSO == null)
                Debug.LogError("TileObjectSO was not found: " + tileObjectSOName);
            return tileObjectSO;
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

        [ContextMenu("Load")]
        public void Load() => LoadAll(false);

        public void LoadAll(bool softLoad)
        {
            if (tileObjectSOs == null)
            {
                tileObjectSOs = Resources.FindObjectsOfTypeAll<TileObjectSO>();
            }

            if (softLoad)
                mapList.Clear();
            else
                DestroyMaps();

            ManagerSaveObject saveMapObject = SaveSystem.LoadObject<ManagerSaveObject>(saveFileName);

            if (saveMapObject == null)
            {
                Debug.Log("No saved maps found. Creating default one.");
                CreateEmptyMap();
                mapList[mapList.Count - 1].IsMain = true;
                SaveAll();
                return;
            }

            foreach (MapSaveObject s in saveMapObject.saveObjectList)
            {
                TileMap map = null;
                if (softLoad)
                {
                    bool found = false;
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        var child = transform.GetChild(i);
                        if (child.name == s.mapName)
                        {
                            map = child.GetComponent<TileMap>();
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        Debug.LogWarning("Map was not found when reinitializing: " + s.mapName);
                        continue;
                    }

                    map.Setup(s.mapName);
                    map.Load(s, true);
                    Debug.Log("Tilemaps soft loaded");
                }
                else
                {
                    map = AddTileMap(s.mapName);
                    map.Load(s, false);
                    Debug.Log("Tilemaps loaded from save");
                }
                mapList.Add(map);
            }

            
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
        private void ResetTileManager()
        {
#if UNITY_EDITOR
            if (EditorUtility.DisplayDialog("Resetting TileMap",
                        "Are you sure that you want to reset? This will DESTROY the currently saved map"
                        , "Ok", "Cancel"))
            {
                DestroyMaps();
                CreateEmptyMap();
                SaveAll();
            }
#endif
        }

        /// <summary>
        /// Reinitialize the map without destroying/creating gameobjects
        /// </summary>
        [ContextMenu("Reinitialize")]
        private void Reinitialize()
        {
            IsInitialized = false;
            Init();
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