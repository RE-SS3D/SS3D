using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Content.Furniture
{
    // This handles an object or an item that can be opened
    // this requires the target to have a container to open
    [RequireComponent(typeof(Animator))]
    public class Openable : InteractionTargetBehaviour
    {
        protected Animator animator;

        // This defines the animation we want to trigger
        private static readonly int OpenId = Animator.StringToHash("Open");
	// This defines the icon that will be shown in the interaction menu
        [SerializeField] private Sprite openInteractionIcon;

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
	    // Here we generate the interactions when we are not holding the item
            OpenInteraction openInteraction = new OpenInteraction();
            openInteraction.icon = openInteractionIcon;
	    // fires an event to tell the subscribers the state has been changed
            openInteraction.OpenStateChange += OnOpenStateChange;
	    // returns the new interaction list
            return new IInteraction[] { openInteraction };
        }

        public void SetOpen(bool open)
        {
            Assert.IsNotNull(animator, "Openable animator cannot be null");
            animator.SetBool(OpenId, open);
        }
        
        public bool IsOpen()
        {
            return animator.GetBool(OpenId);
        }

        protected virtual void OnOpenStateChange(object sender, bool open)
        {
            
        }

        public virtual void Start()
        {
            animator = GetComponent<Animator>();
        }
        
    }
}
