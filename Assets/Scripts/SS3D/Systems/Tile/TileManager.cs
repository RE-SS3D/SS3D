using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static SS3D.Systems.Tile.TileMap;
using static SS3D.Systems.Tile.TileRestrictions;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Manager class that is used for managing all tiles. Scripts that want to interact with the TileMap should do it via this class.
    /// 
    /// Contrary to the previous tilemap implementation you will notice little networking here.
    /// The deliberate choice was made to keep the manager fully server-only for easier object synchronization and preventing
    /// cheating as clients do not have full knowledge of the tilemap.
    /// 
    /// Only PlacedTileObject.Create uses Mirror's spawn function. Everything else is handled by objects and Mirror itself.
    /// 
    /// See MultiAdjacencyConnector.cs as an example.
    /// </summary>
    [ExecuteAlways]
    public class TileManager : MonoBehaviour
    {
        /// <summary>
        /// SaveObject used for saving all maps.
        /// </summary>
        [Serializable]
        public class ManagerSaveObject
        {
            public MapSaveObject[] saveObjectList;
        }

        /// <summary>
        /// Singleton instance
        /// </summary>
        private static TileManager _instance;
        public static TileManager Instance => _instance;

        public bool IsInitialized { get; private set; }
        public static event Action OnTileManagerLoaded;

        private static TileObjectSo[] _tileObjectSOs;
        private string saveFileName = "tilemaps";

        private List<TileMap> _mapList;

        /// <summary>
        /// Initializes the TileManager.
        /// </summary>
        private void Init()
        {
            if (!IsInitialized)
            {
                _mapList = new List<TileMap>();

                // Scene has to be the same when playing in the editor or in the lobby
                //TODO: figure out how to replace SceneLoader or introduce minimal version of it?
                //Scene scene;
                //if (SceneLoaderManager.singleton)
                //    scene = SceneLoaderManager.singleton.GetSelectedScene();
                //else
                //    scene = SceneManager.GetActiveScene();
                //saveFileName = scene.name;

                // Finding all TileObjectSOs differs whether we are in the editor or playing
#if UNITY_EDITOR
                // We have to ensure that all objects used are loaded beforehand
                string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(TileObjectSo)));

                List<TileObjectSo> listTileObjectSO = new List<TileObjectSo>();
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    listTileObjectSO.Add(AssetDatabase.LoadAssetAtPath<TileObjectSo>(assetPath));
                }
                _tileObjectSOs = listTileObjectSO.ToArray();
#else
                Resources.LoadAll<TileObjectSo>("");
                tileObjectSOs = Resources.FindObjectsOfTypeAll<TileObjectSo>();
#endif

                LoadAll(true);
                UpdateAllAdjacencies();
                IsInitialized = true;

                OnTileManagerLoaded?.Invoke();
            }
        }

        private void OnEnable()
        {
            IsInitialized = false;
            Init();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Duplicate TileManager found. Deleting the last instance");
                EditorAndRuntime.Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Duplicate TileManager found on OnValidate().");
            }
            else
            {
                _instance = this;
                EditorApplication.delayCall += () => {
                    if (this)
                        Reinitialize();
                };
                
            }
        }
#endif

        /// <summary>
        /// Adds a new TileMap. Should only be called from the Editor.
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        public TileMap AddTileMap(string mapName)
        {
            TileMap map = Create(mapName);
            map.transform.SetParent(transform);

            return map;
        }

        /// <summary>
        /// Creates an empty map to be used by the editor.
        /// </summary>
        public void CreateEmptyMap()
        {
            int emptyMapNumber = 1;
            foreach (TileMap map in _mapList)
            {
                if (map.GetName() == "Empty map (" + emptyMapNumber + ")")
                {
                    emptyMapNumber++;
                }
            }

            TileMap emptyMap = AddTileMap("Empty map (" + emptyMapNumber + ")");
            _mapList.Add(emptyMap);
        }

        public List<TileMap> GetTileMaps()
        {
            return _mapList;
        }

        public string[] GetTileMapNames()
        {
            string[] names = new string[_mapList.Count];

            for (int i = 0; i < _mapList.Count; i++)
            {
                names[i] = _mapList[i].GetName();
            }

            return names;
        }

        /// <summary>
        /// Returns the main map. There can only be one main map.
        /// </summary>
        /// <returns></returns>
        private TileMap GetMainMap()
        {
            foreach (TileMap existingMap in _mapList)
            {
                if (existingMap.IsMain)
                    return existingMap;
            }

            Debug.LogError("No tilemap was set as main");
            return null;
        }

        /// <summary>
        /// Sets a new main map.
        /// </summary>
        /// <param name="map"></param>
        public void SetMainMap(TileMap map)
        {
            foreach (TileMap existingMap in _mapList)
            {
                existingMap.IsMain = false;
            }
            map.IsMain = true;
        }

        /// <summary>
        /// Sets a new TileObjectSO at a map for a given position and direction. Wrapper function for TileMap.SetTileObject().
        /// </summary>
        /// <param name="map"></param>
        /// <param name="subLayerIndex"></param>
        /// <param name="tileObjectSo"></param>
        /// <param name="position"></param>
        /// <param name="dir"></param>
        public void SetTileObject(TileMap map, int subLayerIndex, TileObjectSo tileObjectSo, Vector3 position, Direction dir)
        {
            if (CanBuild(map, subLayerIndex, tileObjectSo, position, dir, false))
                map.SetTileObject(subLayerIndex, tileObjectSo, position, dir);
        }

        /// <summary>
        /// Simplified version of SetTileObject. Will set a TileObjectSO on the main map without a sub layer.
        /// </summary>
        /// <param name="tileObjectSo"></param>
        /// <param name="position"></param>
        /// <param name="dir"></param>
        private void SetTileObject(TileObjectSo tileObjectSo, Vector3 position, Direction dir)
        {
            if (tileObjectSo.layer is TileLayer.HighWallMount or TileLayer.LowWallMount)
                Debug.LogError("Simplified function SetTileObject() is used. Do not use this function with layers where a sub index is required!");

            GetMainMap().SetTileObject(0, tileObjectSo, position, dir);
        }

        /// <summary>
        /// Simplified version of SetTileObject. Will set a TileObjectSO from a name, on the main map and without a sub layer.
        /// </summary>
        /// <param name="tileObjectSoName"></param>
        /// <param name="position"></param>
        /// <param name="dir"></param>
        public void SetTileObject(string tileObjectSoName, Vector3 position, Direction dir)
        {

            SetTileObject(GetTileObjectSO(tileObjectSoName), position, dir);
        }

        /// <summary>
        /// Sets a new TileObjectSO via name at a map for a given position and direction. Wrapper function for TileMap.SetTileObject().
        /// </summary>
        /// <param name="map"></param>
        /// <param name="subLayerIndex"></param>
        /// <param name="tileObjectSoName"></param>
        /// <param name="position"></param>
        /// <param name="dir"></param>
        public void SetTileObject(TileMap map, int subLayerIndex, string tileObjectSoName, Vector3 position, Direction dir)
        {
            SetTileObject(map, subLayerIndex, GetTileObjectSO(tileObjectSoName), position, dir);
        }

        /// <summary>
        /// Determines if a TileObjectSO can be build at the given location.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="subLayerIndex"></param>
        /// <param name="tileObjectSo"></param>
        /// <param name="position"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public bool CanBuild(TileMap selectedMap, int subLayerIndex, TileObjectSo tileObjectSo, Vector3 position, Direction dir, bool overrideAllowed)
        {
            bool canBuild = true;
            foreach (TileMap map in _mapList)
            {
                if (map == selectedMap)
                {
                    if (overrideAllowed)
                    {
                        // Do not check if the tile is occupied. Only apply tile restrictions.
                        canBuild &= map.CanBuild(subLayerIndex, tileObjectSo, position, dir, CheckRestrictions.OnlyRestrictions);
                    }
                    else
                    {
                        // Check for tile restrictions as well.
                        canBuild &= map.CanBuild(subLayerIndex, tileObjectSo, position, dir, CheckRestrictions.Everything);
                    }
                }
                else
                {
                    // Only check if the tile is occupied. Otherwise we cannot build furniture for example.
                    canBuild &= map.CanBuild(subLayerIndex, tileObjectSo, position, dir, CheckRestrictions.None);
                }
            }
            return canBuild;
        }

        /// <summary>
        /// Simplified version of CanBuild(). Assumes the main map is used and no sub layers are needed.
        /// </summary>
        /// <param name="tileObjectSo"></param>
        /// <param name="position"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public bool CanBuild(TileObjectSo tileObjectSo, Vector3 position, Direction dir)
        {
            if (tileObjectSo.layer is TileLayer.HighWallMount or TileLayer.LowWallMount)
                Debug.LogError("Simplified function CanBuild() is used. Do not use this function with layers where a sub index is required!");

            return CanBuild(GetMainMap(), 0, tileObjectSo, position, dir, false);
        }

        /// <summary>
        /// Clears a PlacedTileObject at a given layer and position.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="layer"></param>
        /// <param name="subLayerIndex"></param>
        /// <param name="position"></param>
        public void ClearTileObject(TileMap map, TileLayer layer, int subLayerIndex, Vector3 position)
        {
            map.ClearTileObject(layer, subLayerIndex, position);
        }

        /// <summary>
        /// Simplified version of ClearTileObject(). Assumes the main map is used and no sub layers are needed.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="position"></param>
        public void ClearTileObject(TileLayer layer, Vector3 position)
        {
            if (layer == TileLayer.HighWallMount || layer == TileLayer.LowWallMount)
                Debug.LogError("Simplified function CanBuild() is used. Do not use this function with layers where a sub index is required!");

            ClearTileObject(GetMainMap(), layer, 0, position);
        }

        /// <summary>
        /// Returns a TileObjectSO for a given name. Used during loading to find a matching object.
        /// </summary>
        /// <param name="tileObjectSoName"></param>
        /// <returns></returns>
        public TileObjectSo GetTileObjectSO(string tileObjectSoName)
        {
            TileObjectSo tileObjectSo = _tileObjectSOs.FirstOrDefault(tileObject => tileObject.nameString == tileObjectSoName);
            if (tileObjectSo == null)
                Debug.LogError("TileObjectSO was not found: " + tileObjectSoName);
            return tileObjectSo;
        }

        /// <summary>
        /// Saves all tilemaps to disk. The filename used is the name of the scene.
        /// </summary>
        public void SaveAll()
        {
            List<MapSaveObject> saveObjectList = new List<MapSaveObject>();

            foreach (TileMap map in _mapList)
            {
                MapSaveObject saveObject = map.Save();
                saveObjectList.Add(saveObject);
            }

            ManagerSaveObject saveMapObject = new()
            {
                saveObjectList = saveObjectList.ToArray()
            };

            SaveSystem.SaveObject(saveFileName, saveMapObject, true);
            Debug.Log("Tilemaps saved");
        }

        /// <summary>
        /// Performs a load of the entire tilemap. Will destroy and recreate existing objects.
        /// </summary>
        [ContextMenu("Load")]
        public void Load() => LoadAll(false);

        /// <summary>
        /// Loads all TileMaps into the manager. The softload parameter determines if saved objects should be
        /// new created, or only reinitalized.
        /// </summary>
        /// <param name="softLoad"></param>
        public void LoadAll(bool softLoad)
        {
            _tileObjectSOs ??= Resources.FindObjectsOfTypeAll<TileObjectSo>();

            if (softLoad)
                _mapList.Clear();
            else
                DestroyMaps();

            ManagerSaveObject saveMapObject = SaveSystem.LoadObject<ManagerSaveObject>(saveFileName);

            if (saveMapObject == null)
            {
                Debug.Log("No saved maps found. Creating default one.");
                CreateEmptyMap();
                _mapList[^1].IsMain = true;
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
                _mapList.Add(map);
            }

            
        }

        /// <summary>
        /// Removes a map. Should only be called by the editor.
        /// </summary>
        /// <param name="map"></param>
        public void RemoveMap(TileMap map)
        {
            map.Clear();
            EditorAndRuntime.Destroy(map.gameObject);

            _mapList.Remove(map);
        }

        /// <summary>
        ///  Destroys all existing maps.
        /// </summary>
        private void DestroyMaps()
        {
            foreach (TileMap map in _mapList)
            {
                map.Clear();
            }

            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                EditorAndRuntime.Destroy(transform.GetChild(i).gameObject);
            }

            _mapList.Clear();
        }

        /// <summary>
        /// Fully resets the tilemanager and wipes the saved file. Use this if a faulty save is preventing loading.
        /// </summary>
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
                _mapList[^1].IsMain = true;
                SaveAll();
            }
#endif
        }

        /// <summary>
        /// Reinitialize the map without destroying/creating gameobjects
        /// </summary>
        [ContextMenu("Reinitialize")]
        public void Reinitialize()
        {
            IsInitialized = false;
            Init();
        }

        /// <summary>
        /// Forces a new adjacency update on all objects. Useful for debugging.
        /// </summary>
        [ContextMenu("Force adjacency update")]
        private void UpdateAllAdjacencies()
        {
            foreach (TileMap map in _mapList)
            {
                map.UpdateAllAdjacencies();
            }
        }
    }
}