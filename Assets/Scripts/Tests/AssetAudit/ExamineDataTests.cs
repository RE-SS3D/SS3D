using NUnit.Framework;
using System.Text;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using SS3D.Systems.Examine;

namespace AssetAudit
{
    public class ExamineDataTests
    {

        private const string ExamineDataSearchTerm = "t:ExamineData";

        #region Tests
        /// <summary>
        /// Ensure all Examine Data have a reference to their localization table.
        /// </summary>
        [Test, TestCaseSource(nameof(AllExamineData))]
        public void EveryExamineDataHasALocalizationTable(ExamineData examineData)
        {
            Assert.IsTrue(examineData?.LocalizationTable.ToString() is not null, $"ExamineData '{examineData.name}' does not have a localization table set.\n");
        }

        #endregion

        #region Helper functions

        private static ExamineData[] AllExamineData()
        {
            return AssetAuditUtilities.GetAssets<ExamineData>(ExamineDataSearchTerm);
        }

        #endregion
    }
}
