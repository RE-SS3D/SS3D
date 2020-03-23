using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Interactions.Extensions
{
    /**
     * <summary>Take all interactions from the parent object</summary>
     */
    public class UseParentInteractions : MonoBehaviour, InteractionCreator
    {
        public List<Interaction> Generate(InteractionEvent ev)
            => transform.parent.gameObject.GetAllInteractions(ev);
    }
}
