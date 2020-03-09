using System.Collections.Generic;
using UnityEngine;
using Interactions.Core;

namespace Interactions.Core.Extensions
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
