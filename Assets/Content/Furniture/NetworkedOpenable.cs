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
        private static readonly int Open = Animator.StringToHash("Open");

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
            return animator.GetBool(Open);
        }

        private void OnOpenStateChange(object sender, bool e)
        {
            RpcSetOpenState(e);
        }

        protected virtual void Start()
        {
            animator = GetComponent<Animator>();
        }
        
        [ClientRpc]
        private void RpcSetOpenState(bool open)
        {
            animator.SetBool(Open, open);
        }
    }
}