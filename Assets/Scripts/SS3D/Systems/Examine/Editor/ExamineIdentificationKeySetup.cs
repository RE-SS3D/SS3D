using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SS3D.Systems.Examine.Editor
{
    public static class ExamineIdentificationKeySetup
    {
        /// <summary>Tier C seed — no MenuItem. Call from batch if Examine keys drift.</summary>
        public static void AddIdentificationCardTemplateKeys()
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(ExamineCanonicalKeyGenerator.ExamineTableName);
            if (collection == null)
            {
                Debug.LogError($"Could not find string table collection '{ExamineCanonicalKeyGenerator.ExamineTableName}'.");
                return;
            }

            if (collection.GetTable(new LocaleIdentifier("en")) is not StringTable englishTable)
            {
                Debug.LogError("Could not find English table for the Examine collection.");
                return;
            }

            UpsertEntry(englishTable, ExamineIdentificationKeys.OwnerLine, ExamineIdentificationKeys.OwnerFallback);
            UpsertEntry(englishTable, ExamineIdentificationKeys.RoleLine, ExamineIdentificationKeys.RoleFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.Damaged, ExamineStructuralIntegrityKeys.DamagedFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.Cracked, ExamineStructuralIntegrityKeys.CrackedFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.TornMetal, ExamineStructuralIntegrityKeys.TornMetalFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.GougedWood, ExamineStructuralIntegrityKeys.GougedWoodFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.SplitWood, ExamineStructuralIntegrityKeys.SplitWoodFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.ChippedGlass, ExamineStructuralIntegrityKeys.ChippedGlassFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.FracturedGlass, ExamineStructuralIntegrityKeys.FracturedGlassFallback);

            EditorUtility.SetDirty(englishTable);
            EditorUtility.SetDirty(englishTable.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();

            Debug.Log("Added identification card + structural integrity template keys to the Examine English table.");
        }

        /// <summary>Tier C seed — no MenuItem. Prefer <see cref="AddIdentificationCardTemplateKeys"/> which also upserts integrity keys.</summary>
        public static void AddStructuralIntegrityTemplateKeys()
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(ExamineCanonicalKeyGenerator.ExamineTableName);
            if (collection == null)
            {
                Debug.LogError($"Could not find string table collection '{ExamineCanonicalKeyGenerator.ExamineTableName}'.");
                return;
            }

            if (collection.GetTable(new LocaleIdentifier("en")) is not StringTable englishTable)
            {
                Debug.LogError("Could not find English table for the Examine collection.");
                return;
            }

            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.Damaged, ExamineStructuralIntegrityKeys.DamagedFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.Cracked, ExamineStructuralIntegrityKeys.CrackedFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.TornMetal, ExamineStructuralIntegrityKeys.TornMetalFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.GougedWood, ExamineStructuralIntegrityKeys.GougedWoodFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.SplitWood, ExamineStructuralIntegrityKeys.SplitWoodFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.ChippedGlass, ExamineStructuralIntegrityKeys.ChippedGlassFallback);
            UpsertEntry(englishTable, ExamineStructuralIntegrityKeys.FracturedGlass, ExamineStructuralIntegrityKeys.FracturedGlassFallback);

            EditorUtility.SetDirty(englishTable);
            EditorUtility.SetDirty(englishTable.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();

            Debug.Log("Added structural integrity template keys to the Examine English table.");
        }

        private static void UpsertEntry(StringTable table, string key, string value)
        {
            StringTableEntry entry = table.GetEntry(key);
            if (entry == null)
            {
                table.AddEntry(key, value);
                return;
            }

            entry.Value = value;
        }
    }
}
