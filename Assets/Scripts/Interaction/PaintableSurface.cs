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
    [CreateAssetMenu(fileName = "Paintable Surface", menuName = "Interaction/Paintable Surface", order = 0)]
    public class PaintableSurface : ContinuousInteraction
    {
        [SerializeField] private InteractionKind kind = null;
        
        public override void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(kind);
        }

        public override IEnumerator Handle(InteractionEvent e)
        {
            if (e.sender == null)
            {
                Debug.LogWarning($"Tried to paint null on ({name})");
                yield break;
            }

            Painter painter = e.sender.GetComponent<Painter>();
            if (painter == null)
            {
                Debug.LogError($"Tried to paint with {e.sender} without a Painter component.)");
                yield break;
            }

            PlayerInteractor playerInteractor = e.player.GetComponent<PlayerInteractor>();
            if (playerInteractor == null)
            {
                Debug.LogError($"Received event from player {e.player} without a PlayerInteractor component.");
                yield break;
            }
            
            var playerHands = e.player.GetComponent<Hands>();
            if (playerHands == null)
            {
                Debug.LogWarning($"Player painting with object ({e.sender.name}) does not have a hands component");
                yield break;
            }
            
            NetworkInteractionSpawner networkInteractionSpawner = e.player.GetComponent<NetworkInteractionSpawner>();
            if (networkInteractionSpawner == null)
            {
                Debug.LogError($"Received paint event from player {e.player} without a NetworkInteractionSpawner component.");
                yield break;
            }

            var inventoryUi = FindObjectOfType<UIInventory>();

            while (e.runWhile.Invoke(e))
            {
                float remainingSupply = painter.GetRemainingSupplyPercentage();
                
                //Update paint location
                if (!playerInteractor.GetWorldData(out var hit)) yield break;
                //Stop painting if surface no longer applicable
                InteractionReceiver receiver = hit.transform.GetComponent<InteractionReceiver>();
                if (receiver == null || !receiver.IsListeningForContinuous(this)) yield break;
                //Update remaining supply bar on painter
                if(inventoryUi) inventoryUi.SetHeldItemSupply(remainingSupply);

                //Decrease supply and spawn new decal. Set to be slightly above collider to avoid clipping.
                playerInteractor.CmdDecreaseSupplyOfItem(painter.gameObject);
                Vector3 position = hit.point + hit.normal * 0.01f;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.back, hit.normal);
                networkInteractionSpawner.CmdSpawnPainterDecal(painter.Properties.name, position, rotation);

                if (remainingSupply <= 0)
                {
                    playerHands.DestroyItemInHand();
                    yield break;
                }
                yield return new WaitForSeconds(0.01f);
            }
        }
    }
}