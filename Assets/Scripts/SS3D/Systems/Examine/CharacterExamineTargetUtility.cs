using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Selection;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Resolves character-examine targets from a selection pick. Worn clothing often carries its own
    /// <see cref="IExaminable"/> and is the deepest pick — callers must walk ancestors for
    /// <see cref="ExamineType.CHARACTER"/> / <see cref="HumanInventory"/> rather than trusting the
    /// first <see cref="IExaminable"/> on the selectable.
    /// </summary>
    public static class CharacterExamineTargetUtility
    {
        /// <summary>
        /// Finds a CHARACTER examinable and its <see cref="HumanInventory"/> on
        /// <paramref name="origin"/> or a parent. Returns false when the pick is not on a character.
        /// </summary>
        public static bool TryResolveFromTransform(
            Transform origin,
            out IExaminable characterExaminable,
            out HumanInventory inventory)
        {
            characterExaminable = null;
            inventory = null;

            Transform current = origin;
            while (current != null)
            {
                if (current.TryGetComponent(out HumanInventory foundInventory))
                {
                    IExaminable examinable = current.GetComponent<IExaminable>();
                    if (examinable?.GetData()?.Type == ExamineType.CHARACTER)
                    {
                        characterExaminable = examinable;
                        inventory = foundInventory;
                        return true;
                    }
                }

                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// Same as <see cref="TryResolveFromTransform"/> starting from the current selection pick.
        /// </summary>
        public static bool TryResolveFromSelectable(
            Selectable selectable,
            out IExaminable characterExaminable,
            out HumanInventory inventory)
        {
            characterExaminable = null;
            inventory = null;
            return selectable != null
                && TryResolveFromTransform(selectable.transform, out characterExaminable, out inventory);
        }
    }
}
