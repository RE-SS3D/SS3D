using SS3D.Core;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// A domain subsystem's offer of its source to the map editor hub. The hub is spawned on another object with no fixed
    /// start order, so the server calls <see cref="Ensure"/> from its own tick until a hub is there.
    /// </summary>
    public sealed class SystemsViewRegistration
    {
        private MapEditorSubSystem _host;

        public void Ensure(ISystemsViewSource source)
        {
            if (_host != null || source == null || !SubSystems.TryGet(out MapEditorSubSystem host))
                return;

            host.SystemsViews.Register(source);
            _host = host;
        }

        public void Release(ISystemsViewSource source)
        {
            if (_host != null && source != null)
                _host.SystemsViews.Unregister(source);
            _host = null;
        }
    }
}
