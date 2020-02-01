using UnityEngine;
using System.Collections;

namespace Interactions2.Custom
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
    public class Openable : Core.InteractionComponent
    {
        public bool Open { get; private set; }

        public override bool CanInteract(GameObject tool, GameObject target, RaycastHit at) => target == gameObject;

        public override void Interact(GameObject tool, GameObject target, RaycastHit at)
        {
            Open = !Open;
            // Start the animator on it's work
            animator.SetBool("open", Open);
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private Animator animator;
    }
}
