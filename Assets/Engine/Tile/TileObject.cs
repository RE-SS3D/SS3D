using Tile;
using UnityEngine;
using UnityEditor;

using SS3D.Engine.Tiles.Connections;
using SS3D.Engine.Tiles.State;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using SS3D.Engine.Atmospherics;

namespace SS3D.Engine.Tiles
{
    /**
     * The tile object takes information about a tile and transforms it into the world gameobject.
     */
    [ExecuteAlways]
    [SelectionBase]
    public class TileObject : MonoBehaviour
    {
        public TileDefinition Tile
        {
            get => tile;
            set => SetContents(value);
        }

        /**
         * Passes through an adjacency update to all children in the Tiles fixture layer
         */
        public void UpdateTileSingleAdjacency(Direction direction, TileDefinition tile, TileFixtureLayers layer)
        {
            int index = (int)layer;
            tileFixtureConnectors[index]?.UpdateSingle(direction, tile);
        }


        /**
         * Passes through an adjacency update to all children in the Floors fixture layer
         */
        public void UpdateFloorSingleAdjacency(Direction direction, TileDefinition tile, FloorFixtureLayers layer)
        {
            int index = (int)layer;
            floorFixtureConnectors[index]?.UpdateSingle(direction, tile);
        }

        /**
         * Passes through an adjacency update to all children and all layers.
         */
        public void UpdateSingleAdjacency(Direction direction, TileDefinition tile)
        {
            // Handle plenum and turf
            plenumConnector?.UpdateSingle(direction, tile);
            turfConnector?.UpdateSingle(direction, tile);

            // Go through Tile fixtures first
            var tileLayers = TileDefinition.GetTileFixtureLayerNames();
            foreach (TileFixtureLayers layer in tileLayers)
            {
                UpdateTileSingleAdjacency(direction, tile, layer);
            }

            // Go through Floor fixtures second
            var floorLayers = TileDefinition.GetTileFixtureLayerNames();
            foreach (FloorFixtureLayers layer in floorLayers)
            {
                UpdateFloorSingleAdjacency(direction, tile, layer);
            }
        }

        /**
         * Passes through an adjacency update to all children
         */
        public void UpdateAllTileAdjacencies(TileDefinition[] tiles, TileFixtureLayers layer)
        {
            int index = (int)layer;
            AdjacencyConnector ac = tileFixtureConnectors[index];
            if (ac != null)
            {
                ac.Layer = layer;
                ac?.UpdateAll(tiles);
            }
        }

        public void UpdateAllFloorAdjacencies(TileDefinition[] tiles, FloorFixtureLayers layer)
        {
            int index = (int)layer;
            AdjacencyConnector ac = floorFixtureConnectors[index];
            if (ac != null)
            {
                ac.Layer = layer;
                ac?.UpdateAll(tiles);
            }
        }

        public void UpdateAllAdjacencies(TileDefinition[] tiles)
        {
            // Update plenum first
            plenumConnector?.UpdateAll(tiles);
            turfConnector?.UpdateAll(tiles);

            // Update every tile layer
            var tileLayers = TileDefinition.GetTileFixtureLayerNames();
            foreach (TileFixtureLayers layer in tileLayers)
            {
                UpdateAllTileAdjacencies(tiles, layer);
            }

            // Update every floor layer
            var floorLayers = TileDefinition.GetTileFixtureLayerNames();
            foreach (FloorFixtureLayers layer in floorLayers)
            {
                UpdateAllFloorAdjacencies(tiles, layer);
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
        private void OnValidate()
        {
            // If we haven't started yet, don't try to validate.
            if (!this || tile.IsEmpty())
                return;

            var tileManager = transform.root.GetComponent<TileManager>();

            // Can't do most things in OnValidate, so wait a sec.
            EditorApplication.delayCall += () => {
                if (!this)
                    return;

                // Update contents
                UpdateContents(false);
                // Inform the tilemanager that the tile has updated, so it can update surroundings
                if (tileManager != null && tileManager.Count > 0 && !TileMapEditorHelpers.IsGhostTile(this))
                {
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
            if (EditorApplication.isPlaying)
                return;

            var tileManager = transform.root.GetComponent<TileManager>();
            if (tileManager != null && !TileMapEditorHelpers.IsGhostTile(this))
                tileManager.RemoveTile(this);
        }
#endif


        /**
         * Modify the tile based on the given information
         */
        private void SetContents(TileDefinition newTile)
        {
            if (newTile.plenum != tile.plenum)
                CreatePlenum(newTile.plenum);
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
            if (tile.plenum)
            {
                plenum = transform.Find("plenum_" + tile.plenum.id)?.gameObject;

                if (plenum == null)
                {
                    if (shouldWarn)
                        Debug.LogWarning("Tile's plenum was not created? Creating now.");

                    // Create the object
                    CreatePlenum(tile.plenum);
                }
                else
                {
                    plenumConnector = plenum.GetComponent<AdjacencyConnector>();
                }
            }
            else
            {
                plenum = null;
            }


            if (tile.turf)
            {
                turf = transform.Find("turf_" + tile.turf.id)?.gameObject;

                if (turf != null)
                {
                    turfConnector = turf.GetComponent<AdjacencyConnector>();
                }
                else
                {
                    // Update our tile object to make up for the fact that the object doesn't exist in the world.
                    // A user would have to fuck around in the editor to get to this point.
                    if (shouldWarn)
                        Debug.LogWarning("Tile's turf was not created? Creating now.");

                    // Create the object
                    CreateTurf(tile.turf);
                }
            }
            else
            {
                turf = null;
                turfConnector = null;
            }

            ValidateFixtures(shouldWarn);

            UpdateChildrenFromSubData(tile);
            UpdateSubDataFromChildren();

            // As extra fuckery ensure no NEW objects have been added either
            for (int j = transform.childCount - 1; j >= 0; j--)
            {
                var child = transform.GetChild(j).gameObject;

                if (child != plenum && child != turf && !fixtures.Contains(child))
                {
                    if (shouldWarn)
                    {
#if UNITY_EDITOR
                        if (MigrateTileDefinition())
                        {
                            Debug.Log("Succesfully migrated " + child.name);
                        }
                        else
                        {
                            Debug.LogWarning("Unknown object found in tile " + name + ": " + child.name + ", deleting.");
                        }
#endif
                    }
                    EditorAndRuntime.Destroy(child);
                }
            }

            //// Set fixture layer size if not set
            //if (tile.fixtures?.Length != TileDefinition.GetFixtureLayerSize())
            //    tile.fixtures = new Fixture[TileDefinition.GetFixtureLayerSize()];
        }

        private void ValidateFixtures(bool shouldWarn)
        {
            int i = 0;

            var layers = TileDefinition.GetTileFixtureLayerNames();

            // FixturesContainer must exist
            if (tile.fixtures != null)
            {
                // Loop through every tile layer
                foreach (TileFixtureLayers layer in TileDefinition.GetTileFixtureLayerNames())
                {
                    var tileFixture = tile.fixtures.GetTileFixtureAtLayer(layer);
                    if (tileFixture != null)
                    {

                        string layerName = layers[i].ToString();
                        fixtures[i] = transform.Find("fixture_" + "tile_" + layerName.ToLower() + "_" + tileFixture.id)?.gameObject;

                        if (fixtures[i] != null)
                        {
                            tileFixtureConnectors[i] = fixtures[i].GetComponent<AdjacencyConnector>();
                        }
                        else
                        {
                            // Update our tile object to make up for the fact that the object doesn't exist in the world.
                            // A user would have to fuck around in the editor to get to this point.
                            if (shouldWarn)
                                Debug.LogWarning("Tile Fixture in Tile but not in TileObject. Creating: " + tileFixture.name);
                            CreateTileFixture(tileFixture, layer);
                        }
                    }
                    else
                    {
                        fixtures[i] = null;
                        tileFixtureConnectors[i] = null;
                    }
                    i++;
                }

                // Loop through every wall layer
                foreach (WallFixtureLayers layer in TileDefinition.GetWallFixtureLayerNames())
                {
                    var wallFixture = tile.fixtures.GetWallFixtureAtLayer(layer);
                    if (wallFixture != null)
                    {

                        string layerName = layers[i - TileDefinition.GetTileFixtureLayerSize()].ToString();
                        fixtures[i] = transform.Find("fixture_" + "wall_" + layerName.ToLower() + "_" + wallFixture.id)?.gameObject;

                        if (fixtures[i] == null)
                        {
                            // Update our tile object to make up for the fact that the object doesn't exist in the world.
                            // A user would have to fuck around in the editor to get to this point.
                            if (shouldWarn)
                                Debug.LogWarning("Wall Fixture in Tile but not in TileObject. Creating: " + wallFixture.name);
                            CreateWallFixture(wallFixture, layer);
                        }
                    }
                    else
                    {
                        fixtures[i] = null;
                    }
                    i++;
                }

                // Loop through every floor layer
                foreach (FloorFixtureLayers layer in TileDefinition.GetFloorFixtureLayerNames())
                {
                    var floorFixture = tile.fixtures.GetFloorFixtureAtLayer(layer);
                    if (floorFixture != null)
                    {

                        string layerName = layers[i - TileDefinition.GetTileFixtureLayerSize() - TileDefinition.GetWallFixtureLayerSize()].ToString();
                        fixtures[i] = transform.Find("fixture_" + "floor_" + layerName.ToLower() + "_" + floorFixture.id)?.gameObject;

                        if (fixtures[i] != null)
                        {
                            floorFixtureConnectors[i - TileDefinition.GetTileFixtureLayerSize() - TileDefinition.GetWallFixtureLayerSize()] = fixtures[i].GetComponent<AdjacencyConnector>();
                        }
                        else
                        {
                            // Update our tile object to make up for the fact that the object doesn't exist in the world.
                            // A user would have to fuck around in the editor to get to this point.
                            if (shouldWarn)
                                Debug.LogWarning("Tile Fixture in Tile but not in TileObject. Creating: " + floorFixture.name);
                            CreateFloorFixture(floorFixture, layer);
                        }
                    }
                    else
                    {
                        fixtures[i] = null;
                        floorFixtureConnectors[i - TileDefinition.GetTileFixtureLayerSize() - TileDefinition.GetWallFixtureLayerSize()] = null;
                    }
                    i++;
                }
            

            }
            else
            {
                //fixtureConnectors = new AdjacencyConnector[TileDefinition.GetFixtureLayerSize()];
            }
        }


        private void CreatePlenum(Plenum plenumDefinition)
        {
            if (plenum != null)
                EditorAndRuntime.Destroy(plenum);
            plenum = EditorAndRuntime.InstantiatePrefab(plenumDefinition.prefab, transform);

            if (plenumDefinition != null)
            {
                plenum.name = "plenum_" + plenumDefinition.id;
                plenumConnector = plenum.GetComponent<AdjacencyConnector>();
            }
            else
            {
                plenum = null;
                plenumConnector = null;
            }
        }

        private void CreateTurf(Turf turfDefinition)
        {
            if (turf != null)
                EditorAndRuntime.Destroy(turf);
            if(turfDefinition != null)
            {
                turf = EditorAndRuntime.InstantiatePrefab(turfDefinition.prefab, transform);

                turf.name = "turf_" + turfDefinition.id;
                turfConnector = turf.GetComponent<AdjacencyConnector>();
            }
            else
            {
                turf = null;
                turfConnector = null;
            }
        }
        private void CreateTileFixture(Fixture fixtureDefinition, TileFixtureLayers layer)
        {
            int index = (int)layer;
            if (fixtures[index] != null)
                EditorAndRuntime.Destroy(fixtures[index]);

            if (fixtureDefinition != null)
            {
                if (fixtures[index] != null)
                {
                    Debug.LogWarning("Trying to overwrite fixture");
                }
                fixtures[index] = EditorAndRuntime.InstantiatePrefab(fixtureDefinition.prefab, transform);
                tileFixtureConnectors[index] = fixtures[index].GetComponent<AdjacencyConnector>();
            }
            else
            {
                fixtures[index] = null;
                tileFixtureConnectors[index] = null;
                return;
            }

            string layerName = Enum.GetName(typeof(TileFixtureLayers), layer).ToLower();
            fixtures[index].name = "fixture_" + "tile_" + layerName + "_" + fixtureDefinition.id;
        }

        private void CreateWallFixture(Fixture fixtureDefinition, WallFixtureLayers layer)
        {
            int index = (int)layer;
            int offset = TileDefinition.GetTileFixtureLayerSize();
            if (fixtures[index + offset] != null)
                EditorAndRuntime.Destroy(fixtures[index + offset]);

            if (fixtureDefinition != null)
            {
                if (fixtures[index + offset] != null)
                {
                    Debug.LogWarning("Trying to overwrite fixture");
                }
                fixtures[index + offset] = EditorAndRuntime.InstantiatePrefab(fixtureDefinition.prefab, transform);
            }
            else
            {
                fixtures[index + offset] = null;
                return;
            }

            string layerName = Enum.GetName(typeof(WallFixtureLayers), layer).ToLower();
            fixtures[index + offset].name = "fixture_" + "wall_" + layerName + "_" + fixtureDefinition.id;
        }

        private void CreateFloorFixture(Fixture fixtureDefinition, FloorFixtureLayers layer)
        {
            int index = (int)layer;
            int offset = TileDefinition.GetTileFixtureLayerSize() + TileDefinition.GetWallFixtureLayerSize();
            if (fixtures[index + offset] != null)
                EditorAndRuntime.Destroy(fixtures[index + offset]);

            if (fixtureDefinition != null)
            {
                if (fixtures[index + offset] != null)
                {
                    Debug.LogWarning("Trying to overwrite fixture");
                }
                fixtures[index + offset] = EditorAndRuntime.InstantiatePrefab(fixtureDefinition.prefab, transform);
                floorFixtureConnectors[index] = fixtures[index + offset].GetComponent<AdjacencyConnector>();
            }
            else
            {
                fixtures[index + offset] = null;
                floorFixtureConnectors[index] = null;
                return;
            }

            string layerName = Enum.GetName(typeof(TileFixtureLayers), layer).ToLower();
            fixtures[index + offset].name = "fixture_" + "floor_" + layerName + "_" + fixtureDefinition.id;
        }

        private void CreateFixtures(FixturesContainer fixturesDefinition)
        {
            //var layers = (FixtureLayers[])Enum.GetValues(typeof(FixtureLayers));
            //for (int i = 0; i < fixturesDefinition.Length; i++)
            //{
            //    CreateFixture(fixturesDefinition[i], layers[i]);
            //}

            //foreach (FixtureLayers layer in TileDefinition.GetFixtureLayerNames())
            //{
            //    CreateFixture(fixturesDefinition.GetFixtureAtLayer(layer), layer);
            //}

            foreach (TileFixtureLayers layer in TileDefinition.GetTileFixtureLayerNames())
            {
                CreateTileFixture(fixturesDefinition.GetTileFixtureAtLayer(layer), layer);
            }

            foreach (WallFixtureLayers layer in TileDefinition.GetWallFixtureLayerNames())
            {
                CreateWallFixture(fixturesDefinition.GetWallFixtureAtLayer(layer), layer);
            }

            foreach (FloorFixtureLayers layer in TileDefinition.GetFloorFixtureLayerNames())
            {
                CreateFloorFixture(fixturesDefinition.GetFloorFixtureAtLayer(layer), layer);
            }

        }

        private void UpdateChildrenFromSubData(TileDefinition newTile)
        {
            if (newTile.subStates != null && newTile.subStates.Length >= 1 && newTile.subStates[0] != null)
                plenum?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[0]);

            if (newTile.subStates != null && newTile.subStates.Length >= 2 && newTile.subStates[1] != null)
                turf?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[1]);

            int i = 0;
            foreach (GameObject fixture in fixtures)
            {
                if (newTile.subStates != null && newTile.subStates.Length >= i + 3 && newTile.subStates[i + 2] != null)
                {
                    fixtures[i]?.GetComponent<TileStateCommunicator>()?.SetTileState(newTile.subStates[i + 2]);
                }
                i++;
            }
        }

        private void UpdateSubDataFromChildren()
        {
            // Plenum + Turf + all fixtures layers
            tile.subStates = new object[1 + TileDefinition.GetAllFixtureLayerSize()];

            tile.subStates[0] = plenum != null ? plenum?.GetComponent<TileStateCommunicator>()?.GetTileState() : null;
            tile.subStates[1] = turf != null ? turf?.GetComponent<TileStateCommunicator>()?.GetTileState() : null;

            int i = 2;
            foreach (GameObject fixture in fixtures)
            {
                if (fixture)
                    tile.subStates[i] = fixture?.GetComponent<TileStateCommunicator>()?.GetTileState();
                i++;
            }
        }
#if UNITY_EDITOR
        /**
         * Migrates existing fixtures that do not have a fixturelayer set.
         */
        private bool MigrateTileDefinition()
        {
            //// set array to proper size
            //// tile.fixtures = new Fixture[TileDefinition.GetFixtureLayerSize()];
            //Fixture oldFurniture = null;
            //FixtureLayers assetLayer = FixtureLayers.Furniture;

            //// Determine all assets
            //List<Fixture> fixtureList = new List<Fixture>();
            //string[] aMaterialFiles = Directory.GetFiles(Application.dataPath, "*.asset", SearchOption.AllDirectories);
            //foreach (string matFile in aMaterialFiles)
            //{
            //    string assetPath = "Assets" + matFile.Replace(Application.dataPath, "").Replace('\\', '/');
            //    Fixture sourceFixture = (Fixture)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Fixture));
            //    if (sourceFixture != null)
            //        fixtureList.Add(sourceFixture);
            //}

            //// find old fixture
            //foreach (Transform child in transform)
            //{
            //    if (child.gameObject.name.Contains("fixture"))
            //    {
            //        fixtures[0] = child.gameObject;
            //        string assetNameWithLayer = fixtures[0].name.Replace("fixture_", "");

            //        // We got the asset name, now get the layer name
            //        foreach (FixtureLayers layer in TileDefinition.GetFixtureLayerNames())
            //        {
            //            if (assetNameWithLayer.Contains(layer.ToString().ToLower()))
            //            {
            //                assetLayer = layer;
            //            }
            //        }
            //        string assetName = assetNameWithLayer.Replace(assetLayer.ToString().ToLower() + "_", "");
            //        foreach (Fixture fix in fixtureList)
            //        {

            //            if (fix.id.Equals(assetName))
            //                oldFurniture = fix;
            //        }
            //    }
            //}

            //// update reference
            //if (oldFurniture != null)
            //{
            //    tile.fixtures.SetFixtureAtLayer(oldFurniture, assetLayer);
            //    return true;
            //}
            return false;
        }
#endif
        [SerializeField]
        private TileDefinition tile = new TileDefinition();

        private GameObject plenum = null;
        private AdjacencyConnector plenumConnector = null;

        private GameObject turf = null;
        private AdjacencyConnector turfConnector = null; // may be null
        public AtmosObject atmos;

        // Total fixtures = tile + wall + floor
        private GameObject[] fixtures = new GameObject[TileDefinition.GetAllFixtureLayerSize()];
        private AdjacencyConnector[] tileFixtureConnectors = new AdjacencyConnector[TileDefinition.GetTileFixtureLayerSize()];
        private AdjacencyConnector[] floorFixtureConnectors = new AdjacencyConnector[TileDefinition.GetFloorFixtureLayerSize()];
    }
}
