using System;
using System.Collections;
using Interaction.Core;
using UnityEngine;

namespace Interaction
{
    [CreateAssetMenu(fileName = "PaintAtInterval", menuName = "Painting/Paint at interval", order = 0)]
    internal class PaintAtInterval : ContinuousInteraction
    {
        [SerializeField] private float interval = 1.0f;
        [SerializeField] private InteractionKind from = null;
        [SerializeField] private InteractionKind to = null;
        
        public override void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(from);
        }

        public override IEnumerator Handle(InteractionEvent e)
        {
            if (!e.target)
            {
                yield break;
            }
            
            var newEvent = e;
            newEvent.target = null;
            newEvent.kind = to;
            newEvent.sender = Receiver.gameObject;
            
            var playerInteractor = e.sender.GetComponent<PlayerInteractor>();
            if (playerInteractor == null)
            {
                Debug.LogError($"Received event from player {e.player} without a PlayerInteractor component.");
                yield break;
            }

            var prevPosition = e.worldPosition;

            while (e.runWhile(e))
            {
                if (!playerInteractor.GetWorldData(out var hit))
                {
                    yield break;
                }

                if (Vector3.Distance(prevPosition, hit.point) > 0.01f)
                {
                    e.target = hit.transform.GetComponent<InteractionReceiver>();
                    newEvent.worldPosition = hit.point;
                    newEvent.worldNormal = hit.normal;
                
                    e.target.Trigger(newEvent);
                }
                
                yield return new WaitForSeconds(interval);
            }
        }
    }
}