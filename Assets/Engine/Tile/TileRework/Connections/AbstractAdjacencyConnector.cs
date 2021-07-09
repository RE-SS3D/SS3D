using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles.Connections
{
    [RequireComponent(typeof(MeshFilter))]
    public abstract class AbstractAdjacencyConnector : MonoBehaviour, IAdjacencyConnector
    {
        protected AdjacencyBitmap adjacents = new AdjacencyBitmap();
        protected AdjacencyBitmap blocked = new AdjacencyBitmap();
        protected MeshFilter filter;

        [Tooltip("Id that adjacent objects must be to count. If empty, any id is accepted")]
        public string type;

        public abstract void UpdateAll(PlacedTileObject[] neighbourObjects);

        public abstract void UpdateSingle(Direction dir, PlacedTileObject placedObject);

        protected abstract void UpdateMeshAndDirection();

        public void Awake() => filter = GetComponent<MeshFilter>();
        public void OnEnable() => UpdateMeshAndDirection();
    }
}