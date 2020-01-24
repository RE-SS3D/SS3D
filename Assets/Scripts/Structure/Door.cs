using UnityEngine;
using System;
using System.Collections;
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
    private GameObject wallCapPrefab; 

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
        float rotation = TileState.orientation == Orientation.Vertical ? 90 : 0;
        transform.localRotation = Quaternion.Euler(0, rotation, 0);

        UpdateWallCaps();
    }

    private void OnValidate() => UnityEditor.EditorApplication.delayCall += () => OnStateUpdate();

    /**
     * Adjusts the connections value based on the given new tile.
     * Returns whether value changed.
     */
    private bool UpdateSingleConnection(Direction direction, TileDefinition tile)
    {
        bool isConnected = tile.turf && tile.turf.genericType == "wall";
        return adjacents.UpdateDirection(direction, isConnected);
    }

    private void UpdateWallCaps()
    {
        if(wallCapPrefab == null)
            return;

        // Go through each direction and ensure the wallcap is present.
        for(int i = 0; i < 4; i++) {
            Direction direction = (Direction)(i * 2);
            bool isPresent = adjacents.Adjacent(direction) == 1;
            if(isPresent && wallCaps[i] == null) {
                wallCaps[i] = EditorAndRuntime.InstantiatePrefab(wallCapPrefab, transform);
                var cardinal = DirectionHelper.ToCardinalVector(direction);
                wallCaps[i].transform.localRotation = Quaternion.Euler(0, (1 - i / 2) * 180, 0);
                wallCaps[i].transform.localPosition = new Vector3(cardinal.Item1 + cardinal.Item2, 0, 0);
            }
            else if(!isPresent && wallCaps[i] != null) {
                EditorAndRuntime.Destroy(wallCaps[i]);
            }
        }
    }

    // WallCap gameobjects, North, East, South, West. Null if not present.
    private GameObject[] wallCaps = new GameObject[4];
    private AdjacencyBitmap adjacents = new AdjacencyBitmap();
}
