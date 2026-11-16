using FishNet;
using SS3D.Core;
using UnityEngine;

namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Makes a loose rigidbody sound when it hits something hard enough. Only the server simulates
    /// items, so the server decides and every sound is a message, held down by
    /// <see cref="CollisionSoundLimiter"/>. Added to item prefabs by <c>AudioContentPrefabRecipes</c>.
    /// </summary>
    public class NoisyCollision : MonoBehaviour
    {
        /// <summary>Slower contacts are silent, which keeps settling and rolling quiet.</summary>
        public const float MinImpactSpeed = 1f;

        /// <summary>At this speed and above an impact plays at full volume.</summary>
        public const float FullImpactSpeed = 7.5f;

        private const float QuietestVolume = 0.3f;
        private const float LoudestVolume = 0.85f;

        [SerializeField]
        [Tooltip("Which set of clips this object plays when it hits something.")]
        private ItemCollisionMaterial _material = ItemCollisionMaterial.Metal;

        private float _nextSoundTime;

        public ItemCollisionMaterial Material => _material;

        /// <summary>The volume of an impact at this speed: harder hits are louder.</summary>
        public static float VolumeForSpeed(float speed)
        {
            return Mathf.Lerp(QuietestVolume, LoudestVolume, Mathf.InverseLerp(MinImpactSpeed, FullImpactSpeed, speed));
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!InstanceFinder.IsServer)
            {
                return;
            }

            float speed = other.relativeVelocity.magnitude;
            if (speed > MinImpactSpeed && SubSystems.TryGet(out AudioSubSystem audio))
            {
                audio.TryPlayCollisionSound(_material, transform.position, VolumeForSpeed(speed), ref _nextSoundTime);
            }
        }
    }
}
