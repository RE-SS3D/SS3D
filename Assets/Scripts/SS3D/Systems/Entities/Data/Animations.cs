using UnityEngine;

namespace SS3D.Systems.Entity.Data
{
    public static class Animations
    {
        public static class Humanoid
        {
            public static readonly int MovementSpeed = Animator.StringToHash("Speed");
        }

        public static class Silicon
        {
            public static readonly int Power = Animator.StringToHash("Power");
        }
    }
}