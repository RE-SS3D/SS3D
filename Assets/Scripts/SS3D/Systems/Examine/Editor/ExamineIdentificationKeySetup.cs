using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SS3D.Systems.Examine.Editor
{
    public static class ExamineIdentificationKeySetup
    {
        [MenuItem("SS3D/Localization/Examine/Add Identification Card Template Keys")]
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

            EditorUtility.SetDirty(englishTable);
            EditorUtility.SetDirty(englishTable.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();

            Debug.Log("Added identification card template keys to the Examine English table.");
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
