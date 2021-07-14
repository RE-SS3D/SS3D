using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles.Connections
{
    public class MultiAdjacencyConnector : AbstractAdjacencyConnector
    {
        public AdjacencyType selectedAdjacencyType;
        [SerializeField] private SimpleConnector simpleAdjacency;
        [SerializeField] private AdvancedConnector advancedAdjacency;
        [SerializeField] private OffsetConnector offsetAdjacency;

        public override void UpdateAll(PlacedTileObject[] neighbourObjects)
        {
            bool changed = false;
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                changed |= UpdateSingleConnection((Direction)i, neighbourObjects[i]);
            }

            if (changed)
                UpdateMeshAndDirection();
        }

        public override void UpdateSingle(Direction dir, PlacedTileObject placedObject)
        {
            if (UpdateSingleConnection(dir, placedObject))
            {
                UpdateMeshAndDirection();
            }
        }

        private bool UpdateSingleConnection(Direction dir, PlacedTileObject placedObject)
        {
            bool isConnected = (placedObject && placedObject.HasAdjacencyConnector() && (placedObject.GetGenericType() == type || type == null));

            isConnected &= (AdjacencyBitmap.Adjacent(blockedDirections, dir) == 0);

            return adjacents.UpdateDirection(dir, isConnected, true);
        }

        public void SetBlockedDirection(Direction dir, bool value)
        {
            AdjacencyBitmap.SetDirection(blockedDirections, dir, value);
        }

        protected override void UpdateMeshAndDirection()
        {
            MeshDirectionInfo info = new MeshDirectionInfo();
            switch (selectedAdjacencyType)
            {
                case AdjacencyType.Simple:
                    info = simpleAdjacency.GetMeshAndDirection(adjacents);
                    break;
                case AdjacencyType.Advanced:
                    info = advancedAdjacency.GetMeshAndDirection(adjacents);
                    break;
                case AdjacencyType.Offset:
                    info = offsetAdjacency.GetMeshAndDirection(adjacents);
                    break;
            }
            

            if (filter == null)
                filter = GetComponent<MeshFilter>();

            filter.mesh = info.mesh;
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, info.rotation, transform.localRotation.eulerAngles.z);
        }
    }
}