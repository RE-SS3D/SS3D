using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Items;
using SS3D.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Systems.Furniture
{
    public class VendingMachine : InteractionSourceNetworkBehaviour, IInteractionTarget
    {
        public Transform SodaDispensePoint;

        [ServerRpc(RequireOwnership = false)]
        public void CmdDispenseProduct()
        {
            DispenseProduct();
        }

        [Server]
        private void DispenseProduct()
        {
            ItemSystem itemSystem = SystemLocator.Get<ItemSystem>();

            if (itemSystem == null)
            {
                itemSystem.CmdSpawnItem(ItemIDs.BikeHorn, SodaDispensePoint.position, Quaternion.identity);
            }
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new DispenseProductInteraction()
            {
                Icon = AssetData.Get(InteractionIcons.Take)
            } };
        }
    }
}