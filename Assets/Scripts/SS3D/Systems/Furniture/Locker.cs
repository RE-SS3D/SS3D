using Coimbra;
using DG.Tweening;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Interactions;
using SS3D.Systems.Selection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Station locker: door open/close + optional ID lock. Storage UI is gated on the door —
    /// view/store only while open; closing the door closes any open storage panels.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class Locker : NetworkActor, IInteractionTarget, IStorageAccessGate
    {
        private static readonly int ColorPropertyIndex = Shader.PropertyToID("_Color");

        [FormerlySerializedAs("Locked")]
        [SyncVar(OnChange = nameof(OnLocked))]
        public bool IsLocked;

        [SyncVar(OnChange = nameof(SyncIsOpen))]
        public bool IsOpen;

        [FormerlySerializedAs("_lockable")]
        [SerializeField]
        [SyncVar]
        [Header("Define if the locker is lockable")]
        public bool Lockable;

        [SerializeField]
        [SyncVar]
        [Header("Optional")]
        private IDPermission permissionToUnlock;

        [SerializeField]
        private GameObject _door;

        [SerializeField]
        private Vector3 _doorChangePunch = new Vector3(-.1f, -.05f, 0);

        [CanBeNull]
        [Header("Optional")]
        public GameObject LockLight;

        private Material _lightMaterial;

        /// <inheritdoc />
        public bool AllowsStorageAccess => IsOpen;

        protected override void OnStart()
        {
            base.OnStart();

            if (LockLight != null)
            {
                _lightMaterial = LockLight.GetComponent<Renderer>().material;
            }
        }

        /// <summary>
        /// Server: close storage UI for every <see cref="ContainerViewer"/> still showing this locker's
        /// containers. Called when the door closes so panels cannot outlive the open door.
        /// </summary>
        [Server]
        public void CloseStorageUIs()
        {
            AttachedContainer[] containers = GetComponentsInChildren<AttachedContainer>();
            if (containers.Length == 0)
            {
                return;
            }

            ContainerViewer[] viewers = FindObjectsByType<ContainerViewer>(FindObjectsSortMode.None);
            foreach (AttachedContainer container in containers)
            {
                if (!container.HasUi)
                {
                    continue;
                }

                foreach (ContainerViewer viewer in viewers)
                {
                    if (viewer != null && viewer.HasContainer(container))
                    {
                        viewer.CloseContainerUI(container);
                    }
                }
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
            if (LockLight == null)
            {
                return;
            }

            DOTween.Kill(_lightMaterial);

            _lightMaterial.DOColor(next ? Color.red : Color.green, ColorPropertyIndex, 0.25f);
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = ListPool.Pop<IInteraction>();

            LockLockerInteraction lockLockerInteraction = new(this, permissionToUnlock);
            UnlockLockerInteraction unlockLockerInteraction = new(this, permissionToUnlock);

            LockerDoorInteraction lockerDoorInteraction = new(this)
            {
                Name = IsOpen ? "Close Locker" : "Open Locker",
            };

            interactions.Add(lockLockerInteraction);
            interactions.Add(unlockLockerInteraction);

            interactions.Add(lockerDoorInteraction);

            IInteraction[] targetInteractions = interactions.ToArray();
            ListPool.Push(interactions);

            return targetInteractions;
        }
    }
}