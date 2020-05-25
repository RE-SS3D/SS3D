using SS3D.Engine.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class AtmosManager : MonoBehaviour
    {
        public static int numOfGases = System.Enum.GetNames(typeof(AtmosStates)).Length;

        public float updateRate = 0f;

        private TileManager tileManager;

        private List<TileObject> tileObjects;
        private List<AtmosObject> atmosTiles;

        private int activeTiles = 0;
        private bool threadDone = true;
        private float lastStep;

        // Start is called before the first frame update
        void Start()
        {
            tileManager = FindObjectOfType<TileManager>();
            atmosTiles = new List<AtmosObject>();

            // Ugly hack to wait for all tiles to be initialized
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            yield return new WaitForSeconds(5);
            Debug.Log("AtmosManager: Initializing tiles");

            // Initialize all tiles with atmos
            tileObjects = tileManager.GetAllTiles();
            foreach (TileObject tile in tileObjects)
            {
                tile.atmos = ScriptableObject.CreateInstance<AtmosObject>();
                tile.atmos.MakeEmpty();
                tile.atmos.MakeAir();
                tile.atmos.RemoveFlux();
                if (tile.Tile.turf.isWall)
                {
                    tile.atmos.SetBlocked(true);
                }

                // Set neighbouring tiles... kill me
                string[] coords = tile.name.Split(',');
                int x = Int32.Parse(coords[0].Replace("[", ""));
                int y = Int32.Parse(coords[1].Replace("]", ""));


                // Ugly AF, needs to be loopified
                // Top
                Tuple<int, int> tileCoordinates = DirectionHelper.ToCardinalVector(Direction.North);
                TileObject tileNeighbour = tileManager.GetTile(tileCoordinates.Item1 + x, tileCoordinates.Item2 + y);
                tile.atmos.setTileNeighbour(tileNeighbour, 0);

                // Bottom
                Tuple<int, int> tileCoordinates2 = DirectionHelper.ToCardinalVector(Direction.South);
                TileObject tileNeighbour2 = tileManager.GetTile(tileCoordinates2.Item1 + x, tileCoordinates2.Item2 + y);
                tile.atmos.setTileNeighbour(tileNeighbour2, 1);

                // Left
                Tuple<int, int> tileCoordinates3 = DirectionHelper.ToCardinalVector(Direction.West);
                TileObject tileNeighbour3 = tileManager.GetTile(tileCoordinates3.Item1 + x, tileCoordinates3.Item2 + y);
                tile.atmos.setTileNeighbour(tileNeighbour3, 2);

                // Right
                Tuple<int, int> tileCoordinates4 = DirectionHelper.ToCardinalVector(Direction.East);
                TileObject tileNeighbour4 = tileManager.GetTile(tileCoordinates4.Item1 + x, tileCoordinates4.Item2 + y);
                tile.atmos.setTileNeighbour(tileNeighbour4, 3);


                //for (Direction direction = Direction.North; direction <= Direction.West; direction++)
                //{
                //    Tuple<int, int> tileCoordinates = DirectionHelper.ToCardinalVector(direction);
                //    TileObject tileNeighbour = tileManager.GetTile(tileCoordinates.Item1 + x, tileCoordinates.Item2 + y);
                //    tile.atmos.setTileNeighbour(tileNeighbour, (int)direction);
                //}

                atmosTiles.Add(tile.atmos);
            }

            // Set neighbouring atmos after all are created
            foreach (TileObject tile in tileObjects)
            {
                tile.atmos.setAtmosNeighbours();
            }
            Debug.Log("AtmosManager: Finished initializing tiles");
            lastStep = Time.fixedTime;
        }

        void Update()
        {
            Debug.Log("AtmosManager: Running update");
            if (Time.fixedTime >= lastStep && threadDone)
            {
                activeTiles = Step();
                Debug.Log("Ran for " + (Time.fixedTime - lastStep) + " seconds, simulating " + activeTiles + " atmos tiles");

                threadDone = true; // false
                // new Thread(Thread).Start();
                

                activeTiles = 0;
                lastStep = Time.fixedTime + updateRate;
            }
        }

        // TODO
        //
        // Set neighbouring tiles
        // Set blocked state if wall or active state if floor -> Done
        // Set airlock blocked state based on if door open

        private void Thread()
        {
            activeTiles = Step();
            threadDone = true;
        }

        private int Step()
        {
            int activeTiles = 0;

            // Step 1: Calculate flux
            foreach (AtmosObject tile in atmosTiles)
            {
                tile.CalculateFlux();
            }

            // Step 2: Simulate
            foreach (AtmosObject tile in atmosTiles)
            {
                tile.SimulateFlux();
                if (tile.GetState() == AtmosStates.Active)
                    activeTiles++;
            }

            return activeTiles;
        }
    }
}