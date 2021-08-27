using System;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    [Serializable]
    public class OpenInteraction : IInteraction
    {
        public Sprite icon;

        public event EventHandler<OpenInteractionEventArgs> OpenStateChange;
        private static readonly int OpenId = Animator.StringToHash("Open");

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return ((IGameObjectProvider)interactionEvent.Target).GameObject.GetComponent<Animator>().GetBool(OpenId) ? "Close" : "Open";
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }
            
            if (interactionEvent.Target is IGameObjectProvider target)
            {
                return target.GameObject.GetComponent<Animator>() != null;
            }
            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            GameObject target = ((IGameObjectProvider) interactionEvent.Target).GameObject;
            Animator animator = target.GetComponent<Animator>();
            bool open = animator.GetBool(OpenId);
            animator.SetBool(OpenId, !open);

            OpenInteractionEventArgs openInteractionEventArgs = new OpenInteractionEventArgs();
            openInteractionEventArgs.Open = !open;
            openInteractionEventArgs.interactionEvent = interactionEvent;
            OnOpenStateChange(openInteractionEventArgs);
            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }

        private void OnOpenStateChange(OpenInteractionEventArgs e)
        {
            OpenStateChange?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Structure used as an Argument sent by the EventHandler<OpenInteractionEventArgs>
    /// </summary>
    public class OpenInteractionEventArgs : EventArgs
    {
        public bool Open { get; set; }
        public InteractionEvent interactionEvent { get; set; }
    }
}