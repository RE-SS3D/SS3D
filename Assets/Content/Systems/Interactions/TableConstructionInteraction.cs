using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class TableConstructionInteraction : ConstructionInteraction
    {
        public FurnitureFloorFixture TableToConstruct { get; set; }

        public override string GetName(InteractionEvent interactionEvent)
        {
            TileObject tileObject = (interactionEvent.Target as IGameObjectProvider)?.GameObject?.GetComponentInParent<TileObject>();
            if (tileObject != null && tileObject.Tile.fixtures.GetFloorFixtureAtLayer(FloorFixtureLayers.FurnitureFixtureMain) == TableToConstruct)
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

            return TargetTile.Tile.turf?.isWall != true;
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

            
            if (tile.fixtures.GetFloorFixtureAtLayer(FloorFixtureLayers.FurnitureFixtureMain) != null) // If there is a fixture on the place
            {
                if (tile.fixtures.GetFloorFixtureAtLayer(FloorFixtureLayers.FurnitureFixtureMain) == TableToConstruct) // If the fixture is a table
                {
                    tile.fixtures.SetFloorFixtureAtLayer(null, FloorFixtureLayers.FurnitureFixtureMain); // Deconstruct
                }
            }
            else // If there is no fixture on place
            {
                tile.fixtures.SetFloorFixtureAtLayer(TableToConstruct, FloorFixtureLayers.FurnitureFixtureMain); // Construct
            }
            
            // TODO: Make an easier way of doing this.
            tile.subStates = new object[2];

            // Validate if we can actually place the table
            FixturesContainer.ValidateFixtures(tile);
            tile.fixtures = (FixturesContainer)tile.fixtures.Clone();

            tileManager.UpdateTile(targetTile.transform.position, tile);
        }
    }
}