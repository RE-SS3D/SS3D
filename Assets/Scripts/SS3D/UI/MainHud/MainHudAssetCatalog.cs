using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud
{
    /// <summary>
    /// Committed resolved refs for the Main HUD. Rebuild via
    /// <c>SS3D → Data → Rebuild All UI Catalogs</c>. Loaded at runtime with <c>Resources.Load</c>
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
        [SerializeField] private Sprite _iconGloves;
        [SerializeField] private Sprite _iconShirt;
        [SerializeField] private Sprite _iconFeet;
        [SerializeField] private Sprite _iconBelt;
        [SerializeField] private Sprite _iconId;
        [FormerlySerializedAs("_iconPda")]
        [SerializeField] private Sprite _iconPocket;
        [SerializeField] private Sprite _iconBack;

        [SerializeField] private Sprite _alertFire;
        [SerializeField] private Sprite _alertHot;
        [SerializeField] private Sprite _alertCold;
        [SerializeField] private Sprite _alertLowPressure;
        [SerializeField] private Sprite _alertHighPressure;
        [SerializeField] private Sprite _alertRadiation;
        [SerializeField] private Sprite _alertHunger;
        [SerializeField] private Sprite _alertThirst;
        [SerializeField] private Sprite _alertPulling;
        [SerializeField] private Sprite _alertRestrained;
        [SerializeField] private Sprite _alertLowOxygen;
        [SerializeField] private Sprite _alertDying;
        [SerializeField] private Sprite _alertBleeding;
        [SerializeField] private Sprite _alertCardiacArrest;
        [SerializeField] private Sprite _alertSealBreach;
        [SerializeField] private Sprite _alertInvincible;
        [SerializeField] private Sprite _alertTankLow;
        [SerializeField] private Sprite _internals;
        [SerializeField] private Sprite _magboots;

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
            Gloves = _iconGloves,
            Shirt = _iconShirt,
            Feet = _iconFeet,
            Belt = _iconBelt,
            Id = _iconId,
            Pocket = _iconPocket,
            Back = _iconBack,
        };

        public AlertIconSet AlertIcons => new()
        {
            Fire = _alertFire,
            Hot = _alertHot,
            Cold = _alertCold,
            LowPressure = _alertLowPressure,
            HighPressure = _alertHighPressure,
            Radiation = _alertRadiation,
            Hunger = _alertHunger,
            Thirst = _alertThirst,
            Pulling = _alertPulling,
            Restrained = _alertRestrained,
            LowOxygen = _alertLowOxygen,
            Dying = _alertDying,
            Bleeding = _alertBleeding,
            CardiacArrest = _alertCardiacArrest,
            SealBreach = _alertSealBreach,
            Invincible = _alertInvincible,
            TankLow = _alertTankLow,
            Internals = _internals,
            Magboots = _magboots,
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

            if (_alertFire == null
                || _alertHot == null
                || _alertCold == null
                || _alertLowPressure == null
                || _alertHighPressure == null
                || _alertRadiation == null
                || _alertHunger == null
                || _alertThirst == null
                || _alertPulling == null
                || _alertRestrained == null
                || _alertLowOxygen == null
                || _alertDying == null
                || _alertBleeding == null
                || _alertCardiacArrest == null
                || _alertSealBreach == null
                || _alertInvincible == null
                || _alertTankLow == null
                || _internals == null
                || _magboots == null)
            {
                missingField = "alert icon sprites";
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
            MainHudIconSet icons,
            AlertIconSet alertIcons)
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
            _iconGloves = icons.Gloves;
            _iconShirt = icons.Shirt;
            _iconFeet = icons.Feet;
            _iconBelt = icons.Belt;
            _iconId = icons.Id;
            _iconPocket = icons.Pocket;
            _iconBack = icons.Back;
            _alertFire = alertIcons.Fire;
            _alertHot = alertIcons.Hot;
            _alertCold = alertIcons.Cold;
            _alertLowPressure = alertIcons.LowPressure;
            _alertHighPressure = alertIcons.HighPressure;
            _alertRadiation = alertIcons.Radiation;
            _alertHunger = alertIcons.Hunger;
            _alertThirst = alertIcons.Thirst;
            _alertPulling = alertIcons.Pulling;
            _alertRestrained = alertIcons.Restrained;
            _alertLowOxygen = alertIcons.LowOxygen;
            _alertDying = alertIcons.Dying;
            _alertBleeding = alertIcons.Bleeding;
            _alertCardiacArrest = alertIcons.CardiacArrest;
            _alertSealBreach = alertIcons.SealBreach;
            _alertInvincible = alertIcons.Invincible;
            _alertTankLow = alertIcons.TankLow;
            _internals = alertIcons.Internals;
            _magboots = alertIcons.Magboots;
        }
#endif
    }
}
