using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud
{
    /// <summary>
    /// Committed resolved refs for the Main HUD. Rebuild via
    /// <c>SS3D → Main HUD → Rebuild Asset Catalog</c>. Loaded at runtime with <c>Resources.Load</c>
    /// so standalone builds work without Editor AssetDatabase.
    /// </summary>
    [CreateAssetMenu(
        fileName = MainHudAssetPaths.ResourcesCatalogName,
        menuName = "SS3D/UI/Main HUD Asset Catalog")]
    public sealed class MainHudAssetCatalog : ScriptableObject
    {
        [SerializeField] private PanelSettings _panelSettings;
        [SerializeField] private StyleSheet _mainHudStyle;
        [SerializeField] private StyleSheet _alertIconStackStyle;
        [SerializeField] private StyleSheet _intentModuleStyle;
        [SerializeField] private StyleSheet _handsGearStripStyle;
        [SerializeField] private StyleSheet _equipmentGridStyle;
        [SerializeField] private StyleSheet _inventorySlotStyle;

        [SerializeField] private Sprite _iconHead;
        [SerializeField] private Sprite _iconEyes;
        [SerializeField] private Sprite _iconFace;
        [SerializeField] private Sprite _iconEars;
        [SerializeField] private Sprite _iconHandLeft;
        [SerializeField] private Sprite _iconHandRight;
        [SerializeField] private Sprite _iconShirt;
        [SerializeField] private Sprite _iconFeet;
        [SerializeField] private Sprite _iconBelt;
        [SerializeField] private Sprite _iconId;
        [FormerlySerializedAs("_iconPda")]
        [SerializeField] private Sprite _iconPocket;
        [SerializeField] private Sprite _iconBack;

        public PanelSettings PanelSettings => _panelSettings;
        public StyleSheet MainHudStyle => _mainHudStyle;
        public StyleSheet AlertIconStackStyle => _alertIconStackStyle;
        public StyleSheet IntentModuleStyle => _intentModuleStyle;
        public StyleSheet HandsGearStripStyle => _handsGearStripStyle;
        public StyleSheet EquipmentGridStyle => _equipmentGridStyle;
        public StyleSheet InventorySlotStyle => _inventorySlotStyle;

        public MainHudIconSet Icons => new()
        {
            Head = _iconHead,
            Eyes = _iconEyes,
            Face = _iconFace,
            Ears = _iconEars,
            HandLeft = _iconHandLeft,
            HandRight = _iconHandRight,
            Shirt = _iconShirt,
            Feet = _iconFeet,
            Belt = _iconBelt,
            Id = _iconId,
            Pocket = _iconPocket,
            Back = _iconBack,
        };

        public bool HasRequiredAssets(out string missingField)
        {
            if (_panelSettings == null)
            {
                missingField = nameof(_panelSettings);
                return false;
            }

            if (_mainHudStyle == null
                || _alertIconStackStyle == null
                || _intentModuleStyle == null
                || _handsGearStripStyle == null
                || _equipmentGridStyle == null
                || _inventorySlotStyle == null)
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
            StyleSheet mainHudStyle,
            StyleSheet alertIconStackStyle,
            StyleSheet intentModuleStyle,
            StyleSheet handsGearStripStyle,
            StyleSheet equipmentGridStyle,
            StyleSheet inventorySlotStyle,
            MainHudIconSet icons)
        {
            _panelSettings = panelSettings;
            _mainHudStyle = mainHudStyle;
            _alertIconStackStyle = alertIconStackStyle;
            _intentModuleStyle = intentModuleStyle;
            _handsGearStripStyle = handsGearStripStyle;
            _equipmentGridStyle = equipmentGridStyle;
            _inventorySlotStyle = inventorySlotStyle;
            _iconHead = icons.Head;
            _iconEyes = icons.Eyes;
            _iconFace = icons.Face;
            _iconEars = icons.Ears;
            _iconHandLeft = icons.HandLeft;
            _iconHandRight = icons.HandRight;
            _iconShirt = icons.Shirt;
            _iconFeet = icons.Feet;
            _iconBelt = icons.Belt;
            _iconId = icons.Id;
            _iconPocket = icons.Pocket;
            _iconBack = icons.Back;
        }
#endif
    }
}
