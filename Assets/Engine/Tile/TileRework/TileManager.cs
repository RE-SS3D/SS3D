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

        private void Init()
        {
            if (!isInitialized)
            {
                Instance = this;
                mapList = new List<TileMap>();

                // Scene scene = SceneLoaderManager.singleton.GetSelectedScene();
                Scene scene = SceneManager.GetActiveScene();
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
                Reinitialize();
                // LoadAll();
                UpdateAllAdjacencies();
                isInitialized = true;
            }
        }

        private void Awake()
        {
            tileObjectSOs = Resources.FindObjectsOfTypeAll<TileObjectSO>();
            Init();
            Reinitialize();
            UpdateAllAdjacencies();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Can't do most things in OnValidate, so wait a sec.
            UnityEditor.EditorApplication.delayCall += () => {
                if (this)
                {
                    isInitialized = false;
                    Init();
                    Reinitialize();
                    UpdateAllAdjacencies();
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

        public void SetTileObject(TileMap map, TileLayer layer, int subLayerIndex, TileObjectSO tileObjectSO, Vector3 position, Direction dir)
        {
            map.SetTileObject(layer, subLayerIndex, tileObjectSO, position, dir);
        }

        public void SetTileObject(TileMap map, TileLayer layer, int subLayerIndex, string tileObjectSOName, Vector3 position, Direction dir)
        {
            SetTileObject(map, layer, subLayerIndex, GetTileObjectSO(tileObjectSOName), position, dir);
        }

        public void ClearTileObject(TileMap map, TileLayer layer, int subLayerIndex, Vector3 position)
        {
            map.ClearTileObject(layer, subLayerIndex, position);
        }

        public TileObjectSO GetTileObjectSO(string tileObjectSOName)
        {
            return tileObjectSOs.FirstOrDefault(tileObject => tileObject.nameString == tileObjectSOName);
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
                }
                else
                {
                    map = AddTileMap(s.mapName);
                    map.Load(s, false);
                }
                mapList.Add(map);
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

        /// <summary>
        /// Reinitialize the map without destroying/creating gameobjects
        /// </summary>
        [ContextMenu("Reinitialize")]
        private void Reinitialize()
        {
            LoadAll(true);
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