using FishNet.Component.Animating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
	public class Ragdoll : NetworkBehaviour
	{
		public Transform ArmatureRoot;
        private Transform _hips;
        private Transform _character;
		private Animator _animator;
		private NetworkAnimator _networkAnimator;
		private HumanoidLivingController _humanoidLivingController;
		private CharacterController _characterController;
		private Transform[] _ragdollParts;
        private bool _isKnockdownTimed;
        private float _knockdownTimer;
		[SyncVar(OnChange = nameof(OnSyncKnockdown))]
		public bool IsKnockedDown;
        [field: SyncVar(OnChange = nameof(OnSyncStandingUp))]
        public bool IsStandingUp { get; [ServerRpc] set; }
        private bool _isFacingDown;
        private enum RagdollState
        {
            Walking,
            Ragdoll,
            BonesReset,
            StandingUp
        }
        private RagdollState _currentState;
        private class BoneTransform
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }
        private BoneTransform[] _standUpBones;
        private BoneTransform[] _ragdollBones;
        [SerializeField]
        private AnimationClip _standUpFaceUpClip;
        [SerializeField]
        private AnimationClip _standUpFaceDownClip;
        private float _elapsedResetBonesTime;
        private float _timeToResetBones = 0.5f;

		private void OnSyncKnockdown(bool prev, bool next, bool asServer)
		{
			if (prev == next) return;

			// As the non owner, simply disable animating and controlling components to avoid jittering
			// or reactivate them if not knowckdown anymore.
			if (next)
			{
                if (!IsOwner)
                {
                    EnableAnimatorAndController(false);
                    return;
                }
                _currentState = RagdollState.Ragdoll;
                Knockdown();
            }
			else
            {
                _currentState = RagdollState.Walking;
				BonesReset();
			}
		}

        private void OnSyncStandingUp(bool prev, bool next, bool asServer)
        {
            if ((prev == next) || IsOwner) return;

            if (next)
            {
                _isFacingDown = _hips.transform.forward.y < 0;
                _animator.enabled = true;
                _networkAnimator.enabled = true;
                _animator.Play(_isFacingDown ? _standUpFaceDownClip.name : _standUpFaceUpClip.name, 0, 0);
            }
            else
            {
                _humanoidLivingController.enabled = true;
                _characterController.enabled = true;
            }
        }

		/// <summary>
		/// On Start network to set up things both on clients and host, only once. 
		/// </summary>
		public override void OnStartNetwork()
		{
			base.OnStartNetwork();

			_animator = GetComponent<Animator>();
			_humanoidLivingController = GetComponent<HumanoidLivingController>();
			_characterController = GetComponent<CharacterController>();
			_networkAnimator = GetComponent<NetworkAnimator>();
            _knockdownTimer = 0;
            _hips = ArmatureRoot.GetChild(0);
            _character = ArmatureRoot.parent;
            if (IsClient)
            {
                _currentState = RagdollState.Walking;
            }

            _ragdollParts = (from part in GetComponentsInChildren<RagdollPart>() select part.transform.GetComponent<Transform>()).ToArray();
            _standUpBones = new BoneTransform[_ragdollParts.Length];
            _ragdollBones = new BoneTransform[_ragdollParts.Length];

            for (int boneIndex = 0; boneIndex < _ragdollParts.Length; boneIndex++)
            {
                _standUpBones[boneIndex] = new();
                _ragdollBones[boneIndex] = new();
            }
            // All rigid bodies get kinematic, only the owner should be able to change that afterwards.
			ToggleKinematic(true);
		}
        
		private void Update()
		{
            if (IsServer && _isKnockdownTimed && IsKnockedDown)
            {
                _knockdownTimer -= Time.deltaTime;
                if (_knockdownTimer <= 0)
                {
                    Recover();
                }
            }
			// Only owners gets to control their ragdoll through pushing buttons.
			if (!IsOwner) return;

			if (Input.GetKeyDown(KeyCode.Y))
			{
                Knockdown(1f);
			}
			if (Input.GetKeyDown(KeyCode.V))
			{
				RandomlyKick();
			}
            switch (_currentState)
            {
                case RagdollState.Walking:
                    WalkingBehavior();
                    break;
                case RagdollState.Ragdoll:
                    RagdollBehavior();
                    break;
                case RagdollState.BonesReset:
                    BonesResetBehavior();
                    break;
                case RagdollState.StandingUp:
                    StandingUpBehavior();
                    break;
            }
		}

        private void WalkingBehavior() { }

        private void RagdollBehavior()
        {
            if (IsOwner)
            {
                AlignToHips();
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
            _currentState = RagdollState.BonesReset;
            // This is important, because otherwise the character will fly away after disabling its animator
            ToggleKinematic(true);
            _elapsedResetBonesTime = 0;
            PopulateBoneTransforms(_ragdollBones);
            PopulateStandUpBoneTransforms(_isFacingDown ? _standUpFaceDownClip : _standUpFaceUpClip);
            BonesResetBehavior();
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
            _animator.enabled = true;
            _networkAnimator.enabled = true;
            _currentState = RagdollState.StandingUp;
            _animator.Play(_isFacingDown ? _standUpFaceDownClip.name : _standUpFaceUpClip.name, 0, 0);
            IsStandingUp = true;
        }

        private void StandingUpBehavior()
        {
            string standUpName = (_isFacingDown ? _standUpFaceDownClip : _standUpFaceUpClip).name;
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName(standUpName) == false)
            {
                Walk();
            }
        }

        private void Walk()
        {
            IsStandingUp = false;
            _characterController.enabled = true;
            _humanoidLivingController.enabled = true;
            _currentState = RagdollState.Walking;
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
        [ServerRpc]
        public void KnockdownTimeless()
        {
            _isKnockdownTimed = false;
            IsKnockedDown = true;
        }
        /// <summary>
        /// Knockdown the character for some time.
        /// </summary>
        /// <param name="seconds"></param>
        [ServerRpc]
        public void Knockdown(float seconds)
        {
            _isKnockdownTimed = true;
            _knockdownTimer += seconds;
            IsKnockedDown = true;
        }

        [ServerRpc]
        public void Recover()
        {
            IsKnockedDown = false;
            _knockdownTimer = 0;
        }

		/// <summary>
		/// Movement is client authoritative,
		/// so only call that on the owner, since it handles adding forces to body parts.
		/// </summary>
		[Client]
		private void Knockdown()
		{
			if (!IsOwner) return;
			Vector3 movement = _humanoidLivingController.TargetMovement * 3;
			EnableAnimatorAndController(false);
			ToggleKinematic(false);
			foreach (Transform part in _ragdollParts)
			{
				part.GetComponent<Rigidbody>().AddForce(movement, ForceMode.VelocityChange);
			}
		}

		/// <summary>
		/// Toggle kinematic to true for all clients, then only the owner should toggle it to false,
		/// since only the owner handle the physics.
		/// </summary>
		[ServerOrClient]
		private void ToggleKinematic(bool isKinematic)
		{
			if (!IsOwner && !isKinematic) return;
			foreach (Transform part in _ragdollParts)
			{
				part.GetComponent<Rigidbody>().isKinematic = isKinematic;
			}
		}

		/// <summary>
		/// When enable is false, simply disablecomponents that could mess with the other physics based components,
		/// to avoid jittering.
		/// </summary>
		[ServerOrClient]
		private void EnableAnimatorAndController(bool enable)
		{
			_humanoidLivingController.enabled = enable;
			_characterController.enabled = enable;
			_animator.enabled = enable;
			_networkAnimator.enabled = enable;
		}

		/// <summary>
		/// Debug method to show the ragdoll sync, randomly kick a body part in a random direction.
		/// </summary>
		private void RandomlyKick()
		{
			int parts = _ragdollParts.Count();
			int randPart = UnityEngine.Random.Range(0, parts);
			int randForceX = UnityEngine.Random.Range(-20, 20);
			int randForceY = UnityEngine.Random.Range(-20, 20);
			int randForceZ = UnityEngine.Random.Range(-20, 20);
			Vector3 randForce = new Vector3(randForceX,randForceY, randForceZ);
			_ragdollParts[randPart].GetComponent<Rigidbody>().AddForce(randForce, ForceMode.VelocityChange);
		}
	}
}