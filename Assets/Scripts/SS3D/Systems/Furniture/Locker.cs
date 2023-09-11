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
        [SyncVar(OnChange = nameof(OnLocked))] public bool Locked;
        [SerializeField, SyncVar] private IDPermission permissionToUnlock;
        public GameObject LockLight;
        private MaterialPropertyBlock propertyBlock;
        private Renderer _renderer;

        private void OnLocked(bool prev, bool next, bool asServer)
        {
            if(next)
            {
                propertyBlock.SetColor("_Color", Color.red);
                _renderer.SetPropertyBlock(propertyBlock);
            }
            else
            {
                propertyBlock.SetColor("_Color", Color.green);
                _renderer.SetPropertyBlock(propertyBlock);
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            propertyBlock = new MaterialPropertyBlock();
            _renderer = LockLight.GetComponent<Renderer>();
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            LockLockerInteraction lockLockerInteraction = 
                new LockLockerInteraction(this, permissionToUnlock);

            UnlockLockerInteraction unlockLockerInteraction = 
                new UnlockLockerInteraction(this, permissionToUnlock);

            return new IInteraction[] { lockLockerInteraction, unlockLockerInteraction };
        }
    }
}