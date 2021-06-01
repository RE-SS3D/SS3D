using UnityEngine;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Items.Functional.Generic
{
    public class Lighter: Item, IIgniter
    {
        private static readonly int OpenHash = Animator.StringToHash("Open");

        private Animator animator;

        public bool CanIgnite => animator.GetBool(OpenHash);

        public override void Start()
        {
            base.Start();
            animator = GetComponent<Animator>();
        }
    }
}
