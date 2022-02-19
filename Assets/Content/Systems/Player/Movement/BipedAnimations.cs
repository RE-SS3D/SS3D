using UnityEngine;

namespace SS3D.Content.Systems.Player
{
    public static class BipedAnimations
    {
        public static class Human
        {
            private const string movementSpeed = "Speed";

            public static int MovementSpeed => Animator.StringToHash(movementSpeed);
        }
    }
}