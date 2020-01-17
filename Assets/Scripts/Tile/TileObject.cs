using UnityEngine;
using UnityEditor;
using System.Collections;

namespace TileMap {

    /**
     * The tile object takes information about a tile and transforms it into the world gameobject.
     */
    [ExecuteInEditMode]
    [SelectionBase]
    public class TileObject : MonoBehaviour
    {
        public ConstructibleTile Tile {
            get => tile;
            set => SetContents(value);
        }

        /**
         * Passes through an adjacency update to all children
         */
        public void UpdateSingleAdjacency(Direction direction, ConstructibleTile tile)
        {
            turfConnector?.UpdateSingle(direction, tile);
            fixtureConnector?.UpdateSingle(direction, tile);
        }
        /**
         * Passes through an adjacency update to all children
         */
        public void UpdateAllAdjacencies(ConstructibleTile[] tiles)
        {
            turfConnector?.UpdateAll(tiles);
            fixtureConnector?.UpdateAll(tiles);
        }

        /**
         * Fill in our non-serialized variables
         */
        private void Start()
        {
            // Fill in our references to objects using the saved information from our tile variable.
            // Effectively, this code expects the tile's children to match up to the turf and fixture.
            // If it finds any inconsistencies, it rectifies them.

            if(tile.turf) {
                turf = transform.Find("turf_" + tile.turf.id)?.gameObject;

                if(turf != null) {
                    turfConnector = turf.GetComponent<AdjacencyConnector>();
                }
                else {
                    // Update our tile object to make up for the fact that the object doesn't exist in the world.
                    // A user would have to fuck around in the editor to get to this point.
                    Debug.LogWarning("Tile's turf was not created? Creating now.");

                    // Create the object
                    CreateTurf(tile.turf);
                }
            }

            if(tile.fixture) {
                fixture = transform.Find("turf_" + tile.fixture.id)?.gameObject;

                if (fixture != null) {
                    fixtureConnector = fixture.GetComponent<AdjacencyConnector>();
                }
                else {
                    // Update our tile object to make up for the fact that the object doesn't exist in the world.
                    // A user would have to fuck around in the editor to get to this point.
                    Debug.LogWarning("Tile's turf was not created?");
                    
                    CreateFixture(tile.fixture, tile.fixtureDirection);
                }
            }

            // As extra fuckery ensure no NEW objects have been added either
            for(int i = transform.childCount - 1; i >= 0; i--) {
                var child = transform.GetChild(i).gameObject;

                if (child != turf && child != fixture) {
                    Debug.LogWarning("Unknown object found in tile " + name + ": " + child.name + ", deleting.");
                    #if UNITY_EDITOR
                    DestroyImmediate(child);
                    #else
                    Destroy(child);
                    #endif
                }
            }
        }

        #if UNITY_EDITOR
        /**
         * When the value is changed, refresh
         */
        private void OnValidate() {
            // If we haven't started yet, don't try to validate.
            if(!isActiveAndEnabled || turf == null)
                return;

            var tileManager = FindObjectOfType<TileManager>();
            if(tileManager.Tiles.Count == 0) // OnValidate gets called waaaay too often.
                return;

            // Update contents 
            SetContents(tile);

            // Inform the tilemanager that the tile has updated, so it can update surroundings
            tileManager.UpdateTile(transform.position, tile);
        }

        /**
         * If the user deletes a tile in the scene while in edit mode, notify the tile manager
         */
        private void OnDestroy()
        {
            // If we are playing a game, a tile removal will be initiated by the tilemanager. 
            if(EditorApplication.isPlaying)
                return;

            var tileManager = FindObjectOfType<TileManager>();
            tileManager?.RemoveTile(this);
        }
        #endif


        /**
         * Modify the tile based on the given information
         */
        private void SetContents(ConstructibleTile newTile)
        {
            // Replace previous tile contents with new contents, if they have changed.

            if (newTile.turf != tile.turf) {
                CreateTurf(newTile.turf);
            }

            if (newTile.fixture != tile.fixture) {
                if (fixture != null)
                    Destroy(fixture);

                fixture = Instantiate(
                    newTile.fixture.prefab,
                    new Vector2(),
                    Quaternion.Euler(0, DirectionHelper.ToAngle(newTile.fixtureDirection), 0),
                    transform
                );
                fixture.name = "fixture_" + newTile.fixture.id;
            }
            else if (newTile.fixtureDirection != tile.fixtureDirection && fixture != null) {
                fixture.transform.rotation = Quaternion.Euler(0, DirectionHelper.ToAngle(newTile.fixtureDirection), 0);
            }

            // Check if they have adjacency connectors, which we can then update.
            turfConnector = turf.GetComponent<AdjacencyConnector>();
            if(fixture != null)
                fixtureConnector = fixture.GetComponent<AdjacencyConnector>();

            tile = newTile;
        }

        private void CreateTurf(Turf turfDefinition)
        {
            if(turf != null) {
                #if UNITY_EDITOR
                DestroyImmediate(turf);
                #else
                Destroy(turf);
                #endif
            }

            turf = Instantiate(turfDefinition.prefab, transform);
            turf.name = "turf_" + turfDefinition.id;
        }
        private void CreateFixture(Fixture fixtureDefinition, Direction direction)
        {
            if (fixture != null) {
            #if UNITY_EDITOR
                DestroyImmediate(turf);
            #else
                Destroy(turf);
            #endif
            }

                fixture = Instantiate(fixtureDefinition.prefab, transform);
            fixture.name = "fixture_" + fixtureDefinition.id;
            // TODO: Allow this to work with non-standard fixture rotations.
            fixture.transform.localRotation *= Quaternion.Euler(0, DirectionHelper.ToAngle(direction), 0);
        }

        [SerializeField]
        private ConstructibleTile tile = new ConstructibleTile { turf = null, fixture = null, fixtureDirection = Direction.North };

        private GameObject turf = null;
        private AdjacencyConnector turfConnector = null; // may be null

        private GameObject fixture = null;
        private AdjacencyConnector fixtureConnector = null; // may be null
    }
}
