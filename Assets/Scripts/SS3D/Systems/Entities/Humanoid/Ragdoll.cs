using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
	public class Ragdoll : NetworkBehaviour
	{
		public Transform ArmatureRoot;
		private Transform _character;
		/// <summary>
		/// Transform, that character is positioned on during ragdoll state
		/// </summary>
		private Transform _hips;
		private Animator _animator;
		private HumanoidLivingController _humanoidLivingController;
		private CharacterController _characterController;
		private Transform[] _ragdollParts;
		private float _elapsedResetBonesTime;
		/// <summary>
		/// If knockdown is going to expire
		/// </summary>
        [SyncVar]
        private bool _isKnockdownTimed;
        [SyncVar]
        private float _knockdownTimer;
        [SyncVar(OnChange = nameof(Test))]
        public bool IsKnockedDown;
		[SerializeField]
		private string _standUpFaceUpStateName;
		[SerializeField]
		private string _standUpFaceDownStateName;
		private bool _isFacingDown;
		[SerializeField]
		private AnimationClip _standUpFaceUpClip;
		[SerializeField]
		private AnimationClip _standUpFaceDownClip;

		private void Test(bool prev, bool next, bool asServer)
		{
			UnityEngine.Debug.Log("New _isKnockedDown: " + next + " Prev: " + prev + " isServer: " + IsServer);
			if (next)
			{
				Knockdown();
			}
			else
			{
				Recover();
			}
		}

		private enum RagdollState
		{
			Walking,
			Ragdoll,
			StandingUp,
			BonesReset
		}

		private RagdollState _currentState;

		private class BoneTransform
		{
			public Vector3 Position;
			public Quaternion Rotation;
		}

		private BoneTransform[] _standUpBones;
		private BoneTransform[] _ragdollBones;
		private float _timeToResetBones = 0.5f;

		private void Start()
		{
			_knockdownTimer = 0;
			_character = ArmatureRoot.parent;
			_hips = ArmatureRoot.GetChild(0);
			_animator = _character.GetComponent<Animator>();
			_humanoidLivingController = _character.GetComponent<HumanoidLivingController>();
			_characterController = _character.GetComponent<CharacterController>();
			_ragdollParts = (from part in _character.GetComponentsInChildren<RagdollPart>() select part.transform.GetComponent<Transform>()).ToArray();
			_standUpBones = new BoneTransform[_ragdollParts.Length];
			_ragdollBones = new BoneTransform[_ragdollParts.Length];
			for (int boneIndex = 0; boneIndex < _ragdollParts.Length; boneIndex++)
			{
				_standUpBones[boneIndex] = new();
				_ragdollBones[boneIndex] = new();
			}
			
			_currentState = RagdollState.Walking;
			ToggleKinematic(true);
		}
		

		private void Update()
		{
			if ((Input.GetKeyDown(KeyCode.Y)) && (IsOwner))
			{
				Knockdown(1);
			}
			
			switch (_currentState)
			{
				case RagdollState.Walking:
					WalkingBehavior();
					break;
				case RagdollState.Ragdoll:
					RagdollBehavior();
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

		private void RagdollBehavior()
		{
			AlignToHips();
			if (_isKnockdownTimed)
			{
				_knockdownTimer -= Time.deltaTime;
				if ((IsKnockedDown) && (_knockdownTimer <= 0))
				{
					BonesReset();
				}
			}
		}
		private void AlignToHips()
		{
			Vector3 originalHipsPosition = _hips.position;
			Vector3 newPosition = _hips.position;
			if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo))
			{
				newPosition.y = hitInfo.point.y + 0.0051f;
			}
			_character.position = newPosition;
			_hips.position = originalHipsPosition;
			
			_isFacingDown = _hips.transform.forward.y < 0;
			Vector3 desiredDirection = _hips.up * (_isFacingDown ? 1 : -1);
			desiredDirection.y = 0;
			desiredDirection.Normalize();
			Quaternion originalHipsRotation = _hips.rotation;
			transform.rotation *= Quaternion.FromToRotation(transform.forward, desiredDirection);
			_hips.rotation = originalHipsRotation;
		}

		private void BonesReset()
		{
			// This is important, because otherwise the character will fly away after disabling its animator
			ToggleKinematic(true);
			_elapsedResetBonesTime = 0;
			_currentState = RagdollState.BonesReset;

			PopulateBoneTransforms(_ragdollBones);
			PopulateStandUpBoneTransforms(_isFacingDown ? _standUpFaceDownClip : _standUpFaceUpClip);
			BonesResetBehavior();
		}

		private void PopulateStandUpBoneTransforms(AnimationClip animationClip)
		{
			Vector3 armaturePositionBefore = ArmatureRoot.localPosition;
			Quaternion armatureRotationBefore = ArmatureRoot.localRotation;
			animationClip.SampleAnimation(gameObject, 0f);
			Vector3 hipsPositionBefore = _hips.position;
			Quaternion hipsRotationBefore = _hips.rotation;
			ArmatureRoot.localPosition = armaturePositionBefore;
			ArmatureRoot.localRotation = armatureRotationBefore;
			_hips.position = hipsPositionBefore;
			_hips.rotation = hipsRotationBefore;
			PopulateBoneTransforms(_standUpBones);
		}

		private void PopulateBoneTransforms(BoneTransform[] boneTransforms)
		{
			for (int boneIndex = 0; boneIndex < _ragdollParts.Length; boneIndex++)
			{
				boneTransforms[boneIndex].Position = _ragdollParts[boneIndex].localPosition;
				boneTransforms[boneIndex].Rotation = _ragdollParts[boneIndex].localRotation;
			}
		}

		private void BonesResetBehavior()
		{
			_elapsedResetBonesTime += Time.deltaTime;
			float elapsedPercentage = _elapsedResetBonesTime / _timeToResetBones;
			for (int partIndex = 0; partIndex < _ragdollParts.Length; partIndex++)
			{
				_ragdollParts[partIndex].localPosition = Vector3.Lerp(_ragdollBones[partIndex].Position, _standUpBones[partIndex].Position, elapsedPercentage);
				_ragdollParts[partIndex].localRotation = Quaternion.Lerp(_ragdollBones[partIndex].Rotation, _standUpBones[partIndex].Rotation, elapsedPercentage);
			}
			if (elapsedPercentage >= 1)
			{
				StandUp();
			}
		}

		private void StandUp()
		{
			UnityEngine.Debug.Log("Standup called isServer " + IsServer);
			_animator.enabled = true;
			_currentState = RagdollState.StandingUp;
			_animator.Play(_isFacingDown ? _standUpFaceDownStateName : _standUpFaceUpStateName, 0, 0);
		}

		private void StandingUpBehavior()
		{
			string standUpName = (_isFacingDown ? _standUpFaceDownClip : _standUpFaceUpClip).name;
			if (_animator.GetCurrentAnimatorStateInfo(0).IsName(standUpName) == false)
			{
				Recover();
			}
		}

		/// <summary>
		/// Knockdown the character.
		/// </summary>
		public void KnockdownTimeless()
		{
			_isKnockdownTimed = false;
			Knockdown();
		}

		/// <summary>
		/// Knockdown the character for some time.
		/// </summary>
		/// <param name="seconds"></param>
		public void Knockdown(float seconds)
		{
			_isKnockdownTimed = true;
			_knockdownTimer += seconds;
			Knockdown();
		}

		private void Knockdown()
		{
			Vector3 movement = _humanoidLivingController.TargetMovement * 3;
			ToggleKinematic(false);
			foreach (Transform part in _ragdollParts)
			{
				part.GetComponent<Rigidbody>().AddForce(movement, ForceMode.VelocityChange);
			}
			_animator.SetFloat("Speed", 0);
			_currentState = RagdollState.Ragdoll;
			_humanoidLivingController.enabled = false;
			_characterController.enabled = false;
			_animator.enabled = false;
			IsKnockedDown = true;
		}

		public void Recover()
		{
			UnityEngine.Debug.Log("Recover isServer" + " " + IsServer);
			_characterController.enabled = true;
			_humanoidLivingController.enabled = true;
			_animator.enabled = true;
			_knockdownTimer = 0;
			IsKnockedDown = false;
			_currentState = RagdollState.Walking;
		}

		private void ToggleKinematic(bool isKinematic)
		{
			foreach (Transform part in _ragdollParts)
			{
				part.GetComponent<Rigidbody>().isKinematic = isKinematic;
			}
		}
	}
}