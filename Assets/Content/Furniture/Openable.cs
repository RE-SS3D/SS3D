using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Content.Furniture
{
    [RequireComponent(typeof(Animator))]
    public class Openable : InteractionTargetBehaviour
    {
        protected Animator animator;
        private static readonly int OpenId = Animator.StringToHash("Open");
        [SerializeField] private Sprite openInteractionIcon;

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            OpenInteraction openInteraction = new OpenInteraction();
            openInteraction.icon = openInteractionIcon;
            openInteraction.OpenStateChange += OnOpenStateChange;
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