using UnityEngine;
using SS3D.Engine.Tiles;
using SS3D.Engine.Interactions;

namespace SS3D.Content.Items.Functional.Tools
{
    [RequireComponent(typeof(WelderController))]
    public class WelderConstructer : DelayedInteraction
    {
        [SerializeField]
        private Turf commonWall = null;
        [SerializeField]
        private Turf reinforcedWall = null;
        [SerializeField]
        private Turf commonFloor = null;
        [SerializeField]
        private Turf reinforcedFloor = null;
        WelderController controller;

        public override InteractionEvent Event { get; set; }
        public override string Name => "Reinforce/Deinforce";

        // The distance in which to allow constructing walls
        public float buildDistance = 1.5f;
        void Awake()
        {
            controller = GetComponent<WelderController>();
        }

        public override bool CanInteract()
        {
            if (!base.CanInteract())
            {
                return false;
            }

            targetTile = Event.target.GetComponentInParent<TileObject>();

            // If target tile exists.
            if (targetTile == null)
            {
                return false;
            }

            //Only shows if the welder is on
            if (!controller.pSystem.isEmitting)
            {
                return false;
            }

            //Dont construct if picking up the item.
            if (Event.tool != gameObject)
            {
                return false;
            }

            // Range check
            if (Vector3.Distance(Event.Player.transform.position, Event.target.transform.position) > 1.5f)
            {
                return false;
            }


            //The target tile's.... Tile.
            var tile = targetTile.Tile;

            return true;
        }

        protected override void InteractDelayed()
        {
            // Note: CanInteract is always called before Interact, so we KNOW targetTile is defined.
            var tileManager = FindObjectOfType<TileManager>();


            var tile = targetTile.Tile;

            if (tile.turf?.isWall == false)
            {
                if (tile.turf == commonFloor)
                {
                    tile.turf = reinforcedFloor;
                }
                else {
                    tile.turf = commonFloor;
                }
            } else {
                if (tile.turf == commonWall)
                {
                    tile.turf = reinforcedWall;
                } else {
                    tile.turf = commonWall;
                }
            }
            tile.subStates = new object[2];

            tileManager.UpdateTile(targetTile.transform.position, tile);
        }
        TileObject targetTile;
    }
}