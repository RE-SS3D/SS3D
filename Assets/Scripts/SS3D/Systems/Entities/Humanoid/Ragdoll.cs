using System;
using System.Collections;
using System.Collections.Generic;
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
		/// <summary>
		/// If knockdown is going to expire
		/// </summary>
		private bool _isKnockDownTimed;
		public bool IsKnockedDown { get; private set; }

		private void Start()
		{
			IsKnockedDown = false;
			_character = ArmatureRoot.parent;
			_center = ArmatureRoot.GetChild(0);
			_animator = _character.GetComponent<Animator>();
			_humanoidLivingController = _character.GetComponent<HumanoidLivingController>();
			_characterController = _character.GetComponent<CharacterController>();
			
			List<Rigidbody> ragdollParts = new();
			foreach (RagdollPart part in _character.GetComponentsInChildren<RagdollPart>())
			{
				ragdollParts.Add(part.transform.GetComponent<Rigidbody>());
			}
			_ragdollParts = ragdollParts.ToArray();
			ToggleKinematic(true);
		}

		private void OnDisable()
		{
			Recover();
		}

		private void Update()
		{
			if (IsKnockedDown)
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

			if ((IsKnockedDown) && (Time.time > _knockDownEnd) && (_isKnockDownTimed))
			{
				// Knockdown expired
				Recover();
				_isKnockDownTimed = false;
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
		}

		public void Recover()
		{
			_animator.enabled = true;
			_characterController.enabled = true;
			_humanoidLivingController.enabled = true;
			IsKnockedDown = false;
			// This is important, because otherwise the character will fly away after disabling its animator
			ToggleKinematic(true);
			_animator.Play("Getting Up");
		}

		private void AlignPositionToHips()
		{
			Vector3 originalHipsPosition = _center.position;
			_character.position = _center.position;
			_center.position = originalHipsPosition;
		}

		private void ToggleKinematic(bool isKinematic)
		{
			foreach (Rigidbody part in _ragdollParts)
			{
				part.isKinematic = isKinematic;
			}
		}
	}
}