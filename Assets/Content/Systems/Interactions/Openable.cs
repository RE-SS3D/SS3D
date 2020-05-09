using Mirror;
using UnityEngine;
using SS3D.Engine.Interactions;

namespace SS3D.Content.Systems.Interactions
{
    /**
     * <summary>
     * Attach to the target, will allow object to open/close via an animation controller
     * </summary>
     * <remarks>
     * The animator should have an 'open' boolean variable, which will get set appropriately
     * on interaction.
     * 
     * The object starts closed by default
     * </remarks>
     * <inheritdoc cref="Core.Interaction"/>
     */
    [RequireComponent(typeof(Animator))]
    public class Openable : NetworkBehaviour, Interaction
    {
        public bool Open { get; private set; }
        public InteractionEvent Event { get; set; }
        public string Name => Open ? "Close" : "Open";

        public bool CanInteract() => Event.target == gameObject;

        public void Interact()
        {
            Open = !Open;
            animator.SetBool(OpenAnimator, Open);
            RpcInteract(Open);
        }

        [ClientRpc]
        private void RpcInteract(bool open)
        {
            Open = open;
            animator.SetBool(OpenAnimator, open);
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private Animator animator;
        private static readonly int OpenAnimator = Animator.StringToHash("Open");
    }
}
