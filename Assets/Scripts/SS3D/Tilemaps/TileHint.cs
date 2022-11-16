using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Tilemaps
{
    public class TileHint : SpessBehaviour
    {
        public Vector3 _currentPosition;

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);

            MoveToMousePosition();
        }

        private void MoveToMousePosition()
        {
            // Convert mouse position to world position by finding point where y = 0.
            if (Camera.main == null)
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 worldPosition = ray.origin - (ray.origin.y / ray.direction.y) * ray.direction;
            Vector3 snappedPosition = TileHelper.GetClosestPosition(worldPosition);
        
            _currentPosition = snappedPosition;
            Position = snappedPosition;
        }
    }
}
