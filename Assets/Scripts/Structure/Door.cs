using UnityEngine;
using System;
using System.Collections;
using TileMap;

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
        UpdateSingleConnection(direction, tile);
        SetMeshAndDirection();
    }

    /**
     * When all (or a significant number) of adjacent turfs update.
     * Turfs are ordered by direction, i.e. North, NorthEast, East ... NorthWest
     */
    public void UpdateAll(TileDefinition[] tiles)
    {
        for (int i = 0; i < tiles.Length; i++) {
            UpdateSingleConnection((Direction)i, tiles[i]);
        }
        SetMeshAndDirection();
    }

    protected override void OnStateUpdate(DoorState prevState)
    {
        float rotation = TileState.orientation == Orientation.Vertical ? 90 : 0;
        transform.localRotation = Quaternion.Euler(0, rotation, 0);
    }

    private void OnValidate()
    {
        OnStateUpdate(new DoorState());
        SetMeshAndDirection();
    }

    /**
     * Adjusts the connections value based on the given new tile
     */
    private void UpdateSingleConnection(Direction direction, TileDefinition tile)
    {
        bool isConnected = tile.turf && tile.turf.genericType == "wall";

        // Set the direction bit to isConnected (1 or 0)
        connections &= (byte)~(1 << (int)direction);
        connections |= (byte)((isConnected ? 1 : 0) << (int)direction);
    }

    private void SetMeshAndDirection()
    {
        if(wallCapPrefab == null)
            return;

        // Go through each direction and ensure the wallcap is present.
        for(int i = 0; i < 4; i++) {
            Direction direction = (Direction)(i * 2);
            bool isPresent = Adjacent(direction) == 1;
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

    /**
     * Returns 0 if no adjacency, or 1 if there is.
     */
    private int Adjacent(Direction direction)
    {
        return (connections >> (int)direction) & 0x1;
    }

    // WallCap gameobjects, North, East, South, West. Null if not present.
    private GameObject[] wallCaps = new GameObject[4];
    private byte connections;
}
