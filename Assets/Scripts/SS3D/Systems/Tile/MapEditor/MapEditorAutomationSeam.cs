using System.Runtime.CompilerServices;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.MapEditor.UI;
using UnityEngine;

// The editor pilots (MapEditorSystemsPilot) drive the editor through the seam below; the mppm steps live in this assembly.
[assembly: InternalsVisibleTo("SS3D.Editor.Automation.Lifecycle")]
// EditMode tests stand in for roles and FishNet's host through MapEditorPermissions.HostOrAdministrator.
[assembly: InternalsVisibleTo("SS3D.Tests.EditMode")]

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Test seam for automation (the visual pilot, mppm:systems-view): the handlers a person's input reaches, called by
    /// name at compile time instead of by reflection, so a rename breaks the build rather than a nightly run.
    /// </summary>
    public sealed partial class MapEditorSubSystem
    {
        internal MapEditorViewModel AutomationViewModel => _viewModel;
        internal void AutomationToggleOpen() => HandleToggleMenu(default);
        internal void AutomationOpenSubTab(MapEditorSubcategory sub) => OpenSubTab(sub);
        internal void AutomationPickPaletteEntry(MapEditorCatalogEntry entry, GenericObjectSo asset) => OnPaletteEntryPicked(entry, asset);
        internal void AutomationSelectTool(MapEditorTool tool) => OnToolSelected(tool);
        internal void AutomationSelectClick() => HandleSelectClick();
        internal void AutomationSelectSystemsView(SystemsViewKind view) => OnSystemsViewSelected(view);
        internal void AutomationCenterOn(Vector3 position) => _session.CenterOn(position);
        internal void AutomationZoomStep(float amount) => _session.ZoomStep(amount);
    }
}
