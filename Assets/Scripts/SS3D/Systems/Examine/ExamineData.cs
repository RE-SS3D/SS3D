using UnityEngine;
using UnityEngine.Localization;

namespace SS3D.Systems.Examine
{
    [CreateAssetMenu(menuName = "Examine", fileName = "ExamineData")]
    public class ExamineData : ScriptableObject
    {
        public ExamineType Type = ExamineType.SIMPLE_TEXT;

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
        /// For image examine types, this is used as the caption below the image.
        /// </summary>
        public string DescriptionKey;

        /// <summary>
        /// Display size for the detailed image view of <see cref="ExamineType.SIMPLE_IMAGE"/> types.
        /// The sprite itself is provided by the examinable object.
        /// </summary>
        public Vector2 DetailedImageSize = new Vector2(260f, 175f);
    }
}