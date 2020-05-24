﻿using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class WallConstructionInteraction : DelayedInteraction
    {
        public Turf WallToConstruct { get; set; }
        public Turf FloorToConstruct { get; set; }

        public override string GetName(InteractionEvent interactionEvent)
        {
            TileObject tileObject = (interactionEvent.Target as IGameObjectProvider)?.GameObject?.GetComponentInParent<TileObject>();
            if (tileObject != null && tileObject.Tile.turf.isWall)
            {
                return "Deconstruct";
            }

            return "Construct";
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionHelpers.RangeCheck(interactionEvent))
            {
                return false;
            }
            
            if (interactionEvent.Target is IGameObjectProvider targetBehaviour)
            {
                TileObject targetTile = targetBehaviour.GameObject.GetComponentInParent<TileObject>();
                if (targetTile == null)
                {
                    return false;
                }

                if (targetTile.Tile.fixture != null)
                {
                    return false;
                }

                return true;
            }
            
            return false;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            
        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            var targetBehaviour = (IGameObjectProvider) interactionEvent.Target;
            TileManager tileManager = Object.FindObjectOfType<TileManager>();
            TileObject targetTile = targetBehaviour.GameObject.GetComponentInParent<TileObject>();
            var tile = targetTile.Tile;
            
            if (tile.turf?.isWall == true) // Deconstruct
                tile.turf = FloorToConstruct;
            else // Construct
                tile.turf = WallToConstruct;

            tile.fixture = null;
            // TODO: Make an easier way of doing this.
            tile.subStates = new object[2];

            tileManager.UpdateTile(targetTile.transform.position, tile);
        }
    }
}