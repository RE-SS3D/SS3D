using NUnit.Framework;
using System.Text;
using UnityEditor;
using UnityEngine.Localization.Tables;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace AssetAudit
{
    public class ExamineDataTests
    {
        private const string ExamineDataSearchTerm = "t:ExamineData";
        private const string ExamineSharedDataPath =
            "Assets/Content/Localization/Table Collections/Examine/Examine Shared Data.asset";
        private const string ExamineTableFolder = "Assets/Content/Localization/Table Collections/Examine";
        private const string ExamineEnglishTablePath =
            "Assets/Content/Localization/Table Collections/Examine/Examine_en.asset";

        #region Tests

        [Test, TestCaseSource(nameof(AllExamineData))]
        public void EveryExamineDataHasValidLocalizedStrings(ExamineData examineData)
        {
            Assert.IsFalse(examineData.Name.IsEmpty, $"ExamineData '{examineData.name}' is missing a localized name reference.\n");
            Assert.IsFalse(examineData.Description.IsEmpty, $"ExamineData '{examineData.name}' is missing a localized description reference.\n");
        }

        [Test, TestCaseSource(nameof(AllExamineData))]
        public void EveryExamineKeyExistsInTable(ExamineData examineData)
        {
            SharedTableData sharedData = GetExamineSharedData();
            StringBuilder errors = new();

            ValidateKeyExists(sharedData, examineData, examineData.Name.TableEntryReference.Key, errors);
            ValidateKeyExists(sharedData, examineData, examineData.Description.TableEntryReference.Key, errors);

            Assert.IsTrue(errors.Length == 0, errors.ToString());
        }

        [Test, TestCaseSource(nameof(AllExamineData))]
        public void EveryExamineEnglishValueIsNonEmpty(ExamineData examineData)
        {
            StringTable englishTable = GetExamineEnglishTable();
            StringBuilder errors = new();

            ValidateEnglishValue(englishTable, examineData, examineData.Name.TableEntryReference.Key, errors);
            ValidateEnglishValue(englishTable, examineData, examineData.Description.TableEntryReference.Key, errors);

            Assert.IsTrue(errors.Length == 0, errors.ToString());
        }

        /// <summary>
        /// Every item prefab examines as something (examine feedback Decision 4): its examinable has data,
        /// or else it sets its own name, since the item fallback shows <c>Item.Name</c> and an empty name
        /// falls back to the GameObject name ("HumanBrain").
        /// </summary>
        [Test]
        public void EveryItemPrefabResolvesToAnExamineName()
        {
            StringBuilder errors = new();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Content" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || !prefab.TryGetComponent(out Item item))
                {
                    continue;
                }

                if (prefab.TryGetComponent(out ExaminableBase examinable) && examinable.GetData() != null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(new SerializedObject(item).FindProperty("_name").stringValue))
                {
                    errors.Append($"-> {path}: no ExamineData and no Item name.\n");
                }
            }

            Assert.IsTrue(errors.Length == 0, errors.ToString());
        }

        /// <summary>
        /// Every <c>&lt;k=…&gt;</c> keyword tag in every Examine locale is one the formatter knows and is
        /// closed (examine.md §3). An unknown tag renders plain, so a typo would silently lose the colour.
        /// </summary>
        [Test]
        public void EveryExamineKeywordTagIsKnownAndClosed()
        {
            StringBuilder errors = new();
            string[] guids = AssetDatabase.FindAssets("t:StringTable", new[] { ExamineTableFolder });
            Assert.IsNotEmpty(guids, $"No string tables under {ExamineTableFolder}.");
            foreach (string guid in guids)
            {
                StringTable table = AssetDatabase.LoadAssetAtPath<StringTable>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (StringTableEntry entry in table.Values)
                {
                    string value = entry.Value;
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    int opened = 0;
                    foreach (string tag in ExamineKeywordFormatter.EnumerateTags(value))
                    {
                        opened++;
                        if (!ExamineKeywordFormatter.IsKnownTag(tag))
                        {
                            errors.Append($"-> {table.name} '{entry.Key}': unknown keyword tag '{tag}'.\n");
                        }
                    }

                    int closed = CountOccurrences(value, "</k>");
                    if (opened != closed)
                    {
                        errors.Append($"-> {table.name} '{entry.Key}': {opened} keyword tags opened, {closed} closed.\n");
                    }
                }
            }

            Assert.IsTrue(errors.Length == 0, errors.ToString());
        }

        #endregion

        #region Helper functions

        private static int CountOccurrences(string text, string token)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(token, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }

            return count;
        }

        private static ExamineData[] AllExamineData()
        {
            return AssetAuditUtilities.GetAssets<ExamineData>(ExamineDataSearchTerm);
        }

        private static SharedTableData GetExamineSharedData()
        {
            return AssetDatabase.LoadAssetAtPath<SharedTableData>(ExamineSharedDataPath);
        }

        private static StringTable GetExamineEnglishTable()
        {
            return AssetDatabase.LoadAssetAtPath<StringTable>(ExamineEnglishTablePath);
        }

        private static void ValidateKeyExists(SharedTableData sharedData, ExamineData examineData, string key, StringBuilder errors)
        {
            if (string.IsNullOrEmpty(key))
            {
                errors.Append($"-> ExamineData '{examineData.name}' has an empty localization key.\n");
                return;
            }

            if (sharedData.GetEntry(key) == null)
            {
                errors.Append($"-> ExamineData '{examineData.name}' references missing key '{key}'.\n");
            }
        }

        private static void ValidateEnglishValue(StringTable englishTable, ExamineData examineData, string key, StringBuilder errors)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            StringTableEntry entry = englishTable.GetEntry(key);
            if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
            {
                errors.Append($"-> ExamineData '{examineData.name}' has empty English text for key '{key}'.\n");
            }
        }

        #endregion
    }
}
