using UnityEngine.UIElements;

namespace SS3D.UI.Shell
{
    /// <summary>
    /// Formalizes the Attach/Detach lifecycle already used by convention across UI Toolkit overlay
    /// views (radial menu, armed overlay, main HUD): a view builds its own tree in code and is
    /// grafted onto a layer root owned by <see cref="UiShellSubSystem"/> rather than a private
    /// <c>UIDocument</c>.
    /// </summary>
    public interface IUiSurface
    {
        void Attach(VisualElement layerRoot);

        void Detach();
    }
}
