using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Stands in for an <see cref="Item"/> that carries no <see cref="ExaminableBase"/> (examine
    /// feedback Decision 4): it examines as <see cref="Item.Name"/> plus the generic item lines. Prefabs
    /// add a real examinable only for flavour or a content provider. Not a component; made by
    /// <see cref="ExamineSubSystem"/> when the hover target has an Item but no examinable.
    /// </summary>
    public sealed class ItemFallbackExaminable : IExaminable
    {
        private static ExamineData _sharedData;

        public ItemFallbackExaminable(Item item)
        {
            Item = item;
        }

        public Item Item { get; }

        /// <summary>A shared text-type data asset so callers that gate on GetData() treat it as examinable.</summary>
        public ExamineData GetData()
        {
            if (_sharedData == null)
            {
                _sharedData = ScriptableObject.CreateInstance<ExamineData>();
                _sharedData.name = nameof(ItemFallbackExaminable);
                _sharedData.hideFlags = HideFlags.HideAndDontSave;
            }

            return _sharedData;
        }
    }
}
