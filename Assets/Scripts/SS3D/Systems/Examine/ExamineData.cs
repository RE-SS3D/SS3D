using UnityEngine;
using UnityEngine.Localization;

namespace SS3D.Systems.Examine
{
    [CreateAssetMenu(menuName = "Examine", fileName = "ExamineData")]
    public class ExamineData : ScriptableObject
    {
        public ExamineType Type = ExamineType.SIMPLE_TEXT;

        public LocalizedString Name;

        public LocalizedString Description;

        /// <summary>
        /// Display size for the detailed image view of <see cref="ExamineType.SIMPLE_IMAGE"/> types.
        /// The sprite itself is provided by the examinable object.
        /// </summary>
        public Vector2 DetailedImageSize = new Vector2(260f, 175f);
    }
}
