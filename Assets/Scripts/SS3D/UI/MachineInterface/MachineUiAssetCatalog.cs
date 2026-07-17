using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Committed resolved refs for machine UI. Rebuild via
    /// <c>SS3D → Machine Interface → Rebuild Asset Catalog</c> from <see cref="MachineUiAssetPaths"/>.
    /// Loaded at runtime with <c>Resources.Load</c>.
    /// </summary>
    [CreateAssetMenu(
        fileName = MachineUiAssetPaths.ResourcesCatalogName,
        menuName = "SS3D/UI/Machine UI Asset Catalog")]
    public sealed class MachineUiAssetCatalog : ScriptableObject
    {
        [SerializeField]
        private PanelSettings _panelSettings;

        [SerializeField]
        private StyleSheet _machineWindowStyle;

        [SerializeField]
        private StyleSheet _ss3dTokensStyle;

        [SerializeField]
        private StyleSheet _ss3dTypographyStyle;

        [SerializeField]
        private StyleSheet _diegeticTokensStyle;

        [SerializeField]
        private StyleSheet _diegeticTonesStyle;

        [SerializeField]
        private VisualTreeAsset _apcTemplate;

        [SerializeField]
        private StyleSheet _apcTemplateStyle;

        [SerializeField]
        private StyleSheet[] _apcComponentStyles;

        [SerializeField]
        private VisualTreeAsset _smesTemplate;

        [SerializeField]
        private StyleSheet _smesTemplateStyle;

        [SerializeField]
        private StyleSheet[] _smesComponentStyles;

        [SerializeField]
        private VisualTreeAsset _vendingTemplate;

        [SerializeField]
        private StyleSheet _vendingTemplateStyle;

        [SerializeField]
        private StyleSheet[] _vendingComponentStyles;

        [SerializeField]
        private VisualTreeAsset _idConsoleTemplate;

        [SerializeField]
        private StyleSheet _idConsoleTemplateStyle;

        [SerializeField]
        private VisualTreeAsset _gasPumpTemplate;

        [SerializeField]
        private StyleSheet _gasPumpTemplateStyle;

        [SerializeField]
        private StyleSheet[] _gasPumpComponentStyles;

        [SerializeField]
        private VisualTreeAsset _airAlarmTemplate;

        [SerializeField]
        private StyleSheet _airAlarmTemplateStyle;

        [SerializeField]
        private StyleSheet[] _airAlarmComponentStyles;

        [SerializeField]
        private VisualTreeAsset _scrubberTemplate;

        [SerializeField]
        private StyleSheet _scrubberTemplateStyle;

        [SerializeField]
        private StyleSheet[] _scrubberComponentStyles;

        [SerializeField]
        private VisualTreeAsset _ventTemplate;

        [SerializeField]
        private StyleSheet _ventTemplateStyle;

        [SerializeField]
        private StyleSheet[] _ventComponentStyles;

        public PanelSettings PanelSettings => _panelSettings;
        public StyleSheet MachineWindowStyle => _machineWindowStyle;
        public StyleSheet Ss3dTokensStyle => _ss3dTokensStyle;
        public StyleSheet Ss3dTypographyStyle => _ss3dTypographyStyle;
        public StyleSheet DiegeticTokensStyle => _diegeticTokensStyle;
        public StyleSheet DiegeticTonesStyle => _diegeticTonesStyle;

        public MachineUiCatalogAssets ToCatalogAssets()
        {
            return new MachineUiCatalogAssets
            {
                ApcTemplate = _apcTemplate,
                ApcTemplateStyle = _apcTemplateStyle,
                ApcComponentStyles = _apcComponentStyles,
                SmesTemplate = _smesTemplate,
                SmesTemplateStyle = _smesTemplateStyle,
                SmesComponentStyles = _smesComponentStyles,
                VendingTemplate = _vendingTemplate,
                VendingTemplateStyle = _vendingTemplateStyle,
                VendingComponentStyles = _vendingComponentStyles,
                IdConsoleTemplate = _idConsoleTemplate,
                IdConsoleTemplateStyle = _idConsoleTemplateStyle,
                GasPumpTemplate = _gasPumpTemplate,
                GasPumpTemplateStyle = _gasPumpTemplateStyle,
                GasPumpComponentStyles = _gasPumpComponentStyles,
                AirAlarmTemplate = _airAlarmTemplate,
                AirAlarmTemplateStyle = _airAlarmTemplateStyle,
                AirAlarmComponentStyles = _airAlarmComponentStyles,
                ScrubberTemplate = _scrubberTemplate,
                ScrubberTemplateStyle = _scrubberTemplateStyle,
                ScrubberComponentStyles = _scrubberComponentStyles,
                VentTemplate = _ventTemplate,
                VentTemplateStyle = _ventTemplateStyle,
                VentComponentStyles = _ventComponentStyles,
            };
        }

        public bool HasRequiredAssets(out string missingField)
        {
            if (_panelSettings == null)
            {
                missingField = nameof(_panelSettings);
                return false;
            }

            if (_machineWindowStyle == null
                || _ss3dTokensStyle == null
                || _ss3dTypographyStyle == null
                || _diegeticTokensStyle == null
                || _diegeticTonesStyle == null)
            {
                missingField = "shared styles";
                return false;
            }

            if (_apcTemplate == null || _apcTemplateStyle == null
                || _smesTemplate == null || _smesTemplateStyle == null
                || _vendingTemplate == null || _vendingTemplateStyle == null
                || _idConsoleTemplate == null || _idConsoleTemplateStyle == null
                || _gasPumpTemplate == null || _gasPumpTemplateStyle == null
                || _airAlarmTemplate == null || _airAlarmTemplateStyle == null
                || _scrubberTemplate == null || _scrubberTemplateStyle == null
                || _ventTemplate == null || _ventTemplateStyle == null)
            {
                missingField = "machine templates";
                return false;
            }

            missingField = null;
            return true;
        }

#if UNITY_EDITOR
        public void EditorAssign(
            PanelSettings panelSettings,
            StyleSheet machineWindowStyle,
            StyleSheet ss3dTokensStyle,
            StyleSheet ss3dTypographyStyle,
            StyleSheet diegeticTokensStyle,
            StyleSheet diegeticTonesStyle,
            VisualTreeAsset apcTemplate,
            StyleSheet apcTemplateStyle,
            StyleSheet[] apcComponentStyles,
            VisualTreeAsset smesTemplate,
            StyleSheet smesTemplateStyle,
            StyleSheet[] smesComponentStyles,
            VisualTreeAsset vendingTemplate,
            StyleSheet vendingTemplateStyle,
            StyleSheet[] vendingComponentStyles,
            VisualTreeAsset idConsoleTemplate,
            StyleSheet idConsoleTemplateStyle,
            VisualTreeAsset gasPumpTemplate,
            StyleSheet gasPumpTemplateStyle,
            StyleSheet[] gasPumpComponentStyles,
            VisualTreeAsset airAlarmTemplate,
            StyleSheet airAlarmTemplateStyle,
            StyleSheet[] airAlarmComponentStyles,
            VisualTreeAsset scrubberTemplate,
            StyleSheet scrubberTemplateStyle,
            StyleSheet[] scrubberComponentStyles,
            VisualTreeAsset ventTemplate,
            StyleSheet ventTemplateStyle,
            StyleSheet[] ventComponentStyles)
        {
            _panelSettings = panelSettings;
            _machineWindowStyle = machineWindowStyle;
            _ss3dTokensStyle = ss3dTokensStyle;
            _ss3dTypographyStyle = ss3dTypographyStyle;
            _diegeticTokensStyle = diegeticTokensStyle;
            _diegeticTonesStyle = diegeticTonesStyle;
            _apcTemplate = apcTemplate;
            _apcTemplateStyle = apcTemplateStyle;
            _apcComponentStyles = apcComponentStyles;
            _smesTemplate = smesTemplate;
            _smesTemplateStyle = smesTemplateStyle;
            _smesComponentStyles = smesComponentStyles;
            _vendingTemplate = vendingTemplate;
            _vendingTemplateStyle = vendingTemplateStyle;
            _vendingComponentStyles = vendingComponentStyles;
            _idConsoleTemplate = idConsoleTemplate;
            _idConsoleTemplateStyle = idConsoleTemplateStyle;
            _gasPumpTemplate = gasPumpTemplate;
            _gasPumpTemplateStyle = gasPumpTemplateStyle;
            _gasPumpComponentStyles = gasPumpComponentStyles;
            _airAlarmTemplate = airAlarmTemplate;
            _airAlarmTemplateStyle = airAlarmTemplateStyle;
            _airAlarmComponentStyles = airAlarmComponentStyles;
            _scrubberTemplate = scrubberTemplate;
            _scrubberTemplateStyle = scrubberTemplateStyle;
            _scrubberComponentStyles = scrubberComponentStyles;
            _ventTemplate = ventTemplate;
            _ventTemplateStyle = ventTemplateStyle;
            _ventComponentStyles = ventComponentStyles;
        }
#endif
    }
}
