using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using UnityEngine;

namespace SS3D.Content.Furniture
{
    // This handles networking for an Openable object
    [RequireComponent(typeof(Animator))]
    public class NetworkedOpenable : InteractionTargetNetworkBehaviour
    {
        protected Animator animator;
        private static readonly int OpenAnimation = Animator.StringToHash("Open");

        [SyncVar(hook = nameof(OpenHook))]
        private bool openState;

        [SerializeField] protected Sprite OpenIcon;
        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
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

        protected virtual void OnOpenStateChange(object sender, bool e)
        {
            OpenAllOpenables(sender, e);
            openState = e;
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
            if (openables[0] == this)
            {
                for (int i = 1; i < openables.Length; i++)
                {
                    openables[i].OnOpenStateChange(sender, e);
                }
            }
        }

        [Server]
        public void setOpenState(bool e)
        {
            openState = e;
            UpdateAnimator();
        }
        
        protected virtual void OpenHook(bool oldVal, bool newVal)
        {
            UpdateAnimator();
        }
        
        private void UpdateAnimator()
        {
            animator.SetBool(OpenAnimation, openState);
        }
    }
}