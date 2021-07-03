using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using UnityEngine;
using SS3D.Content.Furniture.Storage;
using SS3D.Engine.Inventory.UI;
using SS3D.Engine.Inventory.Extensions;
using SS3D.Engine.Inventory;

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

        [SerializeField] private Sprite OpenIcon;

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            OpenInteraction openInteraction = new OpenInteraction();
            openInteraction.icon = OpenIcon;
            openInteraction.OpenStateChange += OnOpenStateChange;
            openInteraction.OpenStateChangeSendEvent += OnOpenStateChangeReceiveEvent;
            return new IInteraction[] { openInteraction };
        }

        /// <summary>
        /// Method called whenever a NetworkedOpenable object is opened or closed. 
        /// </summary>
        public void OnOpenStateChangeReceiveEvent(object sender, OpenInteractionEventArgs e)
        {
            CloseUIWhenClosed(e);
        }

        /// <summary>
        /// Close the UI of opened containers when they are closed
        /// </summary>
        private void CloseUIWhenClosed(OpenInteractionEventArgs e)
        {
            if (e.interactionEvent.Source is Hands)
            {
                Hands hands = e.interactionEvent.Source as Hands;

                if (e.interactionEvent.Target is OpenableContainer && !e.Open)
                {
                    OpenableContainer openableContainer = e.interactionEvent.Target as OpenableContainer;
                    AttachedContainer attachedContainer = openableContainer.GameObject.GetComponent<AttachedContainer>();
                    hands.Inventory.CmdContainerClose(attachedContainer);
                }
            }
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