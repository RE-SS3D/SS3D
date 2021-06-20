using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Engine.TilesRework
{
    public class TileManager : MonoBehaviour
    {
        public static TileManager Instance { get; private set; }
        private static TileObjectSO[] tileObjectSOs;

        private TileMap map;

        private void Awake()
        {
            Instance = this;
            tileObjectSOs = Resources.FindObjectsOfTypeAll<TileObjectSO>();

            int mapWidth = 10;
            int mapHeight = 10;

            map = new TileMap(mapWidth, mapHeight, 1f, new Vector3(0, 0, 0));
        }

        public void SetTileObject(TileLayerType layer, TileObjectSO tileObjectSO, Vector3 position, TileObjectSO.Dir dir)
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

        public void SetTileObject(TileLayerType layer, string tileObjectSOName, Vector3 position, TileObjectSO.Dir dir)
        {
            TileObjectSO tileObjectSO = tileObjectSOs.FirstOrDefault(tileObject => tileObject.nameString == tileObjectSOName);
            SetTileObject(layer, tileObjectSO, position, dir);
        }

            public void Save()
        {
            map.Save();
            Debug.Log("Map saved");
        }

        public void Load()
        {
            map.Clear();
            map.Load();
            Debug.Log("Map loaded");
        }
    }
}