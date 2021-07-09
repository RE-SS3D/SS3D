using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles.Connections
{
    public class MultiAdjacencyConnector : AbstractAdjacencyConnector
    {
        public AdjacencyType selectedAdjacencyType;
        [SerializeField] private SimpleAdjacency simpleAdjacency;
        [SerializeField] private AdvancedAdjacency advancedAdjacency;

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
            return adjacents.UpdateDirection(dir, isConnected, true);
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
            }
            

            if (filter == null)
                filter = GetComponent<MeshFilter>();

            filter.mesh = info.mesh;
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, info.rotation, transform.localRotation.eulerAngles.z);
        }
    }
}