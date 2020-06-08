using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public class ValveObject : MonoBehaviour, IInteractionTarget
    {
        public enum ValveType
        {
            Manual,
            Digital
        }

        public ValveType valveType;
        private PipeObject pipe;
        public bool isEnabled;

        private void SetValve(bool enable)
        {
            isEnabled = enable;
            if (!isEnabled)
            {
                pipe.SetBlocked(true);
            }
            else
            {
                pipe.SetBlocked(false);
            }
        }

        void Start()
        {
            pipe = GetComponent<PipeObject>();
            SetValve(false);
        }

        public IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = isEnabled ? "Close valve" : "Open valve", Interact = ValveInteract, RangeCheck = true
                }
            };
        }
        
        private void ValveInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            SetValve(!isEnabled);
        }
    }
}
