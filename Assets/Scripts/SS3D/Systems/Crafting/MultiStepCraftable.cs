using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using UnityEngine;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using FishNet.Object.Synchronizing;
using SS3D.Logging;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// base class for game objects that need to apply custom things upon crafting, such as changing models, playing sounds...
    /// </summary>
    public abstract class MultiStepCraftable : NetworkActor, ICraftable
    {
        [SyncVar]
        protected string _currentStepName = "";

        public string CurrentStepName => _currentStepName;

        public abstract GameObject Craft(IInteraction interaction, InteractionEvent interactionEvent);

        public abstract void Modify(IInteraction interaction, InteractionEvent interactionEvent, string step);

        protected override void OnAwake()
        {
            if (!gameObject.TryGetComponent(out IWorldObjectAsset targetAssetReference))
            {
                Log.Error(this, $"{gameObject} has a ICraftable component but no IWorldObjectAsset component set up on them, add one or it'll cause trouble for multi step crafting");
                _currentStepName = "";
            }
            else
            {
                _currentStepName = targetAssetReference.Asset.Prefab.name;
            }
        }

    }
}
