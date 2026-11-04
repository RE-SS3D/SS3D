using System.Collections.Generic;
using SS3D.Localization;
using SS3D.Systems.Tile;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Appends Damaged / Cracked integrity lines when examining structural Turf.
    /// </summary>
    public class StructuralIntegrityExaminable : SimpleExaminable, IExamineContentProvider
    {
        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            if (!TryGetComponent(out PlacedTileObject placed))
                return;

            StructuralIntegrityStage stage = placed.IntegrityStage;
            if (stage == StructuralIntegrityStage.Intact || stage == StructuralIntegrityStage.Destroyed)
                return;

            if (SS3D.Systems.StructuralDamage.StructuralIntegrityRules.IsGlassTable(placed.tileObjectSO))
            {
                bool fractured = stage == StructuralIntegrityStage.Cracked;
                sections.Add(new ExamineSection(LocalizedTextService.GetFormattedString(
                    ExamineCanonicalKeyGenerator.ExamineTableName,
                    fractured ? ExamineStructuralIntegrityKeys.FracturedGlass : ExamineStructuralIntegrityKeys.ChippedGlass,
                    null,
                    fractured ? ExamineStructuralIntegrityKeys.FracturedGlassFallback : ExamineStructuralIntegrityKeys.ChippedGlassFallback)));
                return;
            }

            if (SS3D.Systems.StructuralDamage.StructuralIntegrityRules.IsWoodTable(placed.tileObjectSO))
            {
                bool split = stage == StructuralIntegrityStage.Cracked;
                sections.Add(new ExamineSection(LocalizedTextService.GetFormattedString(
                    ExamineCanonicalKeyGenerator.ExamineTableName,
                    split ? ExamineStructuralIntegrityKeys.SplitWood : ExamineStructuralIntegrityKeys.GougedWood,
                    null,
                    split ? ExamineStructuralIntegrityKeys.SplitWoodFallback : ExamineStructuralIntegrityKeys.GougedWoodFallback)));
                return;
            }

            if (stage == StructuralIntegrityStage.Damaged)
            {
                sections.Add(new ExamineSection(LocalizedTextService.GetFormattedString(
                    ExamineCanonicalKeyGenerator.ExamineTableName,
                    ExamineStructuralIntegrityKeys.Damaged,
                    null,
                    ExamineStructuralIntegrityKeys.DamagedFallback)));
                return;
            }

            if (stage == StructuralIntegrityStage.Cracked)
            {
                bool metalObject = placed.GenericType == TileObjectGenericType.Door
                    || SS3D.Systems.StructuralDamage.StructuralIntegrityRules.IsSteelTable(placed.tileObjectSO)
                    || SS3D.Systems.StructuralDamage.StructuralIntegrityRules.IsMetalFurniture(placed.tileObjectSO);
                sections.Add(new ExamineSection(LocalizedTextService.GetFormattedString(
                    ExamineCanonicalKeyGenerator.ExamineTableName,
                    metalObject ? ExamineStructuralIntegrityKeys.TornMetal : ExamineStructuralIntegrityKeys.Cracked,
                    null,
                    metalObject ? ExamineStructuralIntegrityKeys.TornMetalFallback : ExamineStructuralIntegrityKeys.CrackedFallback)));
            }
        }
    }
}
