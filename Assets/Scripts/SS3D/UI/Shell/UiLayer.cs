namespace SS3D.UI.Shell
{
    /// <summary>
    /// Composition layers owned by <see cref="UiShellSubSystem"/>, back-to-front. Mirrors the layer
    /// names named in Documents/architecture/systems/ui-shell.md.
    /// </summary>
    public enum UiLayer
    {
        Hud = 0,
        Overlay = 1,
        Diegetic = 2,
        Modal = 3,
        Debug = 4,
    }
}
