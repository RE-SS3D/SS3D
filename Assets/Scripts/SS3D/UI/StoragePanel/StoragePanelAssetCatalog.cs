using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.StoragePanel
{
    /// <summary>
    /// Committed resolved refs for the storage panel surface. Rebuild via
    /// <c>SS3D → Storage Panel → Rebuild Asset Catalog</c>. Loaded at runtime with
    /// <c>Resources.Load</c> so standalone builds work without Editor AssetDatabase — same
    /// pattern as MainHudAssetCatalog / MachineUiAssetCatalog.
    /// </summary>
    [CreateAssetMenu(
        fileName = StoragePanelAssetPaths.ResourcesCatalogName,
        menuName = "SS3D/UI/Storage Panel Asset Catalog")]
    public sealed class StoragePanelAssetCatalog : ScriptableObject
    {
        [SerializeField] private PanelSettings _panelSettings;
        [SerializeField] private StyleSheet _storagePanelStyle;
        [SerializeField] private StyleSheet _inventorySlotStyle;

        public PanelSettings PanelSettings => _panelSettings;
        public StyleSheet StoragePanelStyle => _storagePanelStyle;
        public StyleSheet InventorySlotStyle => _inventorySlotStyle;

        public bool HasRequiredAssets(out string missingField)
        {
            if (_panelSettings == null)
            {
                missingField = nameof(_panelSettings);
                return false;
            }

            if (_storagePanelStyle == null || _inventorySlotStyle == null)
            {
                missingField = "stylesheets";
                return false;
            }

            missingField = null;
            return true;
        }

#if UNITY_EDITOR
        public void EditorAssign(
            PanelSettings panelSettings,
            StyleSheet storagePanelStyle,
            StyleSheet inventorySlotStyle)
        {
            _panelSettings = panelSettings;
            _storagePanelStyle = storagePanelStyle;
            _inventorySlotStyle = inventorySlotStyle;
        }
#endif
    }
}
