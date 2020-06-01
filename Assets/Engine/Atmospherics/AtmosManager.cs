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
        private enum ViewType { Content, Pressure, Temperature, Combined };

        public float updateRate = 0f;

        private TileManager tileManager;
        private List<TileObject> tileObjects;
        private List<AtmosObject> atmosTiles;

        private int activeTiles = 0;
        private float lastStep;
        private bool drawDebug = false;

        void Start()
        {
            tileManager = FindObjectOfType<TileManager>();
            atmosTiles = new List<AtmosObject>();

            Initialize();
        }

        private void Initialize()
        {
            Debug.Log("AtmosManager: Initializing tiles");

            // Initialize all tiles with atmos
            tileObjects = tileManager.GetAllTiles();

            int tilesInstantiated = 0;
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

                atmosTiles.Add(tile.atmos);
                tilesInstantiated++;
            }

            // Set neighbouring atmos after all are created
            foreach (TileObject tile in tileObjects)
            {
                tile.atmos.setAtmosNeighbours();
                // tile.atmos.ValidateVacuum();
            }
            Debug.Log($"AtmosManager: Finished initializing {tilesInstantiated} tiles");
            lastStep = Time.fixedTime;
            drawDebug = true;
        }

        void Update()
        {
            if (Time.fixedTime >= lastStep)
            {
                activeTiles = Step();
                if (activeTiles > 0)
                    Debug.Log("Ran for " + (Time.fixedTime - lastStep) + " seconds, simulating " + activeTiles + " atmos tiles");



                activeTiles = 0;
                lastStep = Time.fixedTime + updateRate;
            }

            Vector3 hit = GetMouse();
            Vector3 position = new Vector3(hit.x, 0, hit.z);

            Vector3 snappedPosition = tileManager.GetPositionClosestTo(position);
            if (snappedPosition != null)
            {
                TileObject tile = tileManager.GetTile(snappedPosition);
                if (tile == null)
                    return;

                if (Input.GetMouseButton(0))
                {
                    tile.atmos.AddGas(AtmosGasses.Oxygen, 60f);
                }
                else if (Input.GetMouseButton(1))
                {
                    tile.atmos.MakeEmpty();
                }
                else if (Input.GetMouseButton(3))
                {
                    tile.atmos.AddGas(AtmosGasses.Plasma, 60f);
                }
                else if (Input.GetKeyDown("h"))
                {
                    tile.atmos.AddHeat(2000f);
                }
                else if (Input.GetKeyDown("j"))
                {
                    tile.atmos.RemoveHeat(2000f);
                }
                else if (Input.GetMouseButton(4))
                {
                    // Debug.Log("Oxygen content: " + tile.atmos.GetGasses()[0] + " Pressure: " + tile.atmos.GetPressure());
                    // Debug.Log("Plasma content: " + tile.atmos.GetGasses()[3]);
                    // Debug.Log("Velocity: " + tile.atmos.GetVelocity());
                    Debug.Log("Pressure (kPa): " + tile.atmos.GetPressure() + " Temperature (K): = " + tile.atmos.GetTemperature());
                }
            }
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
                {
                    activeTiles++;
                }
            }

            // Step 3: Move items according to the wind velocity
            foreach (TileObject tile in tileObjects)
            {
                Vector2 velocity = tile.atmos.GetVelocity();
                if (velocity != Vector2.zero)
                {
                    MoveVelocity(tile);
                }
            }

            return activeTiles;
        }

        private void MoveVelocity(TileObject tileObject)
        {
            Vector2 velocity = tileObject.atmos.GetVelocity();
            if (velocity.x > 0.5f || velocity.y > 0.5f)
            {
                velocity *= 0.2f;
                Collider[] colliders = Physics.OverlapBox(tileObject.transform.position, new Vector3(1, 2.5f, 1));

                foreach (Collider collider in colliders)
                {
                    if (collider != null)
                    {
                        collider.attachedRigidbody?.AddForce(new Vector3(velocity.x, 0, velocity.y));
                    }
                }
            }
        }


        private Vector3 GetMouse()
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float distance;

            if (plane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.down;
        }

        private void OnDrawGizmos()
        {
            bool drawAll = true;
            float drawRadius = 3.5f;
            ViewType drawView = ViewType.Temperature;

            if (drawDebug)
            {
                Vector3 hit = GetMouse();

                if (hit != Vector3.down)
                {
                    // For each tile in the tilemap
                    foreach (TileObject tile in tileManager.GetAllTiles())
                    {
                        // ugly hack to get coordinates
                        string[] coords = tile.name.Split(',');
                        int xTemp = Int32.Parse(coords[0].Replace("[", ""));
                        int yTemp = Int32.Parse(coords[1].Replace("]", ""));

                        var realcoords = tileManager.GetPosition(xTemp, yTemp);
                        float x = realcoords.x;
                        float y = realcoords.z;

                        Vector3 sizeFactor = new Vector3(0.1f, 0.1f, 0.1f);

                        Vector3 draw = new Vector3(x, 0, y) / 1f;

                        if (Vector3.Distance(draw, hit) < drawRadius || drawAll)
                        {
                            Color state;
                            switch (tile.atmos.GetState())
                            {
                                case AtmosStates.Active: state = new Color(0, 0, 0, 0); break;
                                case AtmosStates.Semiactive: state = new Color(0, 0, 0, 0.8f); break;
                                case AtmosStates.Inactive: state = new Color(0, 0, 0, 0.8f); break;
                                default: state = new Color(0, 0, 0, 1); break;
                            }

                            float pressure;

                            if (tile.atmos.GetState() == AtmosStates.Blocked)
                            {
                                Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 1f);

                                // Draw black cube where atmos flow is blocked
                                Gizmos.DrawCube(new Vector3(x, 0.5f, y), new Vector3(1, 2, 1));
                            }
                            else
                            {
                                switch (drawView)
                                {
                                    case ViewType.Content:
                                        float[] gases = new float[5];
                                        Color[] colors = new Color[] { Color.white, Color.white, Color.gray, Color.magenta };

                                        float offset = 0f;

                                        //for (int k = 3)//(int k = 0; k < 4; ++k)
                                        {
                                            float moles = tile.atmos.GetGasses()[3] / 30f; // k

                                            if (moles != 0f)
                                            {
                                                Gizmos.color = colors[3] - state;  // 
                                                Gizmos.DrawCube(new Vector3(x, moles / 2f + offset, y), new Vector3(1, moles, 1));
                                                offset += moles;
                                            }
                                        }
                                        break;
                                    case ViewType.Pressure:
                                        pressure = tile.atmos.GetPressure() / 160f; // 30f

                                        Gizmos.color = Color.white - state;
                                        Gizmos.DrawCube(new Vector3(x, pressure / 2f, y), new Vector3(0.8f, pressure, 0.8f)); // 1f
                                        break;
                                    case ViewType.Temperature:
                                        float temperatue = tile.atmos.GetTemperature() / 100f;

                                        Gizmos.color = Color.red - state;
                                        Gizmos.DrawCube(new Vector3(x, temperatue / 2f, y), new Vector3(1, temperatue, 1));
                                        break;
                                    case ViewType.Combined:
                                        pressure = tile.atmos.GetPressure() / 30f;

                                        Gizmos.color = new Color(tile.atmos.GetTemperature() / 500f, 0, 0, 1) - state;
                                        Gizmos.DrawCube(new Vector3(x, pressure / 2f, y), new Vector3(1, pressure, 1));
                                        break;
                                }
                            }
                        }
                    }
                }

            }
        }
    }
}