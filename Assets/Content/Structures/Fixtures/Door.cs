using UnityEngine;
using System;
using UnityEditor;
using SS3D.Engine.Tiles;
using SS3D.Engine.Tiles.Connections;
using SS3D.Engine.Tiles.State;

namespace SS3D.Content.Structures.Fixtures
{
    // I'd like this to be internal, but if i make it anything but public
    // then OnStateUpdate complains
    [Serializable]
    public struct DoorState
    {
        // TODO: Construction phase
        public Orientation orientation;
    }

    /**
     * Door script handles opening and closing of door, as well as door related parephenalia.
     * Also does door things.
     * I haven't slept.
     */
    [ExecuteAlways]
    public class Door : TileStateMaintainer<DoorState>, AdjacencyConnector
    {
        public enum DoorType
        {
            Single,
            Double
        };

        /** <summary>Based on peculiarities of the model, the appropriate position of the wall cap</summary> */
        private const float WALL_CAP_DISTANCE_FROM_CENTRE = 0.979f;

        // As is the standard in the rest of the code, wallCap should face east.
        [SerializeField]
        private GameObject wallCapPrefab = null;

        [SerializeField]
        private DoorType doorType;

        private void Start()
        {
            // Note: 'Should' already be validated by the point the game starts.
            // So the only purpose is when loading map from scene to correctly load children.
            ValidateChildren();
        }

        /**
         * When a single adjacent turf is updated
         */
        public void UpdateSingle(Direction direction, TileDefinition tile)
        {
            if (UpdateSingleConnection(direction, tile))
                UpdateWallCaps();
        }

        /**
         * When all (or a significant number) of adjacent turfs update.
         * Turfs are ordered by direction, i.e. North, NorthEast, East ... NorthWest
         */
        public void UpdateAll(TileDefinition[] tiles)
        {
            bool changed = false;
            for (int i = 0; i < tiles.Length; i++) {
                changed |= UpdateSingleConnection((Direction)i, tiles[i]);
            }

            if (changed)
                UpdateWallCaps();
        }

        protected override void OnStateUpdate(DoorState prevState = new DoorState())
        {
            float rotation = OrientationHelper.AngleBetween(Orientation.Horizontal, TileState.orientation);
            transform.localRotation = Quaternion.Euler(0, rotation, 0);

            UpdateWallCaps();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EditorApplication.delayCall += () => {
                if (this) {
                    OnStateUpdate();
                    ValidateChildren();
                }
            };
        }
#endif

        /**
         * Adjusts the connections value based on the given new tile.
         * Returns whether value changed.
         */
        private bool UpdateSingleConnection(Direction direction, TileDefinition tile)
        {
            bool isConnected = tile.turf && tile.turf.genericType == "wall";
            return adjacents.UpdateDirection(direction, isConnected, true);
        }

        private void UpdateWallCaps()
        {
            if (wallCapPrefab == null)
                return;

            // Go through each direction and ensure the wallcap is present.
            for (Direction direction = Direction.North; direction < Direction.NorthWest; direction += 2) {
                int i = (int)direction / 2;

                // Get the direction this applies to for the external world
                Direction outsideDirection = DirectionHelper.Apply(OrientationHelper.ToPrincipalDirection(TileState.orientation), direction);
                bool isPresent = adjacents.Adjacent(outsideDirection) == 1;

                if (isPresent && wallCaps[i] == null) {
                    wallCaps[i] = SpawnWallCap(direction);
                    wallCaps[i].name = $"WallCap{i}";
                }
                else if (!isPresent && wallCaps[i] != null) {
                    EditorAndRuntime.Destroy(wallCaps[i]);
                    wallCaps[i] = null;
                }
            }
        }

        private void ValidateChildren()
        {
            // Note: This only needs to run on the server, which loads from the scene.
            // Anywhere else (including clients, mostly), doesn't really need this.
            for (int i = transform.childCount - 1; i > 0; --i) {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("WallCap")) {
                    bool success = int.TryParse(child.name.Substring(7), out int num);

                    // Remove if no int, int out of bounds, or duplicate
                    if (!success || num > wallCaps.Length || num < 0 || (wallCaps[num] != null && wallCaps[num] != child.gameObject)) {
                        Debug.LogWarning($"Unusual child found whilst searching for wall caps: {child.name}, deleting");
                        EditorAndRuntime.Destroy(child.gameObject);
                        continue;
                    }

                    wallCaps[num] = child.gameObject;
                }
            }
        }

        /**
         * <summary>Spawns a wall cap facing a direction, with appropriate position & settings</summary>
         * <param name="direction">Direction from the centre of the door</param>
         */
        private GameObject SpawnWallCap(Direction direction)
        {
            var wallCap = EditorAndRuntime.InstantiatePrefab(wallCapPrefab, transform);

            var cardinal = DirectionHelper.ToCardinalVector(DirectionHelper.Apply(Direction.East, direction));
            var rotation = DirectionHelper.AngleBetween(Direction.South, direction);

            wallCap.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            wallCap.transform.localPosition = new Vector3(cardinal.Item1 * WALL_CAP_DISTANCE_FROM_CENTRE, 0, cardinal.Item2 * WALL_CAP_DISTANCE_FROM_CENTRE);

            return wallCap;
        }

        // WallCap gameobjects, North, East, South, West. Null if not present.
        private GameObject[] wallCaps = new GameObject[4];
        private AdjacencyBitmap adjacents = new AdjacencyBitmap();
    }
}
