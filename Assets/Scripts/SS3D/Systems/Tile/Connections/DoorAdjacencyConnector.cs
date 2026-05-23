
using System.Collections.Generic;
using UnityEngine;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using Coimbra;
using FishNet.Object;
using SS3D.Core;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for doors, handling adding wall caps, creating custom floor tile under the door.
    /// </summary>
    public class DoorAdjacencyConnector : AbstractHorizontalConnector, IAdjacencyConnector
    {
        private enum DoorType
        {
            Single,
            Double
        };

        public Direction DoorDirection => _placedObject.Direction;

        protected override IMeshAndDirectionResolver AdjacencyResolver => null;

        // Based on peculiarities of the model, the appropriate position of the wall cap
        private const float WALL_CAP_DISTANCE_FROM_CENTRE = 0f;

        // As is the standard in the rest of the code, wallCap should face east.
        [SerializeField]
        private GameObject wallCapPrefab = null;

        [SerializeField]
        private DoorType doorType;

        [SerializeField]
        private GameObject airlockFloorPrefab = null;

        [SerializeField]
        private Material transparentFloorMaterial = null;

        // WallCap gameobjects, North, East, South, West. Null if not present.
        private GameObject[] wallCaps = new GameObject[4];

        private GameObject _airlockFloor;

        public override void OnStartClient()
        {
            base.OnStartClient();
            RefreshAirlockFloorTiles();
        }

        private void Start()
        {
            RefreshAirlockFloorTiles();
        }

        public override bool UpdateSingleConnection(Direction dir, PlacedTileObject placedObject, bool updateNeighbours)
        {
            bool update = base.UpdateSingleConnection(dir, placedObject, updateNeighbours);
            if (update)
                UpdateWallCaps();

            RefreshAirlockFloorTiles();

            return update;
        }

        public override void UpdateAllConnections()
        {
            base.UpdateAllConnections();
            UpdateWallCaps();
            RefreshAirlockFloorTiles();
        }

        public void RefreshAirlockFloorTiles()
        {
            if (_placedObject == null)
            {
                return;
            }

            if (!TryGetAirlockFloorNeighbours(out List<PlacedTileObject> neighbours))
            {
                return;
            }

            GameObject floorTile = GetOrCreateAirlockFloor();
            if (floorTile == null)
            {
                return;
            }

            MeshRenderer activeFloorRenderer = GetActiveAirlockFloorRenderer(floorTile);
            if (activeFloorRenderer == null)
            {
                return;
            }

            _placedObject.HasNeighbourFrontBack(neighbours, out PlacedTileObject frontTile, true);
            _placedObject.HasNeighbourFrontBack(neighbours, out PlacedTileObject backTile, false);

            SetRendererMaterial(activeFloorRenderer, 0, GetFloorMaterialOrDefault(frontTile, transparentFloorMaterial));
            SetRendererMaterial(activeFloorRenderer, 1, GetFloorMaterialOrDefault(backTile, transparentFloorMaterial));
        }

        private bool TryGetAirlockFloorNeighbours(out List<PlacedTileObject> floorNeighbours)
        {
            floorNeighbours = new List<PlacedTileObject>();
            if (!SubSystems.TryGet(out TileSubSystem tileSystem) || tileSystem.CurrentMap == null)
            {
                return false;
            }

            PlacedTileObject[] neighbours = tileSystem.CurrentMap.GetNeighbourPlacedObjects(
                TileLayer.Turf,
                _placedObject.gameObject.transform.position);

            foreach (PlacedTileObject neighbour in neighbours)
            {
                if (neighbour != null)
                {
                    floorNeighbours.Add(neighbour);
                }
            }

            return true;
        }

        /// <summary>
        /// Destroy or add a wall cap.
        /// </summary>
        private void CreateWallCaps(bool isPresent, Direction direction)
        {
            int capIndex = GetWallCapIndex(direction);
            if (isPresent && wallCaps[capIndex] == null)
            {

                wallCaps[capIndex] = SpawnWallCap(direction);
                wallCaps[capIndex].name = $"WallCap{capIndex}";
            }
            else if (!isPresent && wallCaps[capIndex] != null)
            {
                wallCaps[capIndex].Dispose(true);
                wallCaps[capIndex] = null;
            }
        }

        private void UpdateWallCaps()
        {
            if (wallCapPrefab == null)
                return;

            Direction outFacing = TileHelper.GetNextCardinalDir(DoorDirection);

            bool isPresent = _adjacencyMap.HasConnection(outFacing);
            CreateWallCaps(isPresent, outFacing);

            isPresent = _adjacencyMap.HasConnection(TileHelper.GetOpposite(outFacing));
            CreateWallCaps(isPresent, TileHelper.GetOpposite(outFacing));
        }

        
        /// <summary> Spawns a wall cap facing a direction, with appropriate position & settings </summary>
        ///<param name="direction">Direction from the centre of the door</param>
        private GameObject SpawnWallCap(Direction direction)
        {
            var wallCap = Instantiate(wallCapPrefab, transform);

            Direction cardinalDirectionInput = TileHelper.GetRelativeDirection(direction, DoorDirection);
            var cardinal = TileHelper.ToCardinalVector(cardinalDirectionInput);
            float rotation = TileHelper.AngleBetween(direction, DoorDirection);


            wallCap.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            wallCap.transform.localPosition = new Vector3(cardinal.Item1 * WALL_CAP_DISTANCE_FROM_CENTRE, 0, cardinal.Item2 * WALL_CAP_DISTANCE_FROM_CENTRE);
            Spawn(wallCap);
            return wallCap;
        }

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            return (neighbourObject && neighbourObject.HasAdjacencyConnector &&
                neighbourObject.GenericType == TileObjectGenericType.Wall);
        }

        private GameObject GetOrCreateAirlockFloor()
        {
            if (_airlockFloor != null)
            {
                return _airlockFloor;
            }

            if (airlockFloorPrefab == null)
            {
                return null;
            }

            _airlockFloor = Instantiate(airlockFloorPrefab, transform);
            _airlockFloor.name = "AirlockFloorTile";
            _airlockFloor.transform.localPosition = Vector3.zero;
            _airlockFloor.transform.localRotation = Quaternion.identity;
            _airlockFloor.transform.localScale = Vector3.one;
            RemoveTileRuntimeComponents(_airlockFloor);

            return _airlockFloor;
        }

        private void RemoveTileRuntimeComponents(GameObject floorTile)
        {
            foreach (Collider collider in floorTile.GetComponents<Collider>())
            {
                Destroy(collider);
            }

            foreach (MonoBehaviour component in floorTile.GetComponents<MonoBehaviour>())
            {
                Destroy(component);
            }
        }

        private MeshRenderer GetActiveAirlockFloorRenderer(GameObject floorTile)
        {
            bool doorRunsNorthSouth = IsNorthSouth(_placedObject.Direction);
            Transform northSouthTile = floorTile.transform.Find("AirlockTileNS");
            Transform eastWestTile = floorTile.transform.Find("AirlockTileEW");

            if (northSouthTile != null)
            {
                northSouthTile.gameObject.SetActive(doorRunsNorthSouth);
            }

            if (eastWestTile != null)
            {
                eastWestTile.gameObject.SetActive(!doorRunsNorthSouth);
            }

            Transform activeTile = doorRunsNorthSouth ? northSouthTile : eastWestTile;
            return activeTile != null ? activeTile.GetComponent<MeshRenderer>() : null;
        }

        public static bool IsNorthSouth(Direction direction)
        {
            return direction == Direction.North || direction == Direction.South;
        }

        public static Material GetFloorMaterialOrDefault(PlacedTileObject tileObject, Material defaultMaterial)
        {
            if (tileObject == null || tileObject.GenericType != TileObjectGenericType.Floor)
            {
                return defaultMaterial;
            }

            MeshRenderer floorRenderer = tileObject.GetComponentInChildren<MeshRenderer>();
            if (floorRenderer == null || floorRenderer.sharedMaterials.Length == 0)
            {
                return defaultMaterial;
            }

            return floorRenderer.sharedMaterials[0] ?? defaultMaterial;
        }

        public static void SetRendererMaterial(MeshRenderer renderer, int materialIndex, Material material)
        {
            if (renderer == null || material == null)
            {
                return;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materialIndex < 0 || materialIndex >= materials.Length)
            {
                return;
            }

            materials[materialIndex] = material;
            renderer.sharedMaterials = materials;
        }

        /// <summary>
        /// Get the index of a wallcap in the wallcap Array.
        /// </summary>
        private int GetWallCapIndex(Direction dir)
        {
            return (int)dir / 2;
        }
    }

}
