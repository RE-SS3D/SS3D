using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using UnityEngine;

namespace SS3D.Content.Furniture
{
    [RequireComponent(typeof(Animator))]
    public class NetworkedOpenable : InteractionTargetNetworkBehaviour
    {
        protected Animator animator;
        private static readonly int OpenAnimation = Animator.StringToHash("Open");

        [SyncVar(hook = nameof(OpenHook))]
        private bool openState;

        [SerializeField] private Sprite OpenIcon;

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            OpenInteraction openInteraction = new OpenInteraction();
            openInteraction.icon = OpenIcon;
            openInteraction.OpenStateChange += OnOpenStateChange;
            return new IInteraction[] { openInteraction };
        }
        
        public bool IsOpen()
        {
            return openState;
        }

        protected virtual void Start()
        {
            animator = GetComponent<Animator>();
        }

        private void OnOpenStateChange(object sender, bool e)
        {
            openState = e;
            UpdateAnimator();
        }
        
        private void OpenHook(bool oldVal, bool newVal)
        {
            UpdateAnimator();
        }
        
        private void UpdateAnimator()
        {
            animator.SetBool(OpenAnimation, openState);
        }
    }
}