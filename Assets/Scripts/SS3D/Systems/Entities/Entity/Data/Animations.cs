using UnityEngine;

namespace SS3D.Systems.Entities.Data
{
    public static class Animations
    {
        public static class Humanoid
        {
            public static readonly int MovementSpeed = Animator.StringToHash("Speed");
            public static readonly int StartMoving = Animator.StringToHash("StartMoving");
            public static readonly int EndMoving = Animator.StringToHash("EndMoving");
            public static readonly float PickupReachTime = 0.3f;
            public static readonly float PickupMoveItemTime = 0.3f;
        }

        public static class Silicon
        {
            public static readonly int Power = Animator.StringToHash("Power");
        }  

    }
}
