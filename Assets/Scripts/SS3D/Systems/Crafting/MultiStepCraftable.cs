using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// base class for game objects that need to apply custom things upon crafting, such as changing models, playing sounds...
    /// </summary>
    public abstract class MultiStepCraftable : NetworkActor, ICraftable
    {
        [SyncVar]
        private string _currentStepName = string.Empty;

        public string CurrentStepName { get; protected set; }

        public abstract GameObject Craft(IInteraction interaction, InteractionEvent interactionEvent);

        public abstract void Modify(IInteraction interaction, InteractionEvent interactionEvent, string step);

        protected override void OnAwake()
        {
            if (!gameObject.TryGetComponent(out IWorldObjectAsset targetAssetReference))
            {
                Log.Error(this, $"{gameObject} has a ICraftable component but no IWorldObjectAsset component set up on them, add one or it'll cause trouble for multi step crafting");
                _currentStepName = string.Empty;
            }
            else
            {
                _currentStepName = targetAssetReference.Asset.Prefab.name;
            }
        }
    }
}
