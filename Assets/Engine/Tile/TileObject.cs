using Tile;
using UnityEngine;
using UnityEditor;

using SS3D.Engine.Tiles.Connections;
using SS3D.Engine.Tiles.State;
using System;
using System.Linq;

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
            var layers = TileDefinition.GetFixtureLayerNames();

            if (tile.fixtures != null)
            {
                foreach (var tileFixture in tile.fixtures)
                {
                    if (tileFixture != null)
                    {

                        string layerName = layers[i].ToString();
                        fixtures[i] = transform.Find("fixture_" + layerName.ToLower() + "_" + tileFixture.id)?.gameObject;

                        if (fixtures[i] != null)
                        {
                            fixtureConnectors[i] = fixtures[i].GetComponent<AdjacencyConnector>();
                        }
                        else if (fixtures[i] = transform.Find("fixture_" + tile.fixtures[i].id)?.gameObject)
                        {
                            fixtureConnectors[i] = fixtures[i].GetComponent<AdjacencyConnector>();
                            if (shouldWarn)
                                Debug.LogWarning("Old furniture found, updating:" + tile.fixtures[i].name);
                        }
                        else
                        {
                            // Update our tile object to make up for the fact that the object doesn't exist in the world.
                            // A user would have to fuck around in the editor to get to this point.
                            if (shouldWarn)
                                Debug.LogWarning("Fixture in Tile but not in TileObject. Creating: " + tile.fixtures[i].name);
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
            }
            else
            {
                // fixtures = null;
                // fixtureConnectors = null;
                
                fixtureConnectors = new AdjacencyConnector[TileDefinition.GetFixtureLayerSize()];
            }

            if (fixtures == null)
            {
                Debug.LogWarning("Repairing fixtures");
                fixtures = new GameObject[TileDefinition.GetFixtureLayerSize()];
            }

            UpdateChildrenFromSubData(tile);
            UpdateSubDataFromChildren();

            // As extra fuckery ensure no NEW objects have been added either
            for (int j = transform.childCount - 1; j >= 0; j--)
            {
                var child = transform.GetChild(j).gameObject;

                if (child != turf && !fixtures.Contains(child))
                {
                    if (shouldWarn)
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
                if (fixtures[index] != null)
                {
                    Debug.LogWarning("Trying to overwrite fixture");
                }
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

            if (fixturesDefinitation == null)
                Debug.LogError("fixturesDefinition is empty");

            foreach (Fixture fixture in fixturesDefinitation)
            {
                if (fixture != null)
                {
                    CreateFixture(fixture, layers[i]);
                }
                i++;
            }
        }

        private void UpdateChildrenFromSubData(TileDefinition newTile)
        {
            if (newTile.subStates != null && newTile.subStates.Length >= 1 && newTile.subStates[0] != null)
                turf?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[0]);

            int i = 0;
            foreach (GameObject fixture in fixtures)
            {
                if (newTile.subStates != null && newTile.subStates.Length >= i+2 && newTile.subStates[i + 1] != null)
                {
                    fixtures[i]?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[i + 1]);
                }
                i++;
            }
        }

        private void UpdateSubDataFromChildren()
        {
            // Turf + all fixtures layers
            tile.subStates = new object[1 + TileDefinition.GetFixtureLayerSize()];
            tile.subStates[0] = turf?.GetComponent<TileStateCommunicator>()?.GetTileState();

            int i = 1;
            foreach (GameObject fixture in fixtures)
            {
                tile.subStates[i] = fixture?.GetComponent<TileStateCommunicator>()?.GetTileState();
                i++;
            }
        }

        [SerializeField]
        private TileDefinition tile = new TileDefinition();

        private GameObject turf = null;
        private AdjacencyConnector turfConnector = null; // may be null

        private GameObject[] fixtures = new GameObject[Enum.GetValues(typeof(FixtureLayers)).Length];
        private AdjacencyConnector[] fixtureConnectors = new AdjacencyConnector[Enum.GetValues(typeof(FixtureLayers)).Length];
    }
}
