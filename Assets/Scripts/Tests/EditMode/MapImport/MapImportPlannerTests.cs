using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapImport;
using SS3D.Systems.Tile.MapImport.Dmm;
using UnityEngine;

namespace SS3D.Tests.EditMode.MapImport
{
    public class MapImportPlannerTests
    {
        private static string FixturePath =>
            Path.GetFullPath(Path.Combine(Application.dataPath,
                "Scripts/Tests/EditMode/MapImport/Fixtures/tiny_box.dmm"));

        private static string InfraFixturePath =>
            Path.GetFullPath(Path.Combine(Application.dataPath,
                "Scripts/Tests/EditMode/MapImport/Fixtures/tiny_infra.dmm"));

        private static string DoorRunFixturePath =>
            Path.GetFullPath(Path.Combine(Application.dataPath,
                "Scripts/Tests/EditMode/MapImport/Fixtures/tiny_door_run.dmm"));

        private static string FurnitureFixturePath =>
            Path.GetFullPath(Path.Combine(Application.dataPath,
                "Scripts/Tests/EditMode/MapImport/Fixtures/tiny_furniture.dmm"));

        private static string TypeMapPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../Tools/map_import/ss13_type_map.yaml"));

        [Test]
        public void TypeMapYaml_ParsesDefaultsAndSortedPrefixes()
        {
            Ss13TypeMapConfig config = Ss13TypeMapYaml.ParseFile(TypeMapPath);
            Assert.AreEqual("TileGrey", config.DefaultFloor);
            Assert.Greater(config.Prefixes.Count, 5);
            Assert.GreaterOrEqual(config.Prefixes[0].Match.Length, config.Prefixes[^1].Match.Length);
            Assert.IsTrue(config.Prefixes.Any(p => p.Kind == MapImportKind.Cable));
            Assert.IsTrue(config.Prefixes.Any(p => p.Kind == MapImportKind.Pipe));
            Assert.IsTrue(config.Prefixes.Any(p => p.Kind == MapImportKind.Apc));
            Assert.IsTrue(config.Prefixes.Any(p => p.Kind == MapImportKind.Lattice));
            Assert.IsTrue(config.Prefixes.Any(p => p.Kind == MapImportKind.Table));
            Assert.IsTrue(config.Prefixes.Any(p => p.Kind == MapImportKind.Smes));
        }

        [Test]
        public void Mapper_LongestPrefixWins()
        {
            Ss13TypeMapConfig config = Ss13TypeMapYaml.ParseFile(TypeMapPath);
            Ss13TypeMapper mapper = new Ss13TypeMapper(config);
            Assert.IsTrue(mapper.TryMatch("/turf/closed/wall/r_wall", out Ss13TypeMatch match));
            Assert.AreEqual(MapImportKind.Wall, match.Kind);
            Assert.AreEqual("SteelWallReinforced", match.SoName);
        }

        [Test]
        public void Mapper_InfrastructurePrefixes()
        {
            Ss13TypeMapConfig config = Ss13TypeMapYaml.ParseFile(TypeMapPath);
            Ss13TypeMapper mapper = new Ss13TypeMapper(config);

            Assert.IsTrue(mapper.TryMatch("/obj/structure/cable", out Ss13TypeMatch cable));
            Assert.AreEqual(MapImportKind.Cable, cable.Kind);
            Assert.AreEqual("Cables", cable.SoName);

            Assert.IsTrue(mapper.TryMatch("/obj/machinery/light/small", out Ss13TypeMatch bulb));
            Assert.AreEqual("LightBulbFixture", bulb.SoName);

            Assert.IsTrue(mapper.TryMatch("/obj/machinery/light", out Ss13TypeMatch tube));
            Assert.AreEqual("LightTubeFixture", tube.SoName);

            Assert.IsTrue(mapper.TryMatch("/obj/structure/lattice/catwalk", out Ss13TypeMatch catwalk));
            Assert.AreEqual(MapImportKind.Lattice, catwalk.Kind);
            Assert.AreEqual("Catwalk", catwalk.SoName);

            Assert.IsTrue(mapper.TryMatch("/obj/structure/lattice", out Ss13TypeMatch lattice));
            Assert.AreEqual("Lattice", lattice.SoName);

            Assert.IsTrue(mapper.TryMatch("/obj/structure/table/wood", out Ss13TypeMatch wood));
            Assert.AreEqual(MapImportKind.Table, wood.Kind);
            Assert.AreEqual("TableWood", wood.SoName);

            Assert.IsTrue(mapper.TryMatch("/obj/structure/table", out Ss13TypeMatch steel));
            Assert.AreEqual("TableSteel", steel.SoName);

            Assert.IsTrue(mapper.TryMatch("/obj/machinery/power/smes", out Ss13TypeMatch smes));
            Assert.AreEqual(MapImportKind.Smes, smes.Kind);
            Assert.AreEqual("SMES", smes.SoName);
        }

        [Test]
        public void PipeResolver_NetworkTypeBeatsPipingLayer()
        {
            var atom = new DmmAtom { Path = "/obj/machinery/atmospherics/pipe/smart/manifold4w/supply" };
            atom.Vars["piping_layer"] = "3";
            Assert.AreEqual(MapImportPipeResolver.AtmosPipesSupply, MapImportPipeResolver.Resolve(atom.Path, atom));

            atom = new DmmAtom { Path = "/obj/machinery/atmospherics/pipe/smart/manifold4w/scrubbers" };
            Assert.AreEqual(MapImportPipeResolver.AtmosPipesMixed, MapImportPipeResolver.Resolve(atom.Path, atom));

            atom = new DmmAtom { Path = "/obj/machinery/atmospherics/pipe/smart/pipe" };
            atom.Vars["piping_layer"] = "4";
            Assert.AreEqual(MapImportPipeResolver.AtmosPipesSurface, MapImportPipeResolver.Resolve(atom.Path, atom));
        }

        [Test]
        public void Planner_TinyBox_AddsPlenumAndCountsKinds()
        {
            DmmMap map = DmmParser.ParseFile(FixturePath);
            Ss13TypeMapper mapper = new Ss13TypeMapper(Ss13TypeMapYaml.ParseFile(TypeMapPath));
            MapImportPlan plan = MapImportPlanner.Build(map, mapper);

            Assert.Greater(plan.FloorCells, 0);
            Assert.Greater(plan.WallCells, 0);
            Assert.Greater(plan.DoorCells, 0);
            Assert.Greater(plan.WindowCells, 0);
            Assert.AreEqual(1, plan.TablePlacements);
            Assert.IsFalse(plan.UnmappedCounts.ContainsKey("/obj/structure/table"));

            foreach (MapImportCellPlan cell in plan.Cells)
            {
                Assert.AreEqual(MapImportPlanner.PlenumSoName, cell.Placements[0].SoName);
                Assert.GreaterOrEqual(cell.Placements.Count, 2);
            }

            MapImportCellPlan doorCell = plan.Cells.Find(c => c.Placements[1].SoName == "CivillianAirlock");
            Assert.IsNotNull(doorCell);
            Assert.AreEqual(Direction.East, doorCell.Placements[1].Direction);

            MapImportCellPlan tableCell = plan.Cells.Find(c =>
                c.Placements.Any(p => p.SoName == "TableSteel"));
            Assert.IsNotNull(tableCell);
        }

        [Test]
        public void Direction_InferDoor_ScansThroughDoorRun()
        {
            // Horizontal ###AAA### — walls at E/W ends → South (N–S passage).
            var horizontal = new Dictionary<(int, int), MapImportKind>
            {
                [(0, 0)] = MapImportKind.Wall,
                [(1, 0)] = MapImportKind.Door,
                [(2, 0)] = MapImportKind.Door,
                [(3, 0)] = MapImportKind.Door,
                [(4, 0)] = MapImportKind.Wall,
            };
            Assert.AreEqual(Direction.South, MapImportDirection.InferDoorDirection(2, 0, horizontal));
            Assert.AreEqual(Direction.South, MapImportDirection.InferDoorDirection(1, 0, horizontal));

            // Vertical stack with walls at N/S ends → East (E–W passage).
            var vertical = new Dictionary<(int, int), MapImportKind>
            {
                [(0, 4)] = MapImportKind.Wall,
                [(0, 3)] = MapImportKind.Door,
                [(0, 2)] = MapImportKind.Door,
                [(0, 1)] = MapImportKind.Door,
                [(0, 0)] = MapImportKind.Wall,
            };
            Assert.AreEqual(Direction.East, MapImportDirection.InferDoorDirection(0, 2, vertical));
        }

        [Test]
        public void Planner_DoorRun_MiddleDoorsMatchEndOrientation()
        {
            DmmMap map = DmmParser.ParseFile(DoorRunFixturePath);
            Ss13TypeMapper mapper = new Ss13TypeMapper(Ss13TypeMapYaml.ParseFile(TypeMapPath));
            MapImportPlan plan = MapImportPlanner.Build(map, mapper);

            List<MapImportCellPlan> doors = plan.Cells
                .Where(c => c.Placements.Count >= 2 && c.Placements[1].SoName == "CivillianAirlock")
                .OrderBy(c => c.WorldX)
                .ToList();
            Assert.AreEqual(3, doors.Count);
            foreach (MapImportCellPlan door in doors)
                Assert.AreEqual(Direction.South, door.Placements[1].Direction);
        }

        [Test]
        public void Direction_FromDirectionalPathSuffix()
        {
            Assert.IsTrue(MapImportDirection.TryFromPath(
                "/obj/machinery/power/apc/auto_name/directional/east", out Direction dir));
            Assert.AreEqual(Direction.East, dir);

            var atom = new DmmAtom { Path = "/obj/machinery/light/directional/west" };
            Assert.IsTrue(MapImportDirection.TryResolveExplicit(atom, out Direction lightDir));
            Assert.AreEqual(Direction.West, lightDir);
        }

        [Test]
        public void Planner_TinyInfra_PlacesOverlaysAndResolvesPipes()
        {
            DmmMap map = DmmParser.ParseFile(InfraFixturePath);
            Ss13TypeMapper mapper = new Ss13TypeMapper(Ss13TypeMapYaml.ParseFile(TypeMapPath));
            MapImportPlan plan = MapImportPlanner.Build(map, mapper);

            Assert.AreEqual(1, plan.CablePlacements);
            Assert.GreaterOrEqual(plan.PipePlacements, 3); // supply + scrubbers + layer4
            Assert.AreEqual(1, plan.DisposalPlacements);
            Assert.AreEqual(1, plan.DisposalTerminalPlacements);
            Assert.AreEqual(1, plan.VentPlacements);
            Assert.AreEqual(1, plan.ApcPlacements);
            Assert.AreEqual(1, plan.LightPlacements);
            Assert.GreaterOrEqual(plan.DoorCells, 1);

            MapImportCellPlan cableCell = plan.Cells.Find(c =>
                c.Placements.Any(p => p.SoName == "Cables"));
            Assert.IsNotNull(cableCell);
            Assert.IsTrue(cableCell.Placements.Any(p => p.SoName == MapImportPipeResolver.AtmosPipesSupply));
            Assert.IsTrue(cableCell.Placements.Any(p => p.SoName == MapImportPipeResolver.AtmosPipesMixed));

            MapImportCellPlan disposalCell = plan.Cells.Find(c =>
                c.Placements.Any(p => p.SoName == "DisposalPipes"));
            Assert.IsNotNull(disposalCell);
            Assert.IsTrue(disposalCell.Placements.Any(p => p.SoName == "DisposalBin"));

            MapImportCellPlan ventCell = plan.Cells.Find(c =>
                c.Placements.Any(p => p.SoName == "Vent"));
            Assert.IsNotNull(ventCell);
            Assert.AreEqual(Direction.South,
                ventCell.Placements.Find(p => p.SoName == "Vent").Direction);

            // Floor cell with APC/light — mounts offset onto neighbouring wall tiles, facing into room.
            MapImportCellPlan wallMountFloor = plan.Cells.Find(c =>
                c.Placements.Any(p => p.SoName == "APC"));
            Assert.IsNotNull(wallMountFloor);
            MapImportPlacement apc = wallMountFloor.Placements.Find(p => p.SoName == "APC");
            Assert.IsTrue(apc.HasWorldOverride);
            Assert.AreEqual(wallMountFloor.WorldX + 1, apc.WorldX);
            Assert.AreEqual(wallMountFloor.WorldZ, apc.WorldZ);
            Assert.AreEqual(Direction.West, apc.Direction);

            MapImportPlacement light = wallMountFloor.Placements.Find(p => p.SoName == "LightTubeFixture");
            Assert.IsTrue(light.HasWorldOverride);
            Assert.AreEqual(wallMountFloor.WorldX - 1, light.WorldX);
            Assert.AreEqual(Direction.East, light.Direction);

            // Door between E/W walls on the same row, no dir → inferred South (N–S passage).
            MapImportCellPlan doorCell = plan.Cells.Find(c =>
                c.Placements.Count >= 2 && c.Placements[1].SoName == "CivillianAirlock");
            Assert.IsNotNull(doorCell);
            Assert.AreEqual(Direction.South, doorCell.Placements[1].Direction);

            MapImportCellPlan layer4Cell = plan.Cells.Find(c =>
                c.Placements.Any(p => p.SoName == MapImportPipeResolver.AtmosPipesSurface));
            Assert.IsNotNull(layer4Cell);
        }

        [Test]
        public void Planner_TinyFurniture_LatticeAsPlenumAndTablesSmes()
        {
            DmmMap map = DmmParser.ParseFile(FurnitureFixturePath);
            Ss13TypeMapper mapper = new Ss13TypeMapper(Ss13TypeMapYaml.ParseFile(TypeMapPath));
            MapImportPlan plan = MapImportPlanner.Build(map, mapper);

            Assert.AreEqual(2, plan.LatticePlacements);
            Assert.AreEqual(2, plan.TablePlacements);
            Assert.AreEqual(1, plan.SmesPlacements);

            MapImportCellPlan latticeCell = plan.Cells.Find(c =>
                c.Placements.Count == 1 && c.Placements[0].SoName == "Lattice");
            Assert.IsNotNull(latticeCell, "space+lattice should import with Lattice as Plenum only");

            MapImportCellPlan catwalkCell = plan.Cells.Find(c =>
                c.Placements.Count == 1 && c.Placements[0].SoName == "Catwalk");
            Assert.IsNotNull(catwalkCell);

            Assert.IsFalse(plan.Cells.Any(c =>
                c.Placements.Any(p => p.SoName == "Lattice") &&
                c.Placements.Any(p => p.SoName == MapImportPlanner.PlenumSoName)),
                "Lattice must replace Plenum, not stack with it");

            Assert.IsFalse(plan.Cells.Any(c =>
                c.Placements.Any(p => p.SoName == "Lattice") &&
                c.Placements.Any(p => p.SoName == "Catwalk")),
                "Lattice and Catwalk are mutually exclusive Plenum replacements");

            MapImportCellPlan woodCell = plan.Cells.Find(c =>
                c.Placements.Any(p => p.SoName == "TableWood"));
            Assert.IsNotNull(woodCell);
            Assert.AreEqual(MapImportPlanner.PlenumSoName, woodCell.Placements[0].SoName);
            Assert.AreEqual("TileGrey", woodCell.Placements[1].SoName);

            MapImportCellPlan steelCell = plan.Cells.Find(c =>
                c.Placements.Any(p => p.SoName == "TableSteel"));
            Assert.IsNotNull(steelCell);

            MapImportCellPlan smesCell = plan.Cells.Find(c =>
                c.Placements.Any(p => p.SoName == "SMES"));
            Assert.IsNotNull(smesCell);
            Assert.AreEqual(Direction.East,
                smesCell.Placements.Find(p => p.SoName == "SMES").Direction);
        }

        [Test]
        public void Planner_LatticeAndCatwalkOnSameCell_PrefersCatwalkOnly()
        {
            // Rare/malformed DMM: both atoms listed — still one Plenum SO (longer prefix wins).
            const string dmm = @"
""aaa"" = (
/obj/structure/lattice,
/obj/structure/lattice/catwalk,
/turf/open/space/basic,
/area/space)

(1,1,1) = {""
aaa
""}
";
            DmmMap map = DmmParser.Parse(dmm);
            Ss13TypeMapper mapper = new Ss13TypeMapper(Ss13TypeMapYaml.ParseFile(TypeMapPath));
            MapImportPlan plan = MapImportPlanner.Build(map, mapper);

            Assert.AreEqual(1, plan.Cells.Count);
            Assert.AreEqual(1, plan.LatticePlacements);
            Assert.AreEqual(1, plan.Cells[0].Placements.Count);
            Assert.AreEqual("Catwalk", plan.Cells[0].Placements[0].SoName);
        }

        [Test]
        public void Planner_BBox_FiltersCells()
        {
            DmmMap map = DmmParser.ParseFile(FixturePath);
            Ss13TypeMapper mapper = new Ss13TypeMapper(Ss13TypeMapYaml.ParseFile(TypeMapPath));
            MapImportBBox bbox = new MapImportBBox(1, 1, 1, 1);
            MapImportPlan plan = MapImportPlanner.Build(map, mapper, bbox);
            Assert.Less(plan.Cells.Count, 18);
            Assert.Greater(plan.SkippedCells, 0);
        }
    }
}
