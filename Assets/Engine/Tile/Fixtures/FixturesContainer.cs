using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace SS3D.Engine.Tiles
{
    // Class that holds all fixtures that can be placed on a tile
    [Serializable]
    public class FixturesContainer : ICloneable
    {
        public TileFixtureDefinition tileFixtureDefinition;
        public WallFixtureDefinition wallFixtureDefinition;
        public FloorFixtureDefinition floorFixtureDefinition;

        public TileFixture GetTileFixtureAtLayer(TileFixtureLayers layer)
        {
            switch (layer)
            {
                case TileFixtureLayers.Disposal:
                    return tileFixtureDefinition.disposal;
                case TileFixtureLayers.Pipe1:
                    return tileFixtureDefinition.pipe1;
                case TileFixtureLayers.Pipe2:
                    return tileFixtureDefinition.pipe2;
                case TileFixtureLayers.Pipe3:
                    return tileFixtureDefinition.pipe3;
                case TileFixtureLayers.Wire:
                    return tileFixtureDefinition.wire;
            }
            return null;
        }

        public WallFixture GetWallFixtureAtLayer(WallFixtureLayers layer)
        {
            switch (layer)
            {
                case WallFixtureLayers.HighWallNorth:
                    return wallFixtureDefinition.highWallNorth;
                case WallFixtureLayers.HighWallEast:
                    return wallFixtureDefinition.highWallEast;
                case WallFixtureLayers.HighWallSouth:
                    return wallFixtureDefinition.highWallSouth;
                case WallFixtureLayers.HighWallWest:
                    return wallFixtureDefinition.highWallWest;

                case WallFixtureLayers.LowWallNorth:
                    return wallFixtureDefinition.lowWallNorth;
                case WallFixtureLayers.LowWallEast:
                    return wallFixtureDefinition.lowWallEast;
                case WallFixtureLayers.LowWallSouth:
                    return wallFixtureDefinition.lowWallSouth;
                case WallFixtureLayers.LowWallWest:
                    return wallFixtureDefinition.lowWallWest;

            }
            return null;
        }

        public FloorFixture GetFloorFixtureAtLayer(FloorFixtureLayers layer)
        {
            switch (layer)
            {
                case FloorFixtureLayers.FurnitureFixtureMain:
                    return floorFixtureDefinition.furnitureMain;
                case FloorFixtureLayers.FurnitureFixture2:
                    return floorFixtureDefinition.furniture2;
                case FloorFixtureLayers.FurnitureFixture3:
                    return floorFixtureDefinition.furniture3;
                case FloorFixtureLayers.FurnitureFixture4:
                    return floorFixtureDefinition.furniture4;
                case FloorFixtureLayers.FurnitureFixture5:
                    return floorFixtureDefinition.furniture5;

                case FloorFixtureLayers.OverlayFixture1:
                    return floorFixtureDefinition.overlay1;
                case FloorFixtureLayers.OverlayFixture2:
                    return floorFixtureDefinition.overlay2;
                case FloorFixtureLayers.OverlayFixture3:
                    return floorFixtureDefinition.overlay3;

                case FloorFixtureLayers.PipeUpperFixture:
                    return floorFixtureDefinition.pipeUpper;
            }
            return null;
        }

        public void SetTileFixtureAtLayer(TileFixture fixture, TileFixtureLayers layer)
        {
            switch (layer)
            {
                case TileFixtureLayers.Disposal:
                    tileFixtureDefinition.disposal = (DisposalFixture)fixture;
                    break;
                case TileFixtureLayers.Pipe1:
                    tileFixtureDefinition.pipe1 = (PipeFixture)fixture;
                    break;
                case TileFixtureLayers.Pipe2:
                    tileFixtureDefinition.pipe2 = (PipeFixture)fixture;
                    break;
                case TileFixtureLayers.Pipe3:
                    tileFixtureDefinition.pipe3 = (PipeFixture)fixture;
                    break;
                case TileFixtureLayers.Wire:
                    tileFixtureDefinition.wire = (WireFixture)fixture;
                    break;
            }
        }

        public void SetWallFixtureAtLayer(WallFixture fixture, WallFixtureLayers layer)
        {
            switch (layer)
            {
                case WallFixtureLayers.HighWallNorth:
                    wallFixtureDefinition.highWallNorth = (HighWallFixture)fixture;
                    wallFixtureDefinition.highWallNorth?.SetOrientation(WallFixture.Orientation.North);
                    break;
                case WallFixtureLayers.HighWallEast:
                    wallFixtureDefinition.highWallEast = (HighWallFixture)fixture;
                    wallFixtureDefinition.highWallEast?.SetOrientation(WallFixture.Orientation.East);
                    break;
                case WallFixtureLayers.HighWallSouth:
                    wallFixtureDefinition.highWallSouth = (HighWallFixture)fixture;
                    wallFixtureDefinition.highWallSouth?.SetOrientation(WallFixture.Orientation.South);
                    break;
                case WallFixtureLayers.HighWallWest:
                    wallFixtureDefinition.highWallWest = (HighWallFixture)fixture;
                    wallFixtureDefinition.highWallWest?.SetOrientation(WallFixture.Orientation.West);
                    break;

                case WallFixtureLayers.LowWallNorth:
                    wallFixtureDefinition.lowWallNorth = (LowWallFixture)fixture;
                    wallFixtureDefinition.lowWallNorth?.SetOrientation(WallFixture.Orientation.North);
                    break;
                case WallFixtureLayers.LowWallEast:
                    wallFixtureDefinition.lowWallEast = (LowWallFixture)fixture;
                    wallFixtureDefinition.lowWallEast?.SetOrientation(WallFixture.Orientation.East);
                    break;
                case WallFixtureLayers.LowWallSouth:
                    wallFixtureDefinition.lowWallSouth = (LowWallFixture)fixture;
                    wallFixtureDefinition.lowWallSouth?.SetOrientation(WallFixture.Orientation.South);
                    break;
                case WallFixtureLayers.LowWallWest:
                    wallFixtureDefinition.lowWallWest = (LowWallFixture)fixture;
                    wallFixtureDefinition.lowWallWest?.SetOrientation(WallFixture.Orientation.West);
                    break;

            }
        }

        public void SetFloorFixtureAtLayer(FloorFixture fixture, FloorFixtureLayers layer)
        {
            switch (layer)
            {
                case FloorFixtureLayers.FurnitureFixtureMain:
                    floorFixtureDefinition.furnitureMain = (FurnitureFloorFixture)fixture;
                    break;
                case FloorFixtureLayers.FurnitureFixture2:
                    floorFixtureDefinition.furniture2 = (FurnitureFloorFixture)fixture;
                    break;
                case FloorFixtureLayers.FurnitureFixture3:
                    floorFixtureDefinition.furniture3 = (FurnitureFloorFixture)fixture;
                    break;
                case FloorFixtureLayers.FurnitureFixture4:
                    floorFixtureDefinition.furniture4 = (FurnitureFloorFixture)fixture;
                    break;
                case FloorFixtureLayers.FurnitureFixture5:
                    floorFixtureDefinition.furniture5 = (FurnitureFloorFixture)fixture;
                    break;

                case FloorFixtureLayers.OverlayFixture1:
                    floorFixtureDefinition.overlay1 = (OverlayFloorFixture)fixture;
                    break;
                case FloorFixtureLayers.OverlayFixture2:
                    floorFixtureDefinition.overlay2 = (OverlayFloorFixture)fixture;
                    break;
                case FloorFixtureLayers.OverlayFixture3:
                    floorFixtureDefinition.overlay3 = (OverlayFloorFixture)fixture;
                    break;

                case FloorFixtureLayers.PipeUpperFixture:
                    floorFixtureDefinition.pipeUpper = (PipeFloorFixture)fixture;
                    break;
            }
        }

        public Fixture[] GetAllFixtures()
        {
            List<Fixture> fixtures = new List<Fixture>();

            foreach (TileFixtureLayers layer in TileDefinition.GetTileFixtureLayerNames())
            {
                fixtures.Add(GetTileFixtureAtLayer(layer));
            }

            foreach (WallFixtureLayers layer in TileDefinition.GetWallFixtureLayerNames())
            {
                fixtures.Add(GetWallFixtureAtLayer(layer));
            }

            foreach (FloorFixtureLayers layer in TileDefinition.GetFloorFixtureLayerNames())
            {
                fixtures.Add(GetFloorFixtureAtLayer(layer));
            }

            return fixtures.ToArray();
        }

        public Fixture GetFixtureAtLayerIndex(int index)
        {
            int offsetFloor = TileDefinition.GetTileFixtureLayerSize();
            int offsetWall = TileDefinition.GetWallFixtureLayerSize();
            int offsetTotal = offsetFloor + offsetWall + TileDefinition.GetFloorFixtureLayerSize();

            if (index < offsetFloor)
            {
                // We are a Tile fixture
                return GetTileFixtureAtLayer((TileFixtureLayers)index);
            }

            else if (index >= offsetFloor && index < offsetWall)
            {
                // We are a Wall fixture
                return GetWallFixtureAtLayer((WallFixtureLayers)(index - offsetFloor));
            }

            else if (index >= offsetWall && index < offsetTotal)
            {
                // We are a Floor fixture
                return GetFloorFixtureAtLayer((FloorFixtureLayers)(index - offsetFloor - offsetWall));
            }

            else
            {
                Debug.LogError("Requesting out of index Fixture");
            }

            return null;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        // Remove the selection if the option is impossible
        // For example: tables cannot be build in walls, or wall fixtures cannot be build on floors
        public static TileDefinition ValidateFixtures(TileDefinition tileDefinition)
        {
            bool altered = false;
            string reason = "";

            // If lattice, remove tile fixtures
            if (tileDefinition.plenum.name.Contains("Lattice"))
            {
                foreach (TileFixtureLayers layer in TileDefinition.GetTileFixtureLayerNames())
                {
                    if (tileDefinition.fixtures.GetTileFixtureAtLayer(layer) != null)
                    {
                        altered = true;
                        tileDefinition.fixtures.SetTileFixtureAtLayer(null, layer);
                    }
                }
                if (altered)
                    reason += "Lattices do not support any wall/floor fixture.\n";
            }

            // If catwalk
            if (tileDefinition.plenum.name.Contains("Catwalk"))
            {
                // Allow only the wire layer in tile fixtures
                foreach (TileFixtureLayers layer in TileDefinition.GetTileFixtureLayerNames())
                {
                    if (layer != TileFixtureLayers.Wire)
                    {
                        if (tileDefinition.fixtures.GetTileFixtureAtLayer(layer) != null)
                        {
                            altered = true;
                            reason += "Catwalk only supports a wire and upper pipe layer.\n";
                            tileDefinition.fixtures.SetTileFixtureAtLayer(null, layer);
                        }
                    }
                }
            }

            if ((tileDefinition.turf != null && tileDefinition.turf.isWall) || tileDefinition.turf == null || tileDefinition.plenum.name.Contains("Lattice"))
            {
                // Remove floor fixtures
                foreach (FloorFixtureLayers layer in TileDefinition.GetFloorFixtureLayerNames())
                {
                    // Allow upper pipe layer with a catwalk plenum
                    if (tileDefinition.plenum.name.Contains("Catwalk") && layer == FloorFixtureLayers.PipeUpperFixture)
                        continue;

                    if (tileDefinition.fixtures.GetFloorFixtureAtLayer(layer) != null)
                    {
                        altered = true;
                        reason += "Cannot set a floor fixture when there is no floor.\n";
                        Debug.Log("Cannot set a floor fixture when there is no floor");
                    }

                    tileDefinition.fixtures.SetFloorFixtureAtLayer(null, layer);
                }
            }

            if ((tileDefinition.turf != null && !tileDefinition.turf.isWall) || tileDefinition.turf == null || tileDefinition.plenum.name.Contains("Lattice"))
            {
                // Remove wall fixtures
                foreach (WallFixtureLayers layer in TileDefinition.GetWallFixtureLayerNames())
                {
                    if (tileDefinition.fixtures.GetWallFixtureAtLayer(layer) != null)
                    {
                        altered = true;
                        reason += "Cannot set a wall fixture when there is no wall.\n";
                        Debug.Log("Cannot set a wall fixture when there is no wall");
                    }

                    tileDefinition.fixtures.SetWallFixtureAtLayer(null, layer);
                }
            }

            // Prevent low wall mounts on glass walls and reinforced glass walls
            if (tileDefinition.turf != null && tileDefinition.turf.isWall && tileDefinition.turf.name.Contains("GlassWall"))
            {
                foreach (WallFixtureLayers layer in TileDefinition.GetWallFixtureLayerNames())
                {
                    if (layer == WallFixtureLayers.LowWallNorth || layer == WallFixtureLayers.LowWallEast || layer == WallFixtureLayers.LowWallSouth || layer == WallFixtureLayers.LowWallWest)
                    {
                        if (tileDefinition.fixtures.GetWallFixtureAtLayer(layer) != null)
                        {
                            altered = true;
                            reason += "Glass walls do not allow low wall fixtures.\n";
                            tileDefinition.fixtures.SetWallFixtureAtLayer(null, layer);
                        }
                    }
                }
            }

            // Restrict pipes to their own layer
            TileFixture pipe = tileDefinition.fixtures.GetTileFixtureAtLayer(TileFixtureLayers.Pipe1);
            if (pipe != null && !pipe.name.Contains("1"))
            {
                altered = true;
                tileDefinition.fixtures.SetTileFixtureAtLayer(null, TileFixtureLayers.Pipe1);
            }

            pipe = tileDefinition.fixtures.GetTileFixtureAtLayer(TileFixtureLayers.Pipe2);
            if (pipe != null && !pipe.name.Contains("2"))
            {
                altered = true;
                tileDefinition.fixtures.SetTileFixtureAtLayer(null, TileFixtureLayers.Pipe2);
            }

            pipe = tileDefinition.fixtures.GetTileFixtureAtLayer(TileFixtureLayers.Pipe3);
            if (pipe != null && !pipe.name.Contains("3"))
            {
                altered = true;
                tileDefinition.fixtures.SetTileFixtureAtLayer(null, TileFixtureLayers.Pipe3);
            }

#if UNITY_EDITOR
            if (altered)
            {
                EditorUtility.DisplayDialog("Fixture combination", "Invalid because of the following: \n\n" +
                    reason +
                    "\n" +
                    "Definition has been reset.", "ok");
            }
#endif

            return tileDefinition;
        }
    }
}