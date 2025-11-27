using Coimbra;
using DG.Tweening;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using SS3D.Content.Systems.Interactions;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Interactions;
using SS3D.Traits;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// A temporary locker class for easily testing permission checking
    /// </summary>
    public class Locker : NetworkActor, IInteractionTarget
    {
        private static readonly int ColorPropertyIndex = Shader.PropertyToID("_Color");

        [SyncVar(OnChange = nameof(OnLocked))]
        private bool _isLocked;

        [SyncVar(OnChange = nameof(SyncIsOpen))]
        private bool _isOpen;

        [FormerlySerializedAs("Lockable")]
        [SerializeField]
        [SyncVar]
        [Header("Define if the locker is lockable")]
        private bool _lockable;

        [SerializeField]
        [SyncVar]
        [Header("Optional")]
        private IDPermission _permissionToUnlock;

        [SerializeField]
        private GameObject _door;

        [SerializeField]
        private Vector3 _doorChangePunch = new Vector3(-.1f, -.05f, 0);

        [FormerlySerializedAs("LockLight")]
        [CanBeNull]
        [Header("Optional")]
        [SerializeField]
        private GameObject _lockLight;

        private Material _lightMaterial;

        public bool Lockable => _lockable;

        public bool IsLocked
        {
            get => _isLocked;
            set => _isLocked = value;
        }

        public bool IsOpen => _isOpen;

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = ListPool.Pop<IInteraction>();

            LockLockerInteraction lockLockerInteraction = new(this, _permissionToUnlock);
            UnlockLockerInteraction unlockLockerInteraction = new(this, _permissionToUnlock);

            SimpleInteraction lockerDoorInteraction = new()
            {
                Name = _isOpen ? "Close locker" : "Open locker", Interact = OpenOrClose, CanInteractCallback = CanOpenOrClose, RangeCheck = true,
            };

            interactions.Add(lockLockerInteraction);
            interactions.Add(unlockLockerInteraction);
            interactions.Add(lockerDoorInteraction);

            IInteraction[] targetInteractions = interactions.ToArray();
            ListPool.Push(interactions);

            return targetInteractions;
        }

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => TryGetInteractionPoint(source, out point);

        protected override void OnStart()
        {
            base.OnStart();

            if (_lockLight != null)
            {
                _lightMaterial = _lockLight.GetComponent<Renderer>().material;
            }
        }

        private void SyncIsOpen(bool prev, bool next, bool asServer)
        {
            if (asServer)
            {
                return;
            }

            bool isOpen = next;

            DOTween.Kill(_door.transform);
            DOTween.Kill(transform, true);

            Vector3 doorRotation = _door.transform.localEulerAngles;

            // end value
            doorRotation = new Vector3(doorRotation.x, isOpen ? 130 : 0, doorRotation.z);
            Vector3 doorChangePunch = new Vector3(_doorChangePunch.x, isOpen ? -_doorChangePunch.y : _doorChangePunch.y, _doorChangePunch.z);

            transform.DOPunchScale(doorChangePunch, .25f).SetEase(Ease.OutExpo);
            _door.transform.DOLocalRotate(doorRotation, .45f).SetEase(Ease.OutExpo);
        }

        private void OnLocked(bool prev, bool next, bool asServer)
        {
            if (_lockLight == null)
            {
                return;
            }

            DOTween.Kill(_lightMaterial);

            _lightMaterial.DOColor(next ? Color.red : Color.green, ColorPropertyIndex, 0.25f);
        }

        private bool CanOpenOrClose(InteractionEvent interactionEvent) => !_isLocked && InteractionExtensions.RangeCheck(interactionEvent);

        private void OpenOrClose(InteractionEvent interactionEvent, InteractionReference interactionReference)
        {
            _isOpen = !_isOpen;
        }
    }
}
