using DG.Tweening;
using FishNet.Component.Animating;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Animations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Component for character's gameobject, that controlls ragdoll
    /// </summary>
    public class Ragdoll : NetworkBehaviour
    {
        public event Action<bool> OnRagdoll;

        /// <summary>
        /// Determines how much higher than the lowest point character will be during AlignToHips(). This var prevent character from getting stuck in the floor
        /// </summary>
        private const float AlignmentYDelta = 0.0051f;

        [SerializeField]
        private Collider _characterCollider;

        [SerializeField]
        private Rigidbody _characterRigidBody;

        [SerializeField]
        private Transform _hips;

        [SerializeField]
        private Transform _character;

        [SerializeField]
        private byte _ragdollPartSyncInterval;

        [SerializeField]
        private PositionController _positionController;

        private Transform[] _ragdollParts;

        private NetworkConnection _owner;

        public bool IsFacingDown { get; private set; }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _ragdollParts = (from part in GetComponentsInChildren<RagdollPart>() select part.transform.GetComponent<Transform>()).ToArray();

            // All rigid bodies are kinematic at start, only the owner should be able to change that afterwards.
            SetRagdollPhysic(false);
            _positionController.OnChangedPosition += HandleChangedPosition;
        }

        [Server]
        public void KnockDown(float time)
        {
            KnockDown();
            Invoke(nameof(Recover), time);
        }

        [Server]
        public void KnockDown()
        {
            SetRagdollPhysic(true);
            StartCoroutine(AlignToHips());
        }

        [Server]
        public void Recover()
        {
            SetRagdollPhysic(false);
        }

        [Server]
        public void AddForceToAllParts(Vector3 force)
        {
            foreach (Transform part in _ragdollParts)
            {
                part.GetComponent<Rigidbody>().AddForce(force, ForceMode.VelocityChange);
            }
        }

        [Server]
        private void HandleChangedPosition(PositionType position, float recoverTime)
        {
            if (position == PositionType.ResetBones)
            {
                SetRagdollPhysic(false);
            }
        }

        /// <summary>
        /// Adjust player's position and rotation. Character's x and z coords equals hips coords, y is at lowest position.
        /// Character's y rotation is aligned with hips forwards direction.
        /// </summary>
        [Server]
        private IEnumerator AlignToHips()
        {
            while (_positionController.PositionType == PositionType.Ragdoll)
            {
                IsFacingDown = _hips.transform.forward.y < 0;
                Vector3 originalHipsPosition = _hips.position;
                Vector3 newPosition = _hips.position;

                // Get the lowest position
                if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo))
                {
                    newPosition.y = hitInfo.point.y + AlignmentYDelta;
                }

                _character.position = newPosition;
                _hips.position = originalHipsPosition;

                /// TODO : this part mess up with something on client, need to figure that out. Creates lots of jitter.

               /* Vector3 desiredDirection = _hips.up * (IsFacingDown ? 1 : -1);
                desiredDirection.y = 0;
                desiredDirection.Normalize();
                Quaternion originalHipsRotation = _hips.rotation;
                Vector3 rotationDifference = Quaternion.FromToRotation(transform.forward, desiredDirection).eulerAngles;

                // Make sure that rotation is only around Y axis
                rotationDifference.x = 0;
                rotationDifference.z = 0;
                transform.rotation *= Quaternion.Euler(rotationDifference);
                _hips.rotation = originalHipsRotation; */

                yield return null;
            }
        }

        [Server]
        private void SetRagdollPhysic(bool isOn)
        {
            UnityEngine.Debug.Log($"Owner is {Owner}");

            // Managing ownership here is necessary as ragdoll is fully server handled, but player movement is client authoritative.
            // This should be done in an ownership manager or something, and eventually, player movement should be server auth.
            if (isOn)
            {
                _owner = Owner;
                RemoveOwnership();
            }
            else if (_owner != null)
            {
                GiveOwnership(_owner);
            }

            _characterRigidBody.isKinematic = isOn;
            _characterCollider.isTrigger = isOn;

            foreach (Transform part in _ragdollParts)
            {
                part.GetComponent<Rigidbody>().isKinematic = !isOn;
                part.GetComponent<Collider>().isTrigger = !isOn;

                part.GetComponent<NetworkTransform>().SetInterval(_ragdollPartSyncInterval);
                part.GetComponent<NetworkTransform>().SetSynchronizePosition(isOn);
                part.GetComponent<NetworkTransform>().SetSynchronizeRotation(isOn);
            }

            OnRagdoll?.Invoke(isOn);
        }
    }
}
