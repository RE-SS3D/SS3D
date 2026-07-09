using NUnit.Framework;
using System.Text;
using UnityEditor;
using UnityEngine.Localization.Tables;
using SS3D.Systems.Examine;

namespace AssetAudit
{
    public class ExamineDataTests
    {
        private const string ExamineDataSearchTerm = "t:ExamineData";
        private const string ExamineSharedDataPath =
            "Assets/Content/Localization/Table Collections/Examine/Examine Shared Data.asset";
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

        #endregion

        #region Helper functions

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
