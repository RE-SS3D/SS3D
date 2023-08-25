using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Entities.Humanoid
{
	public class Ragdoll : MonoBehaviour
	{
		[FormerlySerializedAs("_armatureRoot")]
		public Transform ArmatureRoot;
		private Transform _character;
		private Transform _center;
		private Animator _animator;
		private HumanoidLivingController _humanoidLivingController;
		private CharacterController _characterController;
		private List<Rigidbody> _ragdollParts;
		private float _knockdownEnd;
		private bool _isKnockedDown = false;

		private void Start()
		{
			_character = ArmatureRoot.parent;
			_center = ArmatureRoot.GetChild(0);
			_animator = _character.GetComponent<Animator>();
			_humanoidLivingController = _character.GetComponent<HumanoidLivingController>();
			_characterController = _character.GetComponent<CharacterController>();
			_ragdollParts = new();

			foreach (RagdollPart part in _character.GetComponentsInChildren<RagdollPart>())
			{
				_ragdollParts.Add(part.transform.GetComponent<Rigidbody>());
			}
			//Recover();
		}

		private void Update()
		{
			if (_isKnockedDown)
			{
				if (Input.GetKeyDown(KeyCode.Y))
				{
					Recover();
				}
				AlignPositionToHips();
			}
			else
			{
				if (Input.GetKeyDown(KeyCode.Y))
				{
					Knockdown(); 
				}
			}
			
		}

		private void Knockdown()
		{
			_humanoidLivingController.enabled = false;
			_characterController.enabled = false;
			_animator.enabled = false;
			Vector3 movement = _humanoidLivingController.TargetMovement * 3;
			foreach (Rigidbody part in _ragdollParts)
			{
				part.isKinematic = false;
				part.AddForce(movement, ForceMode.VelocityChange);
			}

			_isKnockedDown = true;
		}

		private void Recover()
		{
			foreach (Rigidbody part in _ragdollParts)
			{
				part.isKinematic = true;
			}
			_animator.enabled = true;
			_characterController.enabled = true;
			_humanoidLivingController.enabled = true;
			_isKnockedDown = false;
			_animator.Play("Getting Up");
		}

		private void AlignPositionToHips()
		{
			Vector3 originalHipsPosition = _center.position;
			_character.position = _center.position;
			_center.position = originalHipsPosition;
		}
	}
}