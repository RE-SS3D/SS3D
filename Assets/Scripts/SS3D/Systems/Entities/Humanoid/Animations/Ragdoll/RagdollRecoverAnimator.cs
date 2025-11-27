using DG.Tweening;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Systems.Animations;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.UI;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    public partial class RagdollRecoverAnimator : NetworkActor
    {
        private class BoneTransform
        {
            public BoneTransform(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            public Vector3 Position { get; set; }

            public Quaternion Rotation { get; set; }
        }

        [SerializeField]
        private PositionController _positionController;

        [SerializeField]
        private Transform _armatureRoot;

        private Transform[] _ragdollParts;

        [SerializeField]
        private Transform _character;

        [SerializeField]
        private Transform _hips;

        /// <summary>
        /// Bones Transforms (position and rotation) in the first frame of StandUp animation
        /// </summary>
        private BoneTransform[] _standUpBones;

        /// <summary>
        /// Bones Transforms (position and rotation) during the Ragdoll state
        /// </summary>
        private BoneTransform[] _ragdollBones;

        public override void OnStartClient()
        {
            base.OnStartClient();
            _ragdollParts = (from part in GetComponentsInChildren<RagdollPart>() select part.transform.GetComponent<Transform>()).ToArray();
            _standUpBones = new BoneTransform[_ragdollParts.Length];
            _ragdollBones = new BoneTransform[_ragdollParts.Length];

            for (int boneIndex = 0; boneIndex < _ragdollParts.Length; boneIndex++)
            {
                _standUpBones[boneIndex] = new(Vector3.zero, Quaternion.identity);
                _ragdollBones[boneIndex] = new(Vector3.zero, Quaternion.identity);
            }

            _positionController.OnChangedPosition += HandleChangedPosition;
        }

        [Client]
        private void HandleChangedPosition(PositionType position, float recoverTime)
        {
            if (position == PositionType.ResetBones)
            {
                AnimationClip recoverAnimation = _positionController.GetRecoverFromRagdollClip();
                BonesReset(recoverAnimation, recoverTime);
            }
        }

        /// <summary>
        /// Switch to BonesReset state and prepare for BonesResetBehavior
        /// </summary>
        [Client]
        private void BonesReset(AnimationClip clip, float timeToResetBones)
        {
            PopulateStandUpPartsTransforms(clip);
            for (int partIndex = 0; partIndex < _ragdollParts.Length; partIndex++)
            {
                _ragdollParts[partIndex].DOLocalMove(_standUpBones[partIndex].Position, timeToResetBones);
                _ragdollParts[partIndex].DOLocalRotate(_standUpBones[partIndex].Rotation.eulerAngles, timeToResetBones);
            }
        }

        /// <summary>
        /// Copy current ragdoll parts local positions and rotations to array.
        /// </summary>
        /// <param name="partsTransforms">Array, that receives ragdoll parts positions</param>
        [Client]
        private void PopulatePartsTransforms(BoneTransform[] partsTransforms)
        {
            for (int partIndex = 0; partIndex < _ragdollParts.Length; partIndex++)
            {
                partsTransforms[partIndex].Position = _ragdollParts[partIndex].localPosition;
                partsTransforms[partIndex].Rotation = _ragdollParts[partIndex].localRotation;
            }
        }

        /// <summary>
        /// Copy ragdoll parts position in first frame of StandUp animation to array
        /// </summary>
        /// <param name = "partsTransforms">Array, that receives ragdoll parts positions</param>
        /// <param name="animationClip"></param>
        [Client]
        private void PopulateStandUpPartsTransforms(AnimationClip animationClip)
        {
            // Copy into the _ragdollBones list the local position and rotation of bones, while in the current ragdoll position
            PopulatePartsTransforms(_ragdollBones);

            // Register some position and rotation before switching to the first frame of getting up animation
            Vector3 originalArmaturePosition = _armatureRoot.localPosition;
            Quaternion originalArmatureRotation = _armatureRoot.localRotation;
            Vector3 originalPosition = _character.position;
            Quaternion originalRotation = _character.rotation;

            // Put character into first frame of animation
            animationClip.SampleAnimation(gameObject, 0f);

            // Put back character at the right place as the animation moved it
            _character.SetPositionAndRotation(originalPosition, originalRotation);

            // Register hips position and rotation as moving the armature will messes with the hips position
            Vector3 originalHipsPosition = _hips.position;
            Quaternion originalHipsRotation = _hips.rotation;

            // Put back armature at the right place as the animation moved it
            _armatureRoot.localPosition = originalArmaturePosition;
            _armatureRoot.localRotation = originalArmatureRotation;

            // When moving the armature, it messes with the hips position, so we also move the hips back
            _hips.SetPositionAndRotation(originalHipsPosition, originalHipsRotation);

            // Populate the bones local position and rotation when in first frame of animation
            PopulatePartsTransforms(_standUpBones);

            // Move bones back to the ragdolled position they were in
            for (int partIndex = 0; partIndex < _ragdollParts.Length; partIndex++)
            {
                _ragdollParts[partIndex].localPosition = _ragdollBones[partIndex].Position;
                _ragdollParts[partIndex].localRotation = _ragdollBones[partIndex].Rotation;
            }
        }
    }

    public partial class RagdollRecoverAnimator : IPlayInteractionAnimation
    {
        public void PlayAnimation(InteractionType interactionType)
        {
            throw new System.NotImplementedException();
        }

        public void StopAnimation()
        {
            throw new System.NotImplementedException();
        }
    }
}
