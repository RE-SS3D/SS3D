using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Entities.Humanoid
{
	public class Ragdoll : MonoBehaviour
	{
		public Transform ArmatureRoot;
		private Transform _character;
		/// <summary>
		/// Transform, that character is positioned on during ragdoll state
		/// </summary>
		private Transform _center;
		private Animator _animator;
		private HumanoidLivingController _humanoidLivingController;
		private CharacterController _characterController;
		private Rigidbody[] _ragdollParts;
		private float _knockDownEnd;
		private float _elapsedResetBonesTime;
		/// <summary>
		/// If knockdown is going to expire
		/// </summary>
		private bool _isKnockDownTimed;
		public bool IsKnockedDown { get; private set; }
		[SerializeField]
		private string _standUpClipName;
		[SerializeField]
		private string _standUpStateName;

		private enum RagdollState
		{
			Walking,
			Enabled,
			StandingUp,
			BonesReset
		}

		private RagdollState _currentState;

		private class BoneTransform
		{
			public Transform Bone;
			public Vector3 Position;
			public Quaternion Rotation;
		}

		private BoneTransform[] _standUpBones;
		private BoneTransform[] _ragdollBones;
		private Transform[] _bones;
		private float _timeToResetBones = 0.5f;

		private void Start()
		{
			_character = ArmatureRoot.parent;
			_center = ArmatureRoot.GetChild(0);
			_animator = _character.GetComponent<Animator>();
			_humanoidLivingController = _character.GetComponent<HumanoidLivingController>();
			_characterController = _character.GetComponent<CharacterController>();

			_ragdollParts = (from part in _character.GetComponentsInChildren<RagdollPart>() select part.transform.GetComponent<Rigidbody>()).ToArray();
			ToggleKinematic(true);
			_currentState = RagdollState.Walking;

			_bones = (from part in _character.GetComponentsInChildren<RagdollPart>() select part.transform.GetComponent<Transform>()).ToArray();
			_standUpBones = new BoneTransform[_bones.Length];
			_ragdollBones = new BoneTransform[_bones.Length];

			for (int boneIndex = 0; boneIndex < _bones.Length; boneIndex++)
			{
				_standUpBones[boneIndex] = new();
				_ragdollBones[boneIndex] = new();
			}
		}

		private void Update()
		{
			
			if (Input.GetKeyDown(KeyCode.Y))
			{
				Knockdown(1f);
			}
			
			switch (_currentState)
			{
				case RagdollState.Walking:
					WalkingBehavior();
					break;
				case RagdollState.Enabled:
					EnabledBehavior();
					break;
				case RagdollState.StandingUp:
					StandingUpBehavior();
					break;
				case RagdollState.BonesReset:
					BonesResetBehavior();
					break;
			}
		}

		private void WalkingBehavior()
		{
			
		}

		private void EnabledBehavior()
		{
			AlignPositionToHips();
			if ((IsKnockedDown) && (Time.time > _knockDownEnd) && (_isKnockDownTimed))
			{
				BonesReset();
			}
		}

		private void StandUp()
		{
			_animator.enabled = true;
			_currentState = RagdollState.StandingUp;
			_animator.Play(_standUpStateName);
		}

		private void StandingUpBehavior()
		{
			if (_animator.GetCurrentAnimatorStateInfo(0).IsName(_standUpStateName) == false)
			{
				_currentState = RagdollState.Walking;
				Recover();
			}
		}
		private void BonesReset()
		{
			PopulateBoneTransforms(_ragdollBones);
			_currentState = RagdollState.BonesReset;
			// This is important, because otherwise the character will fly away after disabling its animator
			ToggleKinematic(true);
			_elapsedResetBonesTime = 0;
			PopulateStandUpBoneTransforms(_standUpClipName);
		}

		private void BonesResetBehavior()
		{
			_elapsedResetBonesTime += Time.deltaTime;
			float elapsedPercentage = _elapsedResetBonesTime / _timeToResetBones;
			for (int boneIndex = 0; boneIndex < _bones.Length; boneIndex++)
			{
				_bones[boneIndex].position = Vector3.Lerp(_ragdollBones[boneIndex].Position, _standUpBones[boneIndex].Position, elapsedPercentage);
				_bones[boneIndex].rotation = Quaternion.Lerp(_ragdollBones[boneIndex].Rotation, _standUpBones[boneIndex].Rotation, elapsedPercentage);
			}
			if (elapsedPercentage >= 1)
			{
				StandUp();
			}

		}
		/// <summary>
		/// Knockdown the character.
		/// </summary>
		public void KnockdownTimeless()
		{
			_isKnockDownTimed = false;
			Knockdown();
		}

		/// <summary>
		/// Knockdown the character for some time.
		/// </summary>
		/// <param name="seconds"></param>
		public void Knockdown(float seconds)
		{
			if (!IsKnockedDown)
			{
				_isKnockDownTimed = true;
				_knockDownEnd = Time.time + seconds;
				Knockdown();
			}
			else
			{
				_knockDownEnd = Math.Max(_knockDownEnd, Time.time + seconds);
			}
		}
		private void Knockdown()
		{
			_humanoidLivingController.enabled = false;
			_characterController.enabled = false;
			_animator.enabled = false;
			Vector3 movement = _humanoidLivingController.TargetMovement * 3;
			ToggleKinematic(false);
			foreach (Rigidbody part in _ragdollParts)
			{
				part.AddForce(movement, ForceMode.VelocityChange);
			}

			IsKnockedDown = true;
			_currentState = RagdollState.Enabled;
		}

		public void Recover()
		{
			_characterController.enabled = true;
			_humanoidLivingController.enabled = true;
			IsKnockedDown = false;
		}

		private void AlignPositionToHips()
		{
			Vector3 originalHipsPosition = _center.position;
			Vector3 positionOffset = _standUpBones[0].Position;
			//positionOffset.y = 0;
			_character.position = _center.position;
			
			Quaternion originalHipsRotation = _center.rotation;
			Vector3 desiredDirection = _center.up * -1;
			desiredDirection.y = 0;
			desiredDirection.Normalize();
			transform.rotation *= Quaternion.FromToRotation(transform.forward, desiredDirection);
			_center.position = originalHipsPosition;
			_center.rotation = originalHipsRotation;
		}

		private void ToggleKinematic(bool isKinematic)
		{
			foreach (Rigidbody part in _ragdollParts)
			{
				part.isKinematic = isKinematic;
			}
		}

		private void PopulateBoneTransforms(BoneTransform[] boneTransforms)
		{
			for (int boneIndex = 0; boneIndex < _bones.Length; boneIndex++)
			{
				boneTransforms[boneIndex].Position = _bones[boneIndex].position;
				boneTransforms[boneIndex].Rotation = _bones[boneIndex].rotation;
				boneTransforms[boneIndex].Bone = _bones[boneIndex];
			}
		}

		private void PopulateStandUpBoneTransforms(string clipName)
		{
			Vector3 positionBeforeSampling = transform.position;
			Quaternion rotationBeforeSampling = transform.rotation;
			foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
			{
				if (clip.name == clipName)
				{
					clip.SampleAnimation(gameObject, 0f);
					PopulateBoneTransforms(_standUpBones);
					break;
				}
			}

			transform.position = positionBeforeSampling;
			transform.rotation = rotationBeforeSampling;
		}
	}
}