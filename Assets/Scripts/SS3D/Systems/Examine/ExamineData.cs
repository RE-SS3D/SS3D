using UnityEngine;
using UnityEngine.Localization;

namespace SS3D.Systems.Examine
{
    [CreateAssetMenu(menuName = "Examine", fileName = "ExamineData")]
    public class ExamineData : ScriptableObject
    {
        /// <summary>
        /// The table holding the localization strings for this object.
        /// </summary>
        public LocalizedStringTable LocalizationTable;

        /// <summary>
        /// Key to access the name of the object, when the cursor hovers over the item.
        /// </summary>
        public string NameKey;

        /// <summary>
        /// Key to access the description shown below the name, when the cursor hovers over the item while holding Shift.
        /// </summary>
        public string DescriptionKey;
    }
}