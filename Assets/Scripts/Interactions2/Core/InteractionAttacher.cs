using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Interactions2.Core
{
    /**
     * <summary>
     * Attaches InteractionSOs to the given object. <br/>
     * Note: InteractionSOs may be called with the object as a target or a tool.
     * </summary>
     */
    public class InteractionAttacher : MonoBehaviour
    {
        // WOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO! Unity 2019.3 will support interface lists.
        // Meaning, when it comes out, we can do [SerializeReference] and Interaction can be a fucking Interface again!!
        public InteractionSO[] interactions = null;
    }
}
