using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework.Connections
{
    public class MultiAdjacencyConnector : AbstractAdjacencyConnector
    {
        [SerializeField] private AdjacencyType adjacencyType;

        [Header("Simple Meshes")]
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the north edge is connected")]
        public Mesh c;
        [Tooltip("A mesh where the north & south edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the north & east edges are connected")]
        public Mesh l;
        [Tooltip("A mesh where the north, east, and west edges are connected")]
        public Mesh t;
        [Tooltip("A mesh where all edges are connected")]
        public Mesh x;

        [Header("Advanced Meshes")]
        [Tooltip("A mesh where all edges are connected, no corners")]
        public Mesh xNone;
        [Tooltip("A mesh where all edges are connected, SW is a corner")]
        public Mesh xSingle;
        [Tooltip("A mesh where all edges are connected, SW & SW are corners")]
        public Mesh xSide;
        [Tooltip("A mesh where all edges are connected, NW & SE are corners")]
        public Mesh xOpposite;
        [Tooltip("A mesh where all edges are connected, all but NE are corners")]
        public Mesh xTriple;
        [Tooltip("A mesh where all edges are connected, all corners")]
        public Mesh xQuad;

        public override void UpdateAll(PlacedTileObject[] neighbourObjects)
        {
            throw new System.NotImplementedException();
        }

        public override void UpdateSingle(Direction dir, PlacedTileObject placedObject)
        {
            throw new System.NotImplementedException();
        }

        protected override void UpdateMeshAndDirection()
        {
            throw new System.NotImplementedException();
        }
    }
}