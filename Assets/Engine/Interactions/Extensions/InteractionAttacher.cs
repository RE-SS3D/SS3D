using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Engine.Interactions.Extensions
{
    /**
     * <summary>
     * Attaches InteractionSOs and ContinuousInteractionSOs to the given object. <br/>
     * Note: InteractionSOs may be called with the object as a target or a tool.
     * </summary>
     */
    public class InteractionAttacher : MonoBehaviour, InteractionCreator
    {
        // WOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO! Unity 2019.3 will support interface lists.
        // Meaning, when it comes out, we can do [SerializeReference] and Interaction can be a fucking Interface again!!
        public InteractionSO[] interactions = null;
        public ContinuousInteractionSO[] continuousInteractions = null;

        public List<Interaction> Generate(InteractionEvent e)
        {
            return new List<Interaction>()
                .Concat(interactions)
                .Concat(continuousInteractions)
                .ToList();
        }
    }
}
