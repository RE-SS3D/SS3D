using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Blends a body's bones from the pose they hold (a ragdoll, or a frozen clip) into the first frame of an animator
    /// state, so <see cref="Ragdoll"/> can hand the body to that state without a cut: the get-up clips, and the Downed clip.
    /// </summary>
    internal sealed class RagdollPoseBlend
    {
        private struct BonePose
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        /// <summary>
        /// Every humanoid bone. Wider than the ragdoll parts: the chest, neck, hands and feet have no rigidbody but
        /// would otherwise snap.
        /// </summary>
        private readonly Transform[] _bones;
        private readonly BonePose[] _from;
        private readonly BonePose[] _to;
        private float _elapsed;

        public RagdollPoseBlend(Transform[] bones)
        {
            _bones = bones;
            _from = new BonePose[bones.Length];
            _to = new BonePose[bones.Length];
        }

        /// <summary>
        /// Start a blend from the bones' current pose into <paramref name="stateName"/>'s first frame. The clip's first
        /// frame does not lie on the root (face up, the hips start 0.68 m behind it), so the root shifts until that
        /// frame's hips land on the current hips; otherwise the blend drags the body across the floor.
        /// </summary>
        /// <param name="turnToMatch">Also turn the root about Y so the frame's head points where the body's head points.</param>
        public void Begin(Animator animator, Transform armatureRoot, Transform hips, Transform root, string stateName, int layer,
            bool turnToMatch)
        {
            _elapsed = 0f;
            Capture(_from);
            Vector3 currentHips = hips.position;
            Quaternion currentHipsRotation = hips.rotation;
            Vector3 currentHipsUp = hips.up;
            Vector3 targetHips = CaptureTarget(animator, armatureRoot, hips, stateName, layer, out Vector3 targetHipsUp);
            if (turnToMatch)
            {
                TurnRoot(root, currentHipsUp, targetHipsUp, ref targetHips);
            }

            Vector3 offset = currentHips - targetHips;
            offset.y = 0f;
            root.position += offset;
            // Moving the root moved the bones with it: put the hips back and record the start pose again.
            hips.position = currentHips;
            hips.rotation = currentHipsRotation;
            Capture(_from);
        }

        private static void TurnRoot(Transform root, Vector3 currentHipsUp, Vector3 targetHipsUp, ref Vector3 targetHips)
        {
            Vector3 from = Vector3.ProjectOnPlane(targetHipsUp, Vector3.up);
            Vector3 to = Vector3.ProjectOnPlane(currentHipsUp, Vector3.up);
            if (from.sqrMagnitude < 0.01f || to.sqrMagnitude < 0.01f)
            {
                return;
            }

            Quaternion turn = Quaternion.AngleAxis(Vector3.SignedAngle(from, to, Vector3.up), Vector3.up);
            Vector3 pivot = root.position;
            root.rotation = turn * root.rotation;
            targetHips = pivot + turn * (targetHips - pivot);
        }

        /// <summary>
        /// Evaluate <paramref name="stateName"/>'s first frame through the animator, record it as the end of the blend,
        /// then put the bones back in the start pose.
        /// </summary>
        /// <param name="layer">The state's layer: 0 for the get-up clips, Full Body Override for Downed.</param>
        /// <param name="hipsUp">World up axis of the hips in that frame (it points at the head when lying).</param>
        /// <returns>World position of the hips in that frame, for the current root.</returns>
        private Vector3 CaptureTarget(Animator animator, Transform armatureRoot, Transform hips, string stateName, int layer,
            out Vector3 hipsUp)
        {
            Vector3 originalArmaturePosition = armatureRoot.localPosition;
            Quaternion originalArmatureRotation = armatureRoot.localRotation;
            // Evaluate the state through the Animator. These are humanoid muscle clips:
            // AnimationClip.SampleAnimation leaves the bones in the avatar's T-pose, which the blend
            // then passed through. Only the sampled state's layer is weighted, so its pose alone lands on the bones.
            animator.enabled = true;
            for (int otherLayer = 1; otherLayer < animator.layerCount; otherLayer++)
            {
                animator.SetLayerWeight(otherLayer, otherLayer == layer ? 1f : 0f);
            }

            animator.Play(stateName, layer, 0f);
            animator.Update(0f);
            animator.enabled = false;
            Vector3 targetHipsPosition = hips.position;
            Quaternion targetHipsRotation = hips.rotation;
            hipsUp = hips.up;
            armatureRoot.localPosition = originalArmaturePosition;
            armatureRoot.localRotation = originalArmatureRotation;
            hips.position = targetHipsPosition;
            hips.rotation = targetHipsRotation;
            Capture(_to);
            Apply(0f);
            return targetHipsPosition;
        }

        /// <summary>Advance the blend and pose the bones. True once it has run for <paramref name="seconds"/>.</summary>
        public bool Tick(float deltaTime, float seconds)
        {
            _elapsed += deltaTime;
            Apply(Mathf.Clamp01(_elapsed / seconds));
            return _elapsed >= seconds;
        }

        /// <summary>Pose the bones <paramref name="t"/> of the way from the start to the target (0 to 1).</summary>
        public void Apply(float t)
        {
            for (int index = 0; index < _bones.Length; index++)
            {
                _bones[index].localPosition = Vector3.Lerp(_from[index].Position, _to[index].Position, t);
                _bones[index].localRotation = Quaternion.Lerp(_from[index].Rotation, _to[index].Rotation, t);
            }
        }

        private void Capture(BonePose[] poses)
        {
            for (int index = 0; index < _bones.Length; index++)
            {
                poses[index].Position = _bones[index].localPosition;
                poses[index].Rotation = _bones[index].localRotation;
            }
        }
    }
}
