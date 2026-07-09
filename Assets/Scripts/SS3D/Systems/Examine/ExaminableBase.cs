using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Selection;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    [RequireComponent(typeof(Selectable))]
    public abstract class ExaminableBase : Actor, IExaminable, IInteractionTarget
    {
        private static readonly ExamineInteraction Examine = new();

        [SerializeField] private ExamineData key;

        public ExamineData GetData()
        {
            return key;
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            if (GetData() == null)
            {
                return System.Array.Empty<IInteraction>();
            }

            return new IInteraction[] { Examine };
        }
    }
}
