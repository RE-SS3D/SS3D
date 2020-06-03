using SS3D.Engine.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Profiling;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    [ExecuteAlways]
    public class AtmosManager : MonoBehaviour
    {
        public static int numOfGases = System.Enum.GetNames(typeof(AtmosStates)).Length;
        public enum ViewType { Pressure, Content, Temperature, Combined, Wind };
        public bool drawDebug = false;
        public bool drawAll = true;
        public bool drawWall = true;
        public bool showMessages = false;
        public bool isAddingGas = false;
        private AtmosGasses gasToAdd = AtmosGasses.Oxygen;

        
        public TileManager tileManager;
        private List<TileObject> tileObjects;
        private List<AtmosObject> atmosTiles;

        private float updateRate = 0f;
        private int activeTiles = 0;
        private float lastStep;
        private float lastClick;

        private ViewType drawView = ViewType.Pressure;

        // Performance markers
        static ProfilerMarker s_PreparePerfMarker = new ProfilerMarker("Atmospherics.Initialize");
        static ProfilerMarker s_StepPerfMarker = new ProfilerMarker("Atmospherics.Step");

        void Start()
        {
            if (tileManager == null)
            {
                tileManager = FindObjectOfType<TileManager>();
            }
            atmosTiles = new List<AtmosObject>();
            Initialize();
        }

        private void Initialize()
        {
            s_PreparePerfMarker.Begin();

            drawDebug = false;
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

                // Set airlocks to blocked
                if (tile.Tile.fixture != null)
                {
                    if (tile.Tile.fixture.name.Contains("Airlock"))
                    {
                        tile.atmos.SetBlocked(true);
                    }
                }
            }
            Debug.Log($"AtmosManager: Finished initializing {tilesInstantiated} tiles");

            lastStep = Time.fixedTime;
            s_PreparePerfMarker.End();
        }

        void Update()
        {
            if (Time.fixedTime >= lastStep)
            {
                activeTiles = Step();
                if (showMessages)
                    Debug.Log("Atmos loop took: " + (Time.fixedTime - lastStep) + " seconds, simulating " + activeTiles + " atmos tiles. Fixed update rate: " + updateRate);

                activeTiles = 0;
                lastStep = Time.fixedTime + updateRate;
            }

            // Display atmos tile contents if the editor window is open
            if (drawDebug)
            {
                Vector3 hit = GetMouse();
                Vector3 position = new Vector3(hit.x, 0, hit.z);

                Vector3 snappedPosition = tileManager.GetPositionClosestTo(position);
                if (snappedPosition != null)
                {
                    TileObject tile = tileManager.GetTile(snappedPosition);
                    if (tile == null)
                        return;

                    if (Time.fixedTime > lastClick + 1)
                    {
                        if (Input.GetMouseButton(0))
                        {
                            if (isAddingGas)
                            {
                                tile.atmos.AddGas(gasToAdd, 60f);
                            }
                            else
                            {
                                Debug.Log("Pressure (kPa): " + tile.atmos.GetPressure() + " Temperature (K): = " + tile.atmos.GetTemperature() + " State: " + tile.atmos.GetState().ToString() + "\t" + 
                                    " Oxygen content: " + tile.atmos.GetGasses()[0] +
                                    " Nitrogen content: " + tile.atmos.GetGasses()[1] +
                                    " Carbon Dioxide content: " + tile.atmos.GetGasses()[2] +
                                    " Plasma content: " + tile.atmos.GetGasses()[3]);
                                lastClick = Time.fixedTime;
                            }
                        }
                        else if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            isAddingGas = false;
                        }
                    }
                }
            }
        }

        private int Step()
        {
            s_StepPerfMarker.Begin();
            int activeTiles = 0;

            // Step 1: Calculate flux
            foreach (AtmosObject tile in atmosTiles)
            {
                if (tile.GetState() == AtmosStates.Active)
                {
                    tile.CalculateFlux();
                }
            }

            // Step 2: Simulate
            foreach (AtmosObject tile in atmosTiles)
            {
                AtmosStates state = tile.GetState();
                switch (state)
                {
                    case AtmosStates.Active:
                        tile.SimulateFlux();
                        activeTiles++;
                        break;
                    case AtmosStates.Semiactive:
                        tile.SimulateFlux();
                        break;
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
            s_StepPerfMarker.End();
            return activeTiles;
        }

        private void MoveVelocity(TileObject tileObject)
        {
            Vector2 velocity = tileObject.atmos.GetVelocity();
            // if (velocity.x > 0.5f || velocity.y > 0.5f)
            //{
                velocity *= 0.2f;
                Collider[] colliders = Physics.OverlapBox(tileObject.transform.position, new Vector3(1, 2.5f, 1));

                foreach (Collider collider in colliders)
                {
                    if (collider != null)
                    {
                        collider.attachedRigidbody?.AddForce(new Vector3(velocity.x, 0, velocity.y));
                    }
                }
                // tileObject.atmos.RemoveFlux();
            // }
        }

        public void SetUpdateRate(float updateRate)
        {
            this.updateRate = updateRate;
        }

        // Should be moved to the Atmos editor in the future
        public void SetViewType(ViewType viewType)
        {
            drawView = viewType;
        }

        // Should be moved to the Atmos editor in the future
        public void SetAddGas(AtmosGasses gas)
        {
            gasToAdd = gas;
        }

        // Should be moved to the Atmos editor in the future
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

        // Should be moved to the Atmos editor in the future
        private void OnDrawGizmos()
        {
            float drawSize = 0.8f;

            if (drawDebug)
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

                    Vector3 draw = new Vector3(x, 0, y) / 1f;

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
                        if (drawWall)
                            Gizmos.DrawCube(new Vector3(x, 0.5f, y), new Vector3(1, 2, 1));
                    }
                    else
                    {
                        switch (drawView)
                        {
                            case ViewType.Content:
                                float[] gases = new float[5];

                                Color[] colors = new Color[] { Color.yellow, Color.white, Color.gray, Color.magenta };
                                float offset = 0f;

                                for (int k = 0; k < 4; ++k)
                                {
                                    float moles = tile.atmos.GetGasses()[k] / 30f;

                                    if (moles != 0f)
                                    {
                                        Gizmos.color = colors[k] - state;
                                        if (drawAll || k == 3) // Only draw plasma
                                        {
                                            Gizmos.DrawCube(new Vector3(x, moles / 2f + offset, y), new Vector3(1 * drawSize, moles, 1 * drawSize));
                                            offset += moles;
                                        }
                                    }
                                }
                                break;
                            case ViewType.Pressure:
                                pressure = tile.atmos.GetPressure() / 160f;

                                if (drawAll || tile.atmos.GetState() == AtmosStates.Active)
                                {
                                    Gizmos.color = Color.white - state;
                                    Gizmos.DrawCube(new Vector3(x, pressure / 2f, y), new Vector3(1 * drawSize, pressure, 1 * drawSize));
                                }
                                break;
                            case ViewType.Temperature:
                                float temperatue = tile.atmos.GetTemperature() / 100f;

                                Gizmos.color = Color.red - state;
                                Gizmos.DrawCube(new Vector3(x, temperatue / 2f, y), new Vector3(1 * drawSize, temperatue, 1 * drawSize));
                                break;
                            case ViewType.Combined:
                                pressure = tile.atmos.GetPressure() / 30f;

                                Gizmos.color = new Color(tile.atmos.GetTemperature() / 500f, 0, 0, 1) - state;
                                Gizmos.DrawCube(new Vector3(x, pressure / 2f, y), new Vector3(1 * drawSize, pressure, 1 * drawSize));
                                break;
                            case ViewType.Wind:
                                Gizmos.color = Color.white;
                                Gizmos.DrawLine(new Vector3(x, 0, y), new Vector3(x + Mathf.Clamp(tile.atmos.GetVelocity().x, -1, 1), 0, y + Mathf.Clamp(tile.atmos.GetVelocity().y, -1, 1)));
                                break;
                        }
                    }
                }
                
            }
        }
    }
}