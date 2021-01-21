using SS3D.Engine.Tiles;
using SS3D.Engine.Tiles.Connections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    // [ExecuteAlways]
    public class AtmosManager : MonoBehaviour
    {
        // Test
        public Turf floor;

        public enum ViewType { Pressure, Content, Temperature, Combined, Wind };
        public bool drawDebug = false;
        public bool drawTiles = true;
        public bool drawAll = true;
        public bool drawWall = true;
        public bool showMessages = false;
        public bool isAddingGas = false;
        public bool showPipes = false;
        public bool showOnlySelectedPipes = false;
        private AtmosGasses gasToAdd = AtmosGasses.Oxygen;
        public PipeLayer selectedPipeLayer = PipeLayer.Upper;

        private TileManager tileManager;
        private List<TileObject> tileObjects;
        private List<AtmosObject> atmosTiles;
        private List<PipeObject> pipeTiles;
        private List<IAtmosLoop> deviceTiles;

        private float updateRate = 0.1f;
        private int activeTiles = 0;
        private float lastStep;
        private float lastClick;
        private ViewType drawView = ViewType.Pressure;

        // Performance markers
        static ProfilerMarker s_PreparePerfMarker = new ProfilerMarker("Atmospherics.Initialize");
        static ProfilerMarker s_StepPerfMarker = new ProfilerMarker("Atmospherics.Step");

        void Start()
        {
            // Atmos manager only runs on server
            if (!Mirror.NetworkServer.active)
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlaying)
                {
#endif
                    Destroy(this);
                    return;
#if UNITY_EDITOR
                }
#endif
            }
            
            tileManager = FindObjectOfType<TileManager>();
            atmosTiles = new List<AtmosObject>();
            pipeTiles = new List<PipeObject>();
            deviceTiles = new List<IAtmosLoop>();
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
            int pipesInstantiated = 0;
            int devicesInstantiated = 0;

            foreach (TileObject tile in tileObjects)
            {
                tile.atmos = ScriptableObject.CreateInstance<AtmosObject>();
                tile.atmos.MakeEmpty();
                tile.atmos.MakeAir();
                tile.atmos.RemoveFlux();

                // Set walls blocked
                if (tile.Tile.turf)
                {
                    if (tile.Tile.turf.isWall)
                    {
                        tile.atmos.SetBlocked(true);
                    }
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


                // Pipe init
                PipeObject[] pipes = tile.GetComponentsInChildren<PipeObject>();
                foreach (PipeObject pipe in pipes)
                {
                    if (pipe != null)
                    {
                        pipe.SetTileNeighbour(tileNeighbour, 0);
                        pipe.SetTileNeighbour(tileNeighbour2, 1);
                        pipe.SetTileNeighbour(tileNeighbour3, 2);
                        pipe.SetTileNeighbour(tileNeighbour4, 3);
                        pipeTiles.Add(pipe);
                        pipesInstantiated++;
                    }
                }

                // Do pumps
                IAtmosLoop device = tile.GetComponentInChildren<IAtmosLoop>();
                if (device != null)
                {
                    device.SetTileNeighbour(tileNeighbour, 0);
                    device.SetTileNeighbour(tileNeighbour2, 1);
                    device.SetTileNeighbour(tileNeighbour3, 2);
                    device.SetTileNeighbour(tileNeighbour4, 3);
                    deviceTiles.Add(device);
                    devicesInstantiated++;
                }

                tilesInstantiated++;
            }

            // Set neighbouring atmos after all are created
            foreach (TileObject tile in tileObjects)
            {
                // Atmos tiles and pipes
                tile.atmos.setAtmosNeighbours();
                PipeObject[] pipes = tile.GetComponentsInChildren<PipeObject>();
                foreach (PipeObject pipe in pipes)
                {
                    if (pipe)
                        pipe.SetAtmosNeighbours();
                }

                IAtmosLoop device = tile.GetComponentInChildren<IAtmosLoop>();
                if (device != null)
                    device.Initialize();

                // tile.atmos.ValidateVacuum();

                // Set airlocks to blocked
                if (tile.Tile.fixtures != null)
                {
                    Fixture fixture = tile.Tile.fixtures.GetFloorFixtureAtLayer(FloorFixtureLayers.FurnitureFixtureMain);
                    if (fixture)
                    {
                        if (fixture.name.Contains("Airlock"))
                        {
                            tile.atmos.SetBlocked(true);
                        }
                    }
                }
            }
            Debug.Log($"AtmosManager: Finished initializing {tilesInstantiated} tiles, {pipesInstantiated} pipes and {devicesInstantiated} devices");

            lastStep = Time.fixedTime;
            s_PreparePerfMarker.End();
        }

        void Update()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
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
                            if (drawTiles)
                            {
                                if (isAddingGas)
                                {
                                    tile.atmos.AddGas(gasToAdd, 60f);
                                }
                                else
                                {
                                    Debug.Log("Tile, Pressure (kPa): " + tile.atmos.GetPressure() + " Temperature (K): " + tile.atmos.GetAtmosContainer().GetTemperature() + " State: " + tile.atmos.GetState().ToString() + "\t" +
                                        " Oxygen content: " + tile.atmos.GetAtmosContainer().GetGasses()[0] +
                                        " Nitrogen content: " + tile.atmos.GetAtmosContainer().GetGasses()[1] +
                                        " Carbon Dioxide content: " + tile.atmos.GetAtmosContainer().GetGasses()[2] +
                                        " Plasma content: " + tile.atmos.GetAtmosContainer().GetGasses()[3]);
                                    lastClick = Time.fixedTime;
                                }
                            }
                            else if (showPipes)
                            {
                                PipeObject[] pipes = tile.GetComponentsInChildren<PipeObject>();
                                bool pipeLayerFound = false;
                                foreach (PipeObject pipe in pipes)
                                {
                                    if (pipe && pipe.layer == selectedPipeLayer)
                                    {
                                        pipeLayerFound = true;
                                        if (isAddingGas)
                                        {
                                            pipe.AddGas(gasToAdd, 30f);
                                        }
                                        else
                                        {
                                            Debug.Log("Pipe, Pressure (kPa): " + pipe.GetPressure() + " Temperature (K): " + pipe.GetAtmosContainer().GetTemperature() + " State: " + pipe.GetState().ToString() + "\t" +
                                                " Oxygen content: " + pipe.GetAtmosContainer().GetGasses()[0] +
                                                " Nitrogen content: " + pipe.GetAtmosContainer().GetGasses()[1] +
                                                " Carbon Dioxide content: " + pipe.GetAtmosContainer().GetGasses()[2] +
                                                " Plasma content: " + pipe.GetAtmosContainer().GetGasses()[3]);
                                            lastClick = Time.fixedTime;
                                        }
                                    }
                                }

                                if (!pipeLayerFound)
                                {
                                    Debug.Log("No pipe found on the clicked tile for layer " + selectedPipeLayer.ToString());
                                    lastClick = Time.fixedTime;
                                }
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
            bool overPressureEvent = false;

            // Step 1: Calculate flux
            foreach (AtmosObject tile in atmosTiles)
            {
                if (tile.GetState() == AtmosStates.Active)
                {
                    tile.CalculateFlux();
                }
            }

            // Step 2: Simulate
            foreach (TileObject tile in tileObjects)
            {
                AtmosStates state = tile.atmos.GetState();
                switch (state)
                {
                    case AtmosStates.Active:
                        tile.atmos.SimulateFlux();
                        activeTiles++;
                        break;
                    case AtmosStates.Semiactive:
                        tile.atmos.SimulateFlux();
                        break;
                }

                // Step 3: Move items according to the wind velocity
                Vector2 velocity = tile.atmos.GetVelocity();
                if (velocity != Vector2.zero)
                {
                    MoveVelocity(tile);
                }

                // Step 4: Destroy tiles with to much pressure
                if (tile.atmos.CheckOverPressure() && !overPressureEvent)
                {
                    TileDefinition oldDefinition = tile.Tile;
                    if (oldDefinition.turf?.isWall == true)
                    {
                        oldDefinition.turf = floor;
                        tileManager.UpdateTile(tile.transform.position, oldDefinition);
                        tile.atmos.SetBlocked(false);
                        overPressureEvent = true;
                    }
                }
            }

            // Step 5: Do pumps and pipes as well
            StepDevices();
            StepPipe();
            

            s_StepPerfMarker.End();
            return activeTiles;
        }

        private void StepDevices()
        {
            foreach (IAtmosLoop device in deviceTiles)
            {
                device.Step();
            }
        }



        private int StepPipe()
        {
            int activePipes = 0;
            bool overPressureEvent = false;

            foreach (PipeObject pipe in pipeTiles)
            {
                if (pipe.GetState() == AtmosStates.Active)
                {
                    pipe.CalculateFlux();
                }
            }

            foreach (PipeObject pipe in pipeTiles)
            {
                AtmosStates state = pipe.GetState();
                switch (state)
                {
                    case AtmosStates.Active:
                        pipe.SimulateFlux();
                        activeTiles++;
                        break;
                    case AtmosStates.Semiactive:
                        pipe.SimulateFlux();
                        break;
                }

                // Check for pipe overpressure
                if (pipe.CheckOverPressure() && !overPressureEvent)
                {
                    // TODO
                }
            }

            return activePipes;
        }

        private void MoveVelocity(TileObject tileObject)
        {
            Vector2 velocity = tileObject.atmos.GetVelocity();
             if (Mathf.Abs(velocity.x) > Gas.minimumWind || Mathf.Abs(velocity.y) > Gas.minimumWind)
            {
                velocity *= Gas.windFactor;
                Collider[] colliders = Physics.OverlapBox(tileObject.transform.position, new Vector3(1, 2.5f, 1));

                foreach (Collider collider in colliders)
                {
                    Rigidbody rigidbody = collider.attachedRigidbody;
                    if (rigidbody != null)
                    {
                        rigidbody.AddForce(new Vector3(velocity.x, 0, velocity.y));
                    }
                }
            }
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
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
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

                    if (drawTiles)
                    {
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
                                        float moles = tile.atmos.GetAtmosContainer().GetGasses()[k] / 30f;

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
                                    float temperatue = tile.atmos.GetAtmosContainer().GetTemperature() / 100f;

                                    Gizmos.color = Color.red - state;
                                    Gizmos.DrawCube(new Vector3(x, temperatue / 2f, y), new Vector3(1 * drawSize, temperatue, 1 * drawSize));
                                    break;
                                case ViewType.Combined:
                                    pressure = tile.atmos.GetPressure() / 30f;

                                    Gizmos.color = new Color(tile.atmos.GetAtmosContainer().GetTemperature() / 500f, 0, 0, 1) - state;
                                    Gizmos.DrawCube(new Vector3(x, pressure / 2f, y), new Vector3(1 * drawSize, pressure, 1 * drawSize));
                                    break;
                                case ViewType.Wind:
                                    Gizmos.color = Color.white;
                                    Gizmos.DrawLine(new Vector3(x, 0, y), new Vector3(x + Mathf.Clamp(tile.atmos.GetVelocity().x, -1, 1), 0, y + Mathf.Clamp(tile.atmos.GetVelocity().y, -1, 1)));
                                    break;
                            }
                        }
                    }

                    drawSize = 1f;

                    // Draw pipe contents
                    if (showPipes)
                    {
                        PipeObject[] pipes = tile.GetComponentsInChildren<PipeObject>();
                        foreach (PipeObject pipe in pipes)
                        {
                            if (!showOnlySelectedPipes || (showOnlySelectedPipes && pipe.layer == selectedPipeLayer))
                            {
                                float rotation = 0f;
                                OffsetPipesAdjacencyConnector offsetConnector = pipe.GetComponent<OffsetPipesAdjacencyConnector>();
                                OffsetPipesAdjacencyConnector.PipeOrientation pipeOrientation = OffsetPipesAdjacencyConnector.PipeOrientation.o;
                                if (offsetConnector)
                                {
                                    pipeOrientation = offsetConnector.GetPipeOrientation();
                                    rotation = offsetConnector.GetRotation();
                                }

                                switch (pipe.GetState())
                                {
                                    case AtmosStates.Active: state = new Color(0, 0, 0, 0); break;
                                    case AtmosStates.Semiactive: state = new Color(0, 0, 0, 0.8f); break;
                                    case AtmosStates.Inactive: state = new Color(0, 0, 0, 0.8f); break;
                                    default: state = new Color(0, 0, 0, 1); break;
                                }

                                switch (drawView)
                                {
                                    case ViewType.Content:
                                        float[] gases = new float[5];

                                        Color[] colors = new Color[] { Color.yellow, Color.white, Color.gray, Color.magenta };
                                        float offset = 0f;

                                        for (int k = 0; k < 4; ++k)
                                        {
                                            float moles = pipe.GetAtmosContainer().GetGasses()[k] / 30f;

                                            if (moles != 0f)
                                            {
                                                Gizmos.color = colors[k] - state;
                                                if (drawAll || k == 3) // Only draw plasma
                                                {
                                                    DrawPipeCube(x, y, pipe.layer, moles / 2f + offset, pipeOrientation, drawSize, rotation);
                                                    offset += moles;
                                                }
                                            }
                                        }
                                        break;
                                    case ViewType.Pressure:
                                        pressure = pipe.GetPressure() / 160f;

                                        if (drawAll || pipe.GetState() == AtmosStates.Active)
                                        {
                                            Gizmos.color = Color.white - state;
                                            DrawPipeCube(x, y, pipe.layer, pressure, pipeOrientation, drawSize, rotation);
                                        }
                                        break;
                                    case ViewType.Temperature:
                                        float temperatue = pipe.GetAtmosContainer().GetTemperature() / 100f;

                                        Gizmos.color = Color.red - state;
                                        DrawPipeCube(x, y, pipe.layer, temperatue / 2f, pipeOrientation, drawSize, rotation);
                                        break;
                                    case ViewType.Combined:
                                        pressure = pipe.GetPressure() / 30f;

                                        Gizmos.color = new Color(pipe.GetAtmosContainer().GetTemperature() / 500f, 0, 0, 1) - state;
                                        DrawPipeCube(x, y, pipe.layer, pressure, pipeOrientation, drawSize, rotation);
                                        break;
                                }
                            }
                        }
                    }
                }
                
            }
        }

        private void DrawPipeCube(float x, float y, PipeLayer layer, float value, OffsetPipesAdjacencyConnector.PipeOrientation orientation, float drawSize, float rotation)
        {
            float offsetX = 0f;
            float offsetY = 0f;

            switch (layer)
            {
                case PipeLayer.L1:
                    offsetX = -0.25f;
                    offsetY = 0.25f;
                    break;
                case PipeLayer.L3:
                    offsetX = 0.25f;
                    offsetY = -0.25f;
                    break;
            }

            switch (orientation)
            {
                case OffsetPipesAdjacencyConnector.PipeOrientation.o:
                    if (layer == PipeLayer.L2 || layer == PipeLayer.Upper)
                    Gizmos.DrawCube(new Vector3(x, value / 2f, y), new Vector3(0.2f * drawSize, value, 0.2f * drawSize));
                    break;

                case OffsetPipesAdjacencyConnector.PipeOrientation.i:
                    if (rotation > 0)
                        Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                    else
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                    break;

                case OffsetPipesAdjacencyConnector.PipeOrientation.cNorth:
                    if (rotation > 0)
                        Gizmos.DrawCube(new Vector3(x + 0.25f, value / 2f, y + offsetY), new Vector3(0.5f * drawSize, value, 0.2f * drawSize));
                    else
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y + 0.25f), new Vector3(0.2f * drawSize, value, 0.5f * drawSize));

                    break;
                case OffsetPipesAdjacencyConnector.PipeOrientation.cSouth:
                    if (rotation > 0)
                        Gizmos.DrawCube(new Vector3(x - 0.25f, value / 2f, y + offsetY), new Vector3(0.5f * drawSize, value, 0.2f * drawSize));
                    else
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y - 0.25f), new Vector3(0.2f * drawSize, value, 0.5f * drawSize));
                    break;

                case OffsetPipesAdjacencyConnector.PipeOrientation.tNEW:
                    if (layer == PipeLayer.L3)
                    {
                        Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                    }
                    else if (layer == PipeLayer.L1)
                    {
                        Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y + 0.33f), new Vector3(0.2f * drawSize, value, 0.35f * drawSize));
                    }
                    break;

                case OffsetPipesAdjacencyConnector.PipeOrientation.tNSE:
                    if (layer == PipeLayer.L3)
                    {
                        Gizmos.DrawCube(new Vector3(x + 0.33f, value / 2f, y + offsetY), new Vector3(0.35f * drawSize, value, 0.2f * drawSize));
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                    }
                    else if (layer == PipeLayer.L1)
                    {
                        Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                    }
                    break;

                case OffsetPipesAdjacencyConnector.PipeOrientation.tNSW:
                    if (layer == PipeLayer.L3)
                    {
                        Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                    }
                    else if (layer == PipeLayer.L1)
                    {
                        Gizmos.DrawCube(new Vector3(x - 0.33f, value / 2f, y + offsetY), new Vector3(0.35f * drawSize, value, 0.2f * drawSize));
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                    }
                    break;

                case OffsetPipesAdjacencyConnector.PipeOrientation.tSWE:
                    if (layer == PipeLayer.L3)
                    {
                        Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                    }
                    else if (layer == PipeLayer.L1)
                    {
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                        Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                    }
                    break;

                case OffsetPipesAdjacencyConnector.PipeOrientation.lNE:
                    if (layer == PipeLayer.L1)
                    {
                        Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                    }
                    else if (layer == PipeLayer.L3)
                    {
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                    }
                    break;

                case OffsetPipesAdjacencyConnector.PipeOrientation.lNW:
                    if (layer == PipeLayer.L1)
                    {
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y + offsetY), new Vector3(0.35f * drawSize, value, 0.2f * drawSize));
                    }
                    else if (layer == PipeLayer.L3)
                    {
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                        Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                    }
                    break;

                case OffsetPipesAdjacencyConnector.PipeOrientation.lSE:
                    if (layer == PipeLayer.L3)
                    {
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y + offsetY), new Vector3(0.35f * drawSize, value, 0.2f * drawSize));
                    }
                    else if (layer == PipeLayer.L1)
                    {
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                        Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                    }
                    break;

                case OffsetPipesAdjacencyConnector.PipeOrientation.lSW:
                    if (layer == PipeLayer.L3)
                    {
                        Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                    }
                    else if (layer == PipeLayer.L1)
                    {
                        Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                    }
                    break;

                case OffsetPipesAdjacencyConnector.PipeOrientation.x:
                    Gizmos.DrawCube(new Vector3(x + offsetX, value / 2f, y), new Vector3(0.2f * drawSize, value, drawSize));
                    Gizmos.DrawCube(new Vector3(x, value / 2f, y + offsetY), new Vector3(drawSize, value, 0.2f * drawSize));
                    break;
            }

        }
    }
}
