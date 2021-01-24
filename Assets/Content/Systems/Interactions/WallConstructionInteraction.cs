using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class WallConstructionInteraction : ConstructionInteraction
    {
        public Turf WallToConstruct { get; set; }
        public Turf FloorToConstruct { get; set; }

        public override string GetName(InteractionEvent interactionEvent)
        {
            TileObject tileObject = (interactionEvent.Target as IGameObjectProvider)?.GameObject?.GetComponentInParent<TileObject>();
            if (tileObject != null && tileObject.Tile.turf != null && tileObject.Tile.turf.isWall)
            {
                return "Deconstruct";
            }

            return "Construct";
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!base.CanInteract(interactionEvent))
            {
                return false;
            }

            return !TargetTile.Tile.fixtures.floorFixtureDefinition.IsEmpty();
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

            // Deconstruct
            if (tile.turf?.isWall == true)
            {
                tile.turf = null;
            }
            else // Construct
                tile.turf = WallToConstruct;

            // TODO: Change rotation from defaulting to North
            tile.fixtures.SetFloorFixtureAtLayer(null, FloorFixtureLayers.FurnitureFixtureMain);
            FixturesContainer.ValidateFixtures(tile);

            // TODO: Make an easier way of doing this.
            tile.subStates = new object[2];

            tileManager.UpdateTile(targetTile.transform.position, tile);
        }
    }
}
