using Tile;
using UnityEngine;
using UnityEditor;

using SS3D.Engine.Tiles.Connections;
using SS3D.Engine.Tiles.State;
using System;

namespace SS3D.Engine.Tiles
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
        public void UpdateSingleAdjacency(Direction direction, TileDefinition tile, FixtureLayers layer)
        {
            // TODO create static method to get index
            int index = (int)layer;
            turfConnector?.UpdateSingle(direction, tile);
            fixtureConnectors[index]?.UpdateSingle(direction, tile);
        }

        public void UpdateSingleAdjacency(Direction direction, TileDefinition tile)
        {
            var layers = (FixtureLayers[])Enum.GetValues(typeof(FixtureLayers));
            foreach (FixtureLayers layer in layers)
            {
                UpdateSingleAdjacency(direction, tile, layer);
            }
        }

        /**
         * Passes through an adjacency update to all children
         */
        public void UpdateAllAdjacencies(TileDefinition[] tiles, FixtureLayers layer)
        {
            int index = (int)layer;
            turfConnector?.UpdateAll(tiles);
            fixtureConnectors[index]?.UpdateAll(tiles);
        }

        public void UpdateAllAdjacencies(TileDefinition[] tiles)
        {
            // Update every layer
            var layers = (FixtureLayers[])Enum.GetValues(typeof(FixtureLayers));
            foreach (FixtureLayers layer in layers)
            {
                UpdateAllAdjacencies(tiles, layer);
            }
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
            if (newTile.fixtures != tile.fixtures)
                CreateFixtures(newTile.fixtures);
        
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
            // Effectively, this code expects the tile's children to match up to the turf and fixtures.
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

            int i = 0;
            var layers = (FixtureLayers[])Enum.GetValues(typeof(FixtureLayers));


            if (tile.fixtures == null)
            {
                Debug.LogWarning("Tile.fixtures is found null... creating new one");
                tile.fixtures = new Fixture[layers.Length];
            }
            if (tile.fixtures.Length != 6)
            {
                Debug.LogWarning("Fixtures array is of wrong size, creating one");
                tile.fixtures = new Fixture[layers.Length];
            }
            foreach (var fixture in fixtures)
            {
                if (tile.fixtures[i] != null)
                {

                    string layerName = Enum.GetName(typeof(FixtureLayers), layers[i]);
                    fixtures[i] = transform.Find("fixture_" +  layerName + "_" + tile.fixtures[i].id)?.gameObject;

                    if (fixture != null)
                    {
                        fixtureConnectors[i] = fixture.GetComponent<AdjacencyConnector>();
                    }
                    else if (fixtures[i] = transform.Find("fixture_" + tile.fixtures[i].id)?.gameObject)
                    {
                        Debug.LogWarning("Fixture was found without new layer system");
                        fixtureConnectors[i] = fixture.GetComponent<AdjacencyConnector>();
                    }
                    else
                    {
                        // Update our tile object to make up for the fact that the object doesn't exist in the world.
                        // A user would have to fuck around in the editor to get to this point.
                        if (shouldWarn)
                            Debug.LogWarning("Tile's fixture was not created?");
                        CreateFixture(tile.fixtures[i], layers[i]);
                    }
                }
                else
                {
                    fixtures[i] = null;
                    fixtureConnectors[i] = null;
                }
                i++;
            }

            UpdateChildrenFromSubData(tile);
            UpdateSubDataFromChildren();

            // As extra fuckery ensure no NEW objects have been added either
            for (int j = transform.childCount - 1; j >= 0; j--) {
                var child = transform.GetChild(j).gameObject;

                if (child != turf && child != fixtures[j]) {
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
        private void CreateFixture(Fixture fixtureDefinition, FixtureLayers layer)
        {
            int index = (int)layer;
            if (fixtures[index] != null)
                EditorAndRuntime.Destroy(fixtures[index]);

            if (fixtureDefinition != null) {
                fixtures[index] = EditorAndRuntime.InstantiatePrefab(fixtureDefinition.prefab, transform);
                fixtureConnectors[index] = fixtures[index].GetComponent<AdjacencyConnector>();
            }
            else {
                fixtures[index] = null;
                fixtureConnectors[index] = null;
                return;
            }

            string layerName = Enum.GetName(typeof(FixtureLayers), layer).ToLower();
            fixtures[index].name = "fixture_" + layerName + "_" + fixtureDefinition.id;
        }

        private void CreateFixtures(Fixture[] fixturesDefinitation)
        {
            var layers = (FixtureLayers[])Enum.GetValues(typeof(FixtureLayers));
            int i = 0;

            foreach (Fixture fixture in fixturesDefinitation)
            {
                CreateFixture(fixture, layers[i]);
                i++;
            }
        }

        // TODO loopify
        private void UpdateChildrenFromSubData(TileDefinition newTile)
        {
            if (newTile.subStates != null && newTile.subStates.Length >= 1 && newTile.subStates[0] != null)
                turf?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[0]);

            if (newTile.subStates != null && newTile.subStates.Length >= 2 && newTile.subStates[1] != null)
                fixtures[0]?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[1]);

            if (newTile.subStates != null && newTile.subStates.Length >= 3 && newTile.subStates[2] != null)
                fixtures[1]?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[2]);

            if (newTile.subStates != null && newTile.subStates.Length >= 4 && newTile.subStates[3] != null)
                fixtures[2]?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[3]);

            if (newTile.subStates != null && newTile.subStates.Length >= 5 && newTile.subStates[4] != null)
                fixtures[3]?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[4]);

            if (newTile.subStates != null && newTile.subStates.Length >= 6 && newTile.subStates[5] != null)
                fixtures[4]?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[5]);

            if (newTile.subStates != null && newTile.subStates.Length >= 7 && newTile.subStates[6] != null)
                fixtures[5]?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[6]);
        }

        // TODO loopify
        private void UpdateSubDataFromChildren()
        {
            tile.subStates = new object[] {
                turf?.GetComponent<TileStateCommunicator>()?.GetTileState(),
                fixtures[0]?.GetComponent<TileStateCommunicator>()?.GetTileState(),
                fixtures[1]?.GetComponent<TileStateCommunicator>()?.GetTileState(),
                fixtures[2]?.GetComponent<TileStateCommunicator>()?.GetTileState(),
                fixtures[3]?.GetComponent<TileStateCommunicator>()?.GetTileState(),
                fixtures[4]?.GetComponent<TileStateCommunicator>()?.GetTileState(),
                fixtures[5]?.GetComponent<TileStateCommunicator>()?.GetTileState()
            };
        }

        [SerializeField]
        private TileDefinition tile = new TileDefinition();

        private GameObject turf = null;
        private AdjacencyConnector turfConnector = null; // may be null

        private GameObject[] fixtures = new GameObject[Enum.GetValues(typeof(FixtureLayers)).Length];
        private AdjacencyConnector[] fixtureConnectors = new AdjacencyConnector[Enum.GetValues(typeof(FixtureLayers)).Length];
    }
}
