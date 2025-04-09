using Coimbra;
using DG.Tweening;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Interactions;
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

        protected override void OnStart()
        {
            base.OnStart();

            if (LockLight != null)
            {
                _lightMaterial = LockLight.GetComponent<Renderer>().material;
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