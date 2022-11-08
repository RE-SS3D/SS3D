﻿using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Storage.Interactions;
using UnityEngine;

namespace SS3D.Storage
{
    // This handles networking for an Openable object
    [RequireComponent(typeof(Animator))]
    public class NetworkedOpenable : InteractionTargetNetworkBehaviour
    {
        protected Animator Animator;
        private static readonly int OpenAnimation = Animator.StringToHash("Open");

        [SyncVar(OnChange = nameof(SyncOpenState))]
        private bool _openState;

        [SerializeField] protected Sprite OpenIcon;
        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            OpenInteraction openInteraction = new()
            {
                icon = OpenIcon
            };
            openInteraction.OnOpenStateChanged += OpenStateChanged;

            return new IInteraction[] { openInteraction };
        }
        
        public bool IsOpen()
        {
            return _openState;
        }

        protected virtual void Start()
        {
            Animator = GetComponent<Animator>();
        }

        protected virtual void OpenStateChanged(object sender, bool e)
        {
            OpenAllOpenables(sender, e);
            _openState = e;
            UpdateAnimator();
        }

        /// <summary>
        /// On game objects with a single open animation but multiple containers depending on it,
        /// this method assures us that when the animation is fired, all NetworkedOpenable scripts are updated.
        /// </summary>
        private void OpenAllOpenables(object sender, bool e)
        {
            NetworkedOpenable[] openables = gameObject.GetComponents<NetworkedOpenable>();

            // If this is the top NetworkedOpenable in the inspector, tell the others to open too. 
            if (openables[0] != this)
            {
                return;
            }

            for (int i = 1; i < openables.Length; i++)
            {
                openables[i].OpenStateChanged(sender, e);
            }
        }

        [Server]
        public void SetOpenState(bool e)
        {
            _openState = e;
            UpdateAnimator();
        }
        
        protected virtual void SyncOpenState(bool oldVal, bool newVal, bool asServer)
        {
            UpdateAnimator();
        }
        
        private void UpdateAnimator()
        {
            Animator.SetBool(OpenAnimation, _openState);
        }
    }
}