using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Interactions;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// A temporary locker class for easily testing permission checking
    /// </summary>
    public class Locker : NetworkActor, IInteractionTarget
    {
        [SyncVar(OnChange = nameof(OnLocked))]
        public bool Locked;

        public GameObject LockLight;

        [SerializeField]
        [SyncVar]
        private IDPermission _permissionToUnlock;

        private MaterialPropertyBlock _propertyBlock;
        private Renderer _renderer;

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            LockLockerInteraction lockLockerInteraction =
                new LockLockerInteraction(this, _permissionToUnlock);

            UnlockLockerInteraction unlockLockerInteraction =
                new UnlockLockerInteraction(this, _permissionToUnlock);

            return new IInteraction[] { lockLockerInteraction, unlockLockerInteraction };
        }

        protected override void OnStart()
        {
            base.OnStart();
            _propertyBlock = new MaterialPropertyBlock();
            _renderer = LockLight.GetComponent<Renderer>();
        }

        private void OnLocked(bool prev, bool next, bool asServer)
        {
            if (next)
            {
                _propertyBlock.SetColor("_Color", Color.red);
                _renderer.SetPropertyBlock(_propertyBlock);
            }
            else
            {
                _propertyBlock.SetColor("_Color", Color.green);
                _renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}