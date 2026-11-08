using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.MapEditor.Commands;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    public sealed partial class MapEditorSubSystem
    {
        public GasMarkerRecord GasMarkerDraft => _viewModel.GasMarkerDraft;

        private void SelectGasMarkerKind(GasMarkerKind kind)
        {
            if (_viewModel.GasMarkerDraft.Kind != kind)
                _viewModel.GasMarkerDraft = GasMarkerRecord.Default(kind);

            _hologramManager.SetSelectedMarker(MapEditorGasMarkerCatalog.Encode(kind));
            _view.ShowGasMarkerProperties(editingPlaced: false);
        }

        /// <summary>
        /// On the Starting Air rail, Select opens the marker under the cursor and Dropper copies it. Off that
        /// rail this does nothing, and markers stay out of the way of the ordinary pick.
        /// </summary>
        private bool TryPickGasMarker(MapEditorTool tool)
        {
            if (_viewModel.CurrentSubcategory != MapEditorSubcategory.StartingAir)
                return false;

            if (!_tileSystem.GasMarkers.TryGetAt(_hologramManager.GetPlacementPoint(forceTileSnap: true), out GasMarkerRecord marker))
                return true;

            _viewModel.GasMarkerDraft = marker;
            if (tool == MapEditorTool.Select)
            {
                _view.ShowGasMarkerProperties(editingPlaced: true);
                return true;
            }

            if (_catalog.TryGetEntry(MapEditorGasMarkerCatalog.Encode(marker.Kind), out MapEditorCatalogEntry entry))
            {
                _viewModel.SelectEntry(entry, null);
                _viewModel.SetTool(MapEditorTool.Edit);
                SelectGasMarkerKind(marker.Kind);
            }

            return true;
        }

        private void OnGasMarkerApplyRequested(GasMarkerRecord marker)
        {
            SubmitCommands(new[]
            {
                new MapEditorCommandDto
                {
                    Kind = MapEditorCommandKind.PlaceGasMarker,
                    AssetName = MapEditorGasMarkerCatalog.Encode(marker.Kind),
                    Position = marker.Position,
                    GasMarker = marker,
                },
            });
            _viewModel.ShowToast($"Updated {MapEditorGasMarkerCatalog.DisplayName(marker.Kind)}");
            _toastTimer = 1.6f;
        }
    }
}
