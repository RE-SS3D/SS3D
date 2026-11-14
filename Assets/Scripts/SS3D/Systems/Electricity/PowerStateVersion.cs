namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Server: moves whenever device state the Map Editor's Power view shows may have changed (a consumer's
    /// <see cref="PowerStatus"/> written, an APC's breakers or condition, an SMES's output switch, a device registered or
    /// removed). The view's source re-reads every device only when this or the topology moved, so an idle station costs a
    /// counter compare (Documents/architecture/2026-11_map-editor-systems-views.md, Phase 3). Bump it from any new place
    /// that writes such state. One server per process, so a static counter is enough.
    /// </summary>
    public static class PowerStateVersion
    {
        public static int Current { get; private set; }

        public static void Bump() => Current++;
    }
}
