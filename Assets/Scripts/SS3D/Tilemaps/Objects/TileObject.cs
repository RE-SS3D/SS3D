using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Data;
using UnityEngine;

namespace SS3D.Tilemaps.Objects
{
    public class TileObject : NetworkedSpessBehaviour
    {
        [HideInInspector] public TileObjects Id;
        public TileLayer Layer;

        private List<Component> _renderers;

        [SyncVar(OnChange = "HandlePositionChanged")] public Vector3Int SyncPosition;
        [SyncVar(OnChange = "HandleRotationChanged")] public Quaternion SyncRotation;

        public List<Component> Renderers => _renderers;

        protected override void OnAwake()
        {
            base.OnAwake();

            _renderers = GetComponents(typeof(Renderer)).ToList();
            _renderers.AddRange(GetComponentsInChildren(typeof(Renderer)));
        }

        [Server]
        public void SetPositionAndRotation(Vector3Int position, Quaternion rotation)
        {
            SyncPosition = position;
            SyncRotation = rotation;
        }

        public void HandlePositionChanged(Vector3Int oldPosition, Vector3Int newPosition, bool asServer)
        {
            Position = newPosition;
        }

        public void HandleRotationChanged(Quaternion oldRotation, Quaternion newRotation, bool asServer)
        {
            Rotation = newRotation;
        }
    }
}