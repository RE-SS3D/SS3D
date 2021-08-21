using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class WallConstructionInteraction : ConstructionInteraction
    {
        public TileObjectSO WallToConstruct { get; set; }

        public override string GetName(InteractionEvent interactionEvent)
        {
            PlacedTileObject tileObject = (interactionEvent.Target as IGameObjectProvider)?.GameObject?.GetComponentInParent<PlacedTileObject>();
            if (tileObject != null && tileObject.GetGenericType() == "wall")
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

            return true;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            
        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            var targetBehaviour = (IGameObjectProvider) interactionEvent.Target;
            TileManager tileManager = TileManager.Instance;
            PlacedTileObject targetPlacedObject = targetBehaviour.GameObject.GetComponentInParent<PlacedTileObject>();
            // var tile = targetPlacedObject.Tile;

            // Deconstruct
            if (targetPlacedObject != null && targetPlacedObject.GetGenericType() == "wall")
            {
                tileManager.ClearTileObject(TileLayer.Turf, targetPlacedObject.transform.position);
            }
            else // Construct
                tileManager.SetTileObject(WallToConstruct, targetPlacedObject.transform.position, Direction.South);

        }
    }
}
