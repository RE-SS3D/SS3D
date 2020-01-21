using System;
using UnityEngine;

namespace Interaction.Core
{
    public abstract class Interaction : ScriptableObject, IInteraction
    {
        internal GameObject receiver = null;
        protected GameObject Receiver => receiver;
        
        public abstract void Setup(Action<string> listen, Action<string> blocks);
        public abstract bool Handle(Event e);
    }
}