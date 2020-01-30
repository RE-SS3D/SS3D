using System;
using System.Collections;
using Interaction.Core;
using Inventory.Custom;
using Inventory.UI;
using ItemComponents;
using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// Component for handling the Paint action targeted at an object.
    /// </summary>
    [CreateAssetMenu(fileName = "Paintable Surface", menuName = "Painting/Paintable Surface", order = 0)]
    public class PaintableSurface : SingularInteraction
    {
        [SerializeField] private InteractionKind kind = null;
        
        public override void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(kind);
        }

        public override bool Handle(InteractionEvent e)
        {
            if (e.sender == null)
            {
                Debug.LogWarning($"Tried to paint null on ({name})");
                return false;
            }

            var painter = e.sender.GetComponent<Painter>();
            if (painter == null)
            {
                Debug.LogError($"Tried to paint with {e.sender} without a Painter component.)");
                return false;
            }

            var playerInteractor = e.player.GetComponent<PlayerInteractor>();
            if (playerInteractor == null)
            {
                Debug.LogError($"Received event from player {e.player} without a PlayerInteractor component.");
                return false;
            }
            
            var playerHands = e.player.GetComponent<Hands>();
            if (playerHands == null)
            {
                Debug.LogWarning($"Player painting with object ({e.sender.name}) does not have a hands component");
                return false;
            }
            
            var networkInteractionSpawner = e.player.GetComponent<NetworkInteractionSpawner>();
            if (networkInteractionSpawner == null)
            {
                Debug.LogError($"Received paint event from player {e.player} without a NetworkInteractionSpawner component.");
                return false;
            }

            var remainingSupply = painter.GetRemainingSupplyPercentage();
            
            //Update remaining supply bar on painter
            var inventoryUi = FindObjectOfType<UIInventory>();
            if (inventoryUi)
            {
                inventoryUi.SetHeldItemSupply(remainingSupply);
            }

            //Decrease supply and spawn new decal. Set to be slightly above collider to avoid clipping.
            playerInteractor.CmdDecreaseSupplyOfItem(painter.gameObject);
            
            var position = e.worldPosition + e.worldNormal * 0.01f;
            var rotation = Quaternion.FromToRotation(Vector3.back, e.worldNormal);
            
            networkInteractionSpawner.CmdSpawnPainterDecal(painter.Properties.name, position, rotation);

            if (remainingSupply <= 0)
            {
                playerHands.DestroyItemInHand();
            }

            return true;
        }
    }
}