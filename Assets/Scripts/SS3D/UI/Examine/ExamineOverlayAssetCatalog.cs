using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Examine
{
    /// <summary>
    /// Committed resolved refs for the examine overlay UI. Rebuild via
    /// <c>SS3D → Data → Rebuild All UI Catalogs</c>. Loaded at runtime with
    /// <c>Resources.Load</c> so <see cref="ExamineOverlaySubSystem"/> can self-bootstrap without any
    /// scene/prefab placement (same pattern as <c>UiShellAssetCatalog</c>/<c>MainHudAssetCatalog</c>).
    /// </summary>
    [CreateAssetMenu(
        fileName = ExamineOverlayAssetPaths.ResourcesCatalogName,
        menuName = "SS3D/UI/Examine Overlay Asset Catalog")]
    public sealed class ExamineOverlayAssetCatalog : ScriptableObject
    {
        [SerializeField] private StyleSheet _examineStyle;
        [SerializeField] private StyleSheet _inventorySlotStyle;
        [SerializeField] private StyleSheet _diegeticTokensStyle;
        [SerializeField] private StyleSheet _machineWindowStyle;

        [SerializeField] private Sprite _iconHead;
        [SerializeField] private Sprite _iconEyes;
        [SerializeField] private Sprite _iconFace;
        [SerializeField] private Sprite _iconEars;
        [SerializeField] private Sprite _iconSuit;
        [SerializeField] private Sprite _iconShirt;
        [SerializeField] private Sprite _iconGloves;
        [SerializeField] private Sprite _iconBack;
        [SerializeField] private Sprite _iconFeet;
        [SerializeField] private Sprite _iconBelt;
        [SerializeField] private Sprite _iconIdCard;
        [SerializeField] private Sprite _iconPocket;
        [SerializeField] private Sprite _iconHandLeft;
        [SerializeField] private Sprite _iconHandRight;

        public StyleSheet ExamineStyle => _examineStyle;

        public StyleSheet InventorySlotStyle => _inventorySlotStyle;

        public StyleSheet DiegeticTokensStyle => _diegeticTokensStyle;

        public StyleSheet MachineWindowStyle => _machineWindowStyle;

        public CharacterExamineIconSet Icons => new()
        {
            Head = _iconHead,
            Eyes = _iconEyes,
            Face = _iconFace,
            Ears = _iconEars,
            Suit = _iconSuit,
            Shirt = _iconShirt,
            Gloves = _iconGloves,
            Back = _iconBack,
            Feet = _iconFeet,
            Belt = _iconBelt,
            IdCard = _iconIdCard,
            Pocket = _iconPocket,
            HandLeft = _iconHandLeft,
            HandRight = _iconHandRight,
        };

        public bool HasRequiredAssets(out string missingField)
        {
            if (_examineStyle == null
                || _inventorySlotStyle == null
                || _diegeticTokensStyle == null
                || _machineWindowStyle == null)
            {
                missingField = "stylesheets";
                return false;
            }

            missingField = null;
            return true;
        }

#if UNITY_EDITOR
        public void EditorAssign(
            StyleSheet examineStyle,
            StyleSheet inventorySlotStyle,
            StyleSheet diegeticTokensStyle,
            StyleSheet machineWindowStyle,
            CharacterExamineIconSet icons)
        {
            _examineStyle = examineStyle;
            _inventorySlotStyle = inventorySlotStyle;
            _diegeticTokensStyle = diegeticTokensStyle;
            _machineWindowStyle = machineWindowStyle;
            _iconHead = icons.Head;
            _iconEyes = icons.Eyes;
            _iconFace = icons.Face;
            _iconEars = icons.Ears;
            _iconSuit = icons.Suit;
            _iconShirt = icons.Shirt;
            _iconGloves = icons.Gloves;
            _iconBack = icons.Back;
            _iconFeet = icons.Feet;
            _iconBelt = icons.Belt;
            _iconIdCard = icons.IdCard;
            _iconPocket = icons.Pocket;
            _iconHandLeft = icons.HandLeft;
            _iconHandRight = icons.HandRight;
        }
#endif
    }
}
