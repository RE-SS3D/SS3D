using Tile;
using UnityEngine;
using UnityEditor;

using Engine.Tiles.Connections;
using Engine.Tiles.State;

namespace Engine.Tiles
{
    /**
     * The tile object takes information about a tile and transforms it into the world gameobject.
     */
    [ExecuteAlways]
    [SelectionBase]
    public class TileObject : MonoBehaviour
    {
        public TileDefinition Tile {
            get => tile;
            set => SetContents(value);
        }

        /**
         * Passes through an adjacency update to all children
         */
        public void UpdateSingleAdjacency(Direction direction, TileDefinition tile)
        {
            turfConnector?.UpdateSingle(direction, tile);
            fixtureConnector?.UpdateSingle(direction, tile);
        }
        /**
         * Passes through an adjacency update to all children
         */
        public void UpdateAllAdjacencies(TileDefinition[] tiles)
        {
            turfConnector?.UpdateAll(tiles);
            fixtureConnector?.UpdateAll(tiles);
        }

        #if UNITY_EDITOR
        /**
         * Allows the editor to refresh the tile.subStates when it knows it has
         * modified the child of a tile.
         */
        public void RefreshSubData()
        {
            UpdateSubDataFromChildren();
        }
        #endif

        /**
         * Fill in our non-serialized variables
         */
        private void OnEnable()
        {
            UpdateContents(true);
        }

        #if UNITY_EDITOR
        /**
         * When the value is changed, refresh
         */
        private void OnValidate() {
            // If we haven't started yet, don't try to validate.
            if(!this || tile.IsEmpty())
                return;

            var tileManager = transform.root.GetComponent<TileManager>();

            // Can't do most things in OnValidate, so wait a sec.
            EditorApplication.delayCall += () => {
                if(!this)
                    return;

                // Update contents
                UpdateContents(false);
                // Inform the tilemanager that the tile has updated, so it can update surroundings
                if (tileManager != null && tileManager.Count > 0 && !TileMapEditorHelpers.IsGhostTile(this)) {
                    var pos = tileManager.GetIndexAt(transform.position);
                    tileManager.EditorUpdateTile(pos.x, pos.y, tile);
                }
            };
        }

        /**
         * If the user deletes a tile in the scene while in edit mode, notify the tile manager
         */
        private void OnDestroy()
        {
            // If we are playing a game, a tile removal will be initiated by the tilemanager. 
            if(EditorApplication.isPlaying)
                return;

            var tileManager = transform.root.GetComponent<TileManager>();
            if(tileManager != null && !TileMapEditorHelpers.IsGhostTile(this))
                tileManager.RemoveTile(this);
        }
        #endif


        /**
         * Modify the tile based on the given information
         */
        private void SetContents(TileDefinition newTile)
        {
            if (newTile.turf != tile.turf)
                CreateTurf(newTile.turf);
            if (newTile.fixture != tile.fixture)
                CreateFixture(newTile.fixture);
        
            UpdateChildrenFromSubData(newTile);

            tile = newTile;

#if UNITY_EDITOR
            // If we're in the editor we'll try to correct any errors with tilestate.
            UpdateSubDataFromChildren();
#endif
        }

        /**
         * Run a more comprehensive complete update of tile, used
         * when you don't know what the previous tile contents was.
         */
        private void UpdateContents(bool shouldWarn)
        {
            // Fill in our references to objects using the saved information from our tile variable.
            // Effectively, this code expects the tile's children to match up to the turf and fixture.
            // If it finds any inconsistencies, it rectifies them.

            if (tile.turf) {
                turf = transform.Find("turf_" + tile.turf.id)?.gameObject;

                if (turf != null) {
                    turfConnector = turf.GetComponent<AdjacencyConnector>();
                }
                else {
                    // Update our tile object to make up for the fact that the object doesn't exist in the world.
                    // A user would have to fuck around in the editor to get to this point.
                    if(shouldWarn)
                        Debug.LogWarning("Tile's turf was not created? Creating now.");

                    // Create the object
                    CreateTurf(tile.turf);
                }
            }
            else {
                turf = null;
                turfConnector = null;
            }

            if (tile.fixture) {
                fixture = transform.Find("fixture_" + tile.fixture.id)?.gameObject;

                if (fixture != null) {
                    fixtureConnector = fixture.GetComponent<AdjacencyConnector>();
                }
                else {
                    // Update our tile object to make up for the fact that the object doesn't exist in the world.
                    // A user would have to fuck around in the editor to get to this point.
                    if(shouldWarn)
                        Debug.LogWarning("Tile's turf was not created?");
                    CreateFixture(tile.fixture);
                }
            }
            else {
                fixture = null;
                fixtureConnector = null;
            }

            UpdateChildrenFromSubData(tile);
            UpdateSubDataFromChildren();

            // As extra fuckery ensure no NEW objects have been added either
            for (int i = transform.childCount - 1; i >= 0; i--) {
                var child = transform.GetChild(i).gameObject;

                if (child != turf && child != fixture) {
                    if(shouldWarn)
                        Debug.LogWarning("Unknown object found in tile " + name + ": " + child.name + ", deleting.");
                    EditorAndRuntime.Destroy(child);
                }
            }
        }

        private void CreateTurf(Turf turfDefinition)
        {
            if (turf != null)
                EditorAndRuntime.Destroy(turf);
            turf = EditorAndRuntime.InstantiatePrefab(turfDefinition.prefab, transform);

            turf.name = "turf_" + turfDefinition.id;
            turfConnector = turf.GetComponent<AdjacencyConnector>();
        }
        private void CreateFixture(Fixture fixtureDefinition)
        {
            if (fixture != null)
                EditorAndRuntime.Destroy(fixture);

            if (fixtureDefinition != null) {
                fixture = EditorAndRuntime.InstantiatePrefab(fixtureDefinition.prefab, transform);
                fixtureConnector = fixture.GetComponent<AdjacencyConnector>();
            }
            else {
                fixture = null;
                fixtureConnector = null;
                return;
            }

            fixture.name = "fixture_" + fixtureDefinition.id;
        }

        private void UpdateChildrenFromSubData(TileDefinition newTile)
        {
            if (newTile.subStates != null && newTile.subStates.Length >= 1 && newTile.subStates[0] != null)
                turf?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[0]);

            if (newTile.subStates != null && newTile.subStates.Length >= 2 && newTile.subStates[1] != null)
                fixture?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[1]);
        }
        private void UpdateSubDataFromChildren()
        {
            tile.subStates = new object[] {
                turf?.GetComponent<TileStateCommunicator>()?.GetTileState(),
                fixture?.GetComponent<TileStateCommunicator>()?.GetTileState()
            };
        }

        [SerializeField]
        private TileDefinition tile = new TileDefinition();

        private GameObject turf = null;
        private AdjacencyConnector turfConnector = null; // may be null

        private GameObject fixture = null;
        private AdjacencyConnector fixtureConnector = null; // may be null
    }
}
