using SS3D.Attributes;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    [CreateAssetMenu(menuName = "Examine", fileName = "ExamineData")]
    public class ExamineData : ScriptableObject
    {
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