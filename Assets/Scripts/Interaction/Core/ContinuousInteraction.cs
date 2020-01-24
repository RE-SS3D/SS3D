using System;
using System.Collections;
using UnityEngine;

namespace Interaction.Core
{
    /// <summary>
    /// This is a Scriptable Object that may be added to an `InteractionReceiver` to receive an event without the need for a `MonoBehaviour`.<br/>
    /// The intended use case of this class is to implement fully or mostly stateless interactions.
    /// </summary>
    public abstract class ContinuousInteraction : ScriptableObject, IContinuousInteraction
    {
        internal GameObject receiver = null;
        
        /// <summary>
        /// The receiver that this Scriptable Object has been added to.
        /// </summary>
        protected GameObject Receiver => receiver;
        
        public abstract void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks);
        public abstract IEnumerator Handle(InteractionEvent e);
    }
}