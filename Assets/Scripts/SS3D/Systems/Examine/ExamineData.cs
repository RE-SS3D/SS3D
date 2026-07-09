using SS3D.Interactions;
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

        /// <summary>
        /// Maximum distance from the player for showing the detailed image view of
        /// <see cref="ExamineType.SIMPLE_IMAGE"/> types. Outside this range, Shift-held
        /// examine falls back to the text detailed view.
        /// </summary>
        public RangeLimit DetailedImageRange = new(1.5f, 2f);
    }
}
