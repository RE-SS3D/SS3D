using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Interactions;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    [RequireComponent(typeof(AttachedContainer))]
    [RequireComponent(typeof(Animator))]
    public class OpenableContainer : NetworkActor, IOpenable, IInteractionTarget
    {
        private static readonly int OpenAnimation = Animator.StringToHash("Open");

        private Animator _animator;

        [SyncVar(OnChange = nameof(SyncOpenState))]
        private bool _openState;

        public bool IsOpen => _openState;

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            OpenInteraction openInteraction = new(this);

            return new IInteraction[]
            {
                openInteraction,
            };
        }

        [Server]
        public void SetOpen(bool openState)
        {
            _openState = openState;
            UpdateAnimator();
        }

        protected override void OnAwake()
        {
            _animator = GetComponent<Animator>();
        }

        private void SyncOpenState(bool oldVal, bool newVal, bool asServer)
        {
            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            _animator.SetBool(OpenAnimation, _openState);
        }
    }
}
