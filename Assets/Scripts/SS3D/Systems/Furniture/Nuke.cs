using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.Inventory.Items.Generic;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public class Nuke : InteractionSource, IInteractionTarget
    {
        private AssetHandle<Sprite> _nukeIconHandle;

        protected override void OnAwake()
        {
            base.OnAwake();
            AcquireIcon();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            ReleaseIcon();
        }

        [ServerRpc(RequireOwnership = false)]
        public void Detonate()
        {
            // Ends the round, regardless of how many objectives were completed
            SubSystems.Get<GamemodeSubSystem>().EndRound();
        }

        IInteraction[] IInteractionTarget.CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new NukeDetonateInteraction
                {
                    Icon = _nukeIconHandle?.Asset,
                },
            };
        }

        private async void AcquireIcon()
        {
            if (!_nukeIconHandle)
            {
                _nukeIconHandle = await new AssetRequest<Sprite>(InteractionIcons.Nuke).LoadAsync();
            }
        }

        private void ReleaseIcon()
        {
            if (_nukeIconHandle is not { IsValid: true })
            {
                return;
            }

            _nukeIconHandle.Dispose();
            _nukeIconHandle = null;
        }
    }
}