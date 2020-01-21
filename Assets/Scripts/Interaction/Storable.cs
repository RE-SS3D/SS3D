﻿using System;
using Interaction.Core;
using UnityEngine;
using Event = Interaction.Core.Event;

namespace Interaction
{
    [RequireComponent(typeof(InteractionReceiver))]
    public class Storable : MonoBehaviour, IInteraction
    {
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen("use");
        }

        public bool Handle(Event e)
        {
            if (!e.forwardTo) return false;
            e.forwardTo.Trigger(e.Forward("store", gameObject));

            return true;
        }
    }
}