using SS3D.Core;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Offers the map editor's Atmos view its source (Documents/architecture/2026-11_map-editor-systems-views.md Q10).
    /// The editor hub is spawned on another object with no fixed start order, so the server retries from each atmos tick
    /// until it finds one, and offers again if that hub is replaced.
    /// </summary>
    public sealed partial class AtmosSubSystem
    {
        private AtmosSystemsViewSource _systemsViewSource;
        private MapEditorSubSystem _systemsViewHost;

        /// <summary>Server: the Atmos view's structure source; null on a pure client.</summary>
        public AtmosSystemsViewSource SystemsViewSource => _systemsViewSource;

        private void EnsureSystemsViewRegistered()
        {
            if (!IsServer)
                return;

            _systemsViewSource ??= new AtmosSystemsViewSource(
                () => SubSystems.TryGet(out TileSubSystem tiles) ? tiles.CurrentMap : null,
                () => _pipeRegistry,
                _portRegistry);
            if (_systemsViewHost != null || !SubSystems.TryGet(out MapEditorSubSystem host))
                return;

            host.SystemsViews.Register(_systemsViewSource);
            _systemsViewHost = host;
        }

        private void UnregisterSystemsView()
        {
            if (_systemsViewHost != null && _systemsViewSource != null)
                _systemsViewHost.SystemsViews.Unregister(_systemsViewSource);
            _systemsViewHost = null;
        }
    }
}
