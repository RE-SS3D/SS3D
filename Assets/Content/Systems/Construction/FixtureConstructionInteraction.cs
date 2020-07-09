using System;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;

namespace SS3D.Content.Systems.Construction
{
    public class FixtureConstructionInteraction : ConstructionInteraction
    {
        /// <summary>
        /// The fixture to construct
        /// </summary>
        public Fixture Fixture { get; set; }
        
        /// <summary>
        /// If any existing fixture should be overwritten
        /// </summary>
        public bool Overwrite { get; set; }
        
        /// <summary>
        /// The type of the fixture to construct
        /// </summary>
        public FixtureType FixtureType { get; set; }
        
        public TileFixtureLayers TileLayer
        {
            get => tileLayer;
            set
            {
                tileLayer = value;
                FixtureType = FixtureType.TileFixture;
            }
        }
        
        public WallFixtureLayers WallLayer
        {
            get => wallLayer;
            set
            {
                wallLayer = value;
                FixtureType = FixtureType.WallFixture;
            }
        }
        
        public FloorFixtureLayers FloorLayer
        {
            get => floorLayer;
            set
            {
                floorLayer = value;
                FixtureType = FixtureType.FloorFixture;
            }
        }

        private TileFixtureLayers tileLayer;
        private WallFixtureLayers wallLayer;
        private FloorFixtureLayers floorLayer;


        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Construct fixture";
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!base.CanInteract(interactionEvent))
            {
                return false;
            }

            return Overwrite || !GetFixture(TargetTile.Tile.fixtures);
        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            TileManager tileManager = UnityEngine.Object.FindObjectOfType<TileManager>();
            TileDefinition definition = TargetTile.Tile;
            // Set desired fixture
            SetFixture(definition.fixtures);
            
            // Required to get the tile to update fixtures
            // TODO: Add flag?
            definition.fixtures = (FixturesContainer) definition.fixtures.Clone();
            
            // Apply change
            tileManager.UpdateTile(TargetTile.transform.position, definition);
        }

        private Fixture GetFixture(FixturesContainer container)
        {
            switch (FixtureType)
            {
                case FixtureType.TileFixture:
                    return container.GetTileFixtureAtLayer(tileLayer);
                case FixtureType.WallFixture:
                    return container.GetWallFixtureAtLayer(wallLayer);
                case FixtureType.FloorFixture:
                    return container.GetFloorFixtureAtLayer(floorLayer);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetFixture(FixturesContainer container)
        {
            switch (FixtureType)
            {
                case FixtureType.TileFixture:
                    container.SetTileFixtureAtLayer((TileFixture) Fixture, tileLayer);
                    break;
                case FixtureType.WallFixture:
                    container.SetWallFixtureAtLayer((WallFixture) Fixture, wallLayer);
                    break;
                case FixtureType.FloorFixture:
                    container.SetFloorFixtureAtLayer((FloorFixture) Fixture, floorLayer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
