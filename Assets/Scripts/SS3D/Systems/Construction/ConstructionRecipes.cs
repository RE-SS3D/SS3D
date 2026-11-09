using System.Collections.Generic;
using SS3D.Systems.Tile;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Every construction rule, as one reviewable table (D4 of 2026-11_construction-mvp1). Timings follow
    /// construction.md §10 feel targets; costs are first guesses for the balancing pass. Move to SO assets
    /// if balancing needs the Inspector.
    /// </summary>
    public static class ConstructionRecipes
    {
        public static readonly IReadOnlyList<ConstructionRule> Rules = new[]
        {
            // Floors and hull (S2)
            new ConstructionRule
            {
                Step = ConstructionStep.PlacePlating, GenericName = "ConstructionPlacePlating", Verb = "Patch hull",
                Input = ConstructionInput.SteelSheet, Target = ConstructionTargetKind.HullHole, Layer = TileLayer.Plenum,
                Cost = 1, Seconds = 3f, Outcome = ConstructionOutcome.Place, ResultName = ConstructionNames.Plenum,
                IsCellAction = true,
            },
            new ConstructionRule
            {
                Step = ConstructionStep.PlaceFloorTile, GenericName = "ConstructionPlaceFloorTile", Verb = "Lay floor tile",
                Input = ConstructionInput.FloorTile, Target = ConstructionTargetKind.BarePlating, Layer = TileLayer.Turf,
                Cost = 1, Seconds = 1f, Outcome = ConstructionOutcome.Place, ResultName = ConstructionNames.FloorTurf,
                IsCellAction = true,
            },
            new ConstructionRule
            {
                Step = ConstructionStep.PryFloorTile, GenericName = "ConstructionPryTile", Verb = "Pry tile",
                Input = ConstructionInput.Crowbar, Target = ConstructionTargetKind.FloorTurf, Layer = TileLayer.Turf,
                Seconds = 1f, Outcome = ConstructionOutcome.Remove,
                ReturnItemName = ConstructionNames.FloorTileItem, ReturnCount = 1,
            },
            new ConstructionRule
            {
                Step = ConstructionStep.RepairPlating, GenericName = "ConstructionRepairPlating", Verb = "Repair",
                Input = ConstructionInput.Welder, Target = ConstructionTargetKind.DamagedPlenum, Layer = TileLayer.Plenum,
                Seconds = 4f, Outcome = ConstructionOutcome.Repair,
            },

            // Weld repair (S3)
            new ConstructionRule
            {
                Step = ConstructionStep.RepairStructure, GenericName = "ConstructionRepairStructure", Verb = "Repair",
                Input = ConstructionInput.Welder, Target = ConstructionTargetKind.DamagedStructure, Layer = TileLayer.Turf,
                Seconds = 4f, Outcome = ConstructionOutcome.Repair,
            },

            // Girder ladder and windows (S4). Girder to sealed is about 15 s across three steps (§10).
            new ConstructionRule
            {
                Step = ConstructionStep.PlaceGirder, GenericName = "ConstructionPlaceGirder", Verb = "Build girder",
                Input = ConstructionInput.SteelSheet, Target = ConstructionTargetKind.ClearFloor, Layer = TileLayer.Turf,
                Cost = 2, Seconds = 5f, Outcome = ConstructionOutcome.Place, ResultName = ConstructionNames.Girder,
                IsCellAction = true,
            },
            new ConstructionRule
            {
                Step = ConstructionStep.PlateGirder, GenericName = "ConstructionPlateGirder", Verb = "Plate girder",
                Input = ConstructionInput.SteelSheet, Target = ConstructionTargetKind.NamedTile, TargetName = ConstructionNames.Girder,
                Layer = TileLayer.Turf, Cost = 2, Seconds = 5f, Outcome = ConstructionOutcome.Replace,
                ResultName = ConstructionNames.GirderPlated,
            },
            new ConstructionRule
            {
                Step = ConstructionStep.WeldPlated, GenericName = "ConstructionWeldSeams", Verb = "Weld seams",
                Input = ConstructionInput.Welder, Target = ConstructionTargetKind.NamedTile, TargetName = ConstructionNames.GirderPlated,
                Layer = TileLayer.Turf, Seconds = 5f, Outcome = ConstructionOutcome.Replace, ResultName = ConstructionNames.Wall,
            },
            new ConstructionRule
            {
                Step = ConstructionStep.GlazeGirder, GenericName = "ConstructionGlazeGirder", Verb = "Fit pane",
                Input = ConstructionInput.GlassSheet, Target = ConstructionTargetKind.NamedTile, TargetName = ConstructionNames.Girder,
                Layer = TileLayer.Turf, Cost = 2, Seconds = 7f, Outcome = ConstructionOutcome.Replace,
                ResultName = ConstructionNames.Window,
            },
            new ConstructionRule
            {
                Step = ConstructionStep.ClearWreckage, GenericName = "ConstructionClearWreckage", Verb = "Clear wreckage",
                Input = ConstructionInput.Crowbar, Target = ConstructionTargetKind.Wreckage, Layer = TileLayer.Turf,
                Seconds = 3f, Outcome = ConstructionOutcome.ClearWreckage,
                ReturnItemName = ConstructionNames.SteelSheetItem, ReturnCount = 2, IsCellAction = true,
            },

            // Deconstruction (S5): the ladder backwards, from Intact only, about twice the build time,
            // never on reinforced (§9). No debris.
            new ConstructionRule
            {
                Step = ConstructionStep.CutSeams, GenericName = "ConstructionCutSeams", Verb = "Cut seams",
                Input = ConstructionInput.Welder, Target = ConstructionTargetKind.NamedTile, TargetName = ConstructionNames.Wall,
                Layer = TileLayer.Turf, Seconds = 10f, Outcome = ConstructionOutcome.Replace,
                ResultName = ConstructionNames.GirderPlated, RequiresIntact = true, RefusesReinforced = true,
            },
            new ConstructionRule
            {
                Step = ConstructionStep.PullPlate, GenericName = "ConstructionPullPlate", Verb = "Pull plate",
                Input = ConstructionInput.Crowbar, Target = ConstructionTargetKind.NamedTile, TargetName = ConstructionNames.GirderPlated,
                Layer = TileLayer.Turf, Seconds = 10f, Outcome = ConstructionOutcome.Replace, ResultName = ConstructionNames.Girder,
                ReturnItemName = ConstructionNames.SteelSheetItem, ReturnCount = 2, RequiresIntact = true, RefusesReinforced = true,
            },
            new ConstructionRule
            {
                Step = ConstructionStep.LiftPane, GenericName = "ConstructionLiftPane", Verb = "Lift pane",
                Input = ConstructionInput.Crowbar, Target = ConstructionTargetKind.NamedTile, TargetName = ConstructionNames.Window,
                Layer = TileLayer.Turf, Seconds = 14f, Outcome = ConstructionOutcome.Replace, ResultName = ConstructionNames.Girder,
                ReturnItemName = ConstructionNames.GlassSheetItem, ReturnCount = 2, RequiresIntact = true, RefusesReinforced = true,
            },
            new ConstructionRule
            {
                Step = ConstructionStep.DismantleGirder, GenericName = "ConstructionDismantleGirder", Verb = "Dismantle girder",
                Input = ConstructionInput.Welder, Target = ConstructionTargetKind.NamedTile, TargetName = ConstructionNames.Girder,
                Layer = TileLayer.Turf, Seconds = 10f, Outcome = ConstructionOutcome.Remove,
                ReturnItemName = ConstructionNames.SteelSheetItem, ReturnCount = 2, RequiresIntact = true, RefusesReinforced = true,
            },
        };

        public static ConstructionRule Get(ConstructionStep step)
        {
            for (int i = 0; i < Rules.Count; i++)
            {
                if (Rules[i].Step == step)
                {
                    return Rules[i];
                }
            }

            return null;
        }

        /// <summary>Rules this input can run, in table order. Cell actions or object steps only.</summary>
        public static void ForInput(ConstructionInput input, bool cellActions, List<ConstructionRule> into)
        {
            into.Clear();
            for (int i = 0; i < Rules.Count; i++)
            {
                ConstructionRule rule = Rules[i];
                if (rule.Input == input && rule.IsCellAction == cellActions)
                {
                    into.Add(rule);
                }
            }
        }

        /// <summary>Every catalog name the table refers to, for the resolve-all test.</summary>
        public static IEnumerable<string> ReferencedNames()
        {
            foreach (ConstructionRule rule in Rules)
            {
                if (!string.IsNullOrEmpty(rule.TargetName)) yield return rule.TargetName;
                if (!string.IsNullOrEmpty(rule.ResultName)) yield return rule.ResultName;
                if (!string.IsNullOrEmpty(rule.ReturnItemName)) yield return rule.ReturnItemName;
            }

            yield return ConstructionNames.SteelSheetItem;
            yield return ConstructionNames.GlassSheetItem;
            yield return ConstructionNames.FloorTileItem;
            yield return ConstructionNames.WelderItem;
            yield return ConstructionNames.CrowbarItem;
        }
    }
}
