using System.Collections.Generic;
using System.Linq;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class WelderConstructionInteraction : DelayedInteraction
    {
        public Dictionary<Turf, Turf> TurfReinforceList { get; set; }

        public override string GetName(InteractionEvent interactionEvent)
        {
            GameObject target = ((IGameObjectProvider) interactionEvent.Target).GameObject;
            TileObject tileObject = target.GetComponentInParent<TileObject>();
            
            if (TurfReinforceList.ContainsKey(tileObject.Tile.turf))
            {
                return "Reinforce";
            }
            
            return "Unreinforce";
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            GameObject target = (interactionEvent.Target as IGameObjectProvider)?.GameObject;
            if (target == null)
            {
                return false;
            }
            
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }
            
            // Needs to be used on tile
            TileObject tileObject = target.GetComponentInParent<TileObject>();
            if (tileObject == null || tileObject.Tile.turf == null)
            {
                return false;
            }

            Turf turf = tileObject.Tile.turf;

            // Check if welder is on
            if ((interactionEvent.Source as IToggleable)?.GetState() == false)
            {
                return false;
            }
            
            // Check if turf is in dict
            if (!TurfReinforceList.ContainsKey(turf) && !TurfReinforceList.ContainsValue(turf))
            {
                return false;
            }

            return true;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            
        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            TileManager tileManager = Object.FindObjectOfType<TileManager>();
            GameObject target = ((IGameObjectProvider) interactionEvent.Target).GameObject;
            TileDefinition tile = target.GetComponentInParent<TileObject>().Tile;

            if (TurfReinforceList.TryGetValue(tile.turf, out Turf turf))
            {
                tile.turf = turf;
            }
            else
            {
                tile.turf = TurfReinforceList.First(x => x.Value == tile.turf).Key;
            }
            
            tileManager.UpdateTile(target.transform.position, tile);
        }
    }
}