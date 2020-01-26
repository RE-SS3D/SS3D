using UnityEngine;
using System;
using UnityEditor;
using TileMap;
using TileMap.Connections;
using TileMap.State;

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

    // As is the standard in the rest of the code, wallCap should face east.
    [SerializeField]
    private GameObject wallCapPrefab = null; 

    [SerializeField]
    private DoorType doorType;

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

    private void OnValidate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
            if (this) OnStateUpdate();
        };
#endif
    }

    private void Start()
    {
        for(int i = transform.childCount - 1; i > 0; --i) {
            var child = transform.GetChild(i);
            if(child.name.StartsWith("WallCap")) {
                int num = 0;
                bool success = int.TryParse(child.name.Substring(7), out num);
                if(!success || num > wallCaps.Length) {
                    Debug.LogWarning($"Unusual child found whilst searching for wall caps: {child.name}, deleting");
                    EditorAndRuntime.Destroy(child.gameObject);
                    continue;
                }

                wallCaps[num] = child.gameObject;
            }
        }
    }

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
        if(wallCapPrefab == null)
            return;

        // Go through each direction and ensure the wallcap is present.
        for(Direction direction = Direction.North; direction < Direction.NorthWest; direction += 2) {
            int i = (int)direction / 2;

            // Get the direction this applies to for the external world
            Direction outsideDirection = DirectionHelper.Apply(OrientationHelper.ToPrincipalDirection(TileState.orientation), direction);
            bool isPresent = adjacents.Adjacent(outsideDirection) == 1;

            if(isPresent && wallCaps[i] == null) {
                wallCaps[i] = EditorAndRuntime.InstantiatePrefab(wallCapPrefab, transform);
                wallCaps[i].name = $"WallCap{i}";

                var cardinal = DirectionHelper.ToCardinalVector(DirectionHelper.Apply(Direction.East, direction));
                var rotation = DirectionHelper.AngleBetween(Direction.South, direction);

                wallCaps[i].transform.localRotation = Quaternion.Euler(0, rotation, 0);
                wallCaps[i].transform.localPosition = new Vector3(cardinal.Item1, 0, cardinal.Item2);
            }
            else if(!isPresent && wallCaps[i] != null) {
                EditorAndRuntime.Destroy(wallCaps[i]);
                wallCaps[i] = null;
            }
        }
    }

    // WallCap gameobjects, North, East, South, West. Null if not present.
    private GameObject[] wallCaps = new GameObject[4];
    private AdjacencyBitmap adjacents = new AdjacencyBitmap();
}
