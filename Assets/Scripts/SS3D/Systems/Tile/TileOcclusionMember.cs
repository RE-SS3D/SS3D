using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Component placed on tile object prefabs. Automatically informs the <see cref="TileOcclusionSubSystem"/>
    /// when this tile object is spawned or despawned on the client, and allows controlling the visibility
    /// of its renderers without disabling game logic, colliders, or network behaviors.
    /// </summary>
    public class TileOcclusionMember : Actor
    {
        [SerializeField]
        private TileLayer _layer;

        private Vector2Int _gridPosition;
        private Renderer[] _renderers;
        private TileOcclusionSubSystem _occlusionSubSystem;
        private bool _isRegistered;
        private bool _isVisible = true;

        public TileLayer Layer => _layer;

        public Vector2Int GridPosition => _gridPosition;

        /// <summary>
        /// Toggles the visibility of all renderers on this tile object.
        /// </summary>
        /// <param name="visible">True to enable renderers, false to disable them.</param>
        public void SetVisible(bool visible)
        {
            if (_isVisible == visible)
            {
                return;
            }

            _isVisible = visible;

            if (_renderers == null)
            {
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = visible;
                }
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            // Ignore execution on dedicated servers without graphics
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;
            }

            // Holograms and preview clones are not networked spawned objects
            if (!TryGetComponent(out NetworkObject networkObject) || !networkObject.IsSpawned)
            {
                return;
            }

            if (!SubSystems.TryGet(out _occlusionSubSystem))
            {
                return;
            }

            Vector3 worldPosition = Position;
            _gridPosition = new Vector2Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));

            // Only cache renderers for coverable layers (Turf is never hidden)
            if (_layer != TileLayer.Turf)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }

            _occlusionSubSystem.RegisterMember(this, _gridPosition, _layer);
            _isRegistered = true;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            if (_isRegistered && _occlusionSubSystem != null)
            {
                _occlusionSubSystem.UnregisterMember(this, _gridPosition, _layer);
            }

            _isRegistered = false;
        }
    }
}
