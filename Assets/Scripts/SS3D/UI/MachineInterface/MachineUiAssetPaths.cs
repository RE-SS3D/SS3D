namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Stable asset paths for machine UI templates and styles.
    /// Rebuild the committed catalog with <c>SS3D → Machine Interface → Rebuild Asset Catalog</c>.
    /// </summary>
    public static class MachineUiAssetPaths
    {
        public const string ResourcesCatalogName = "MachineUiAssetCatalog";
        public const string CatalogAssetPath =
            "Assets/Content/Systems/UI/MachineInterface/Resources/MachineUiAssetCatalog.asset";

        public const string PanelSettings =
            "Assets/Content/Systems/UI/MachineInterface/MachineInterfacePanelSettings.asset";

        public const string MachineWindowStyle =
            "Assets/Content/Systems/UI/MachineInterface/Components/MachineWindow.uss";

        public const string Ss3dTokens =
            "Assets/Content/Systems/UI/Tokens/ss3d-tokens.uss";

        public const string Ss3dTypography =
            "Assets/Content/Systems/UI/Tokens/ss3d-typography.uss";

        public const string DiegeticTokens =
            "Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tokens.uss";

        public const string DiegeticTones =
            "Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tones.uss";

        public const string ApcTemplate =
            "Assets/Content/Systems/UI/MachineInterface/Templates/ApcPowerController.uxml";

        public const string ApcTemplateStyle =
            "Assets/Content/Systems/UI/MachineInterface/Templates/ApcPowerController.uss";

        public const string SmesTemplate =
            "Assets/Content/Systems/UI/MachineInterface/Templates/SmesUnitInterface.uxml";

        public const string SmesTemplateStyle =
            "Assets/Content/Systems/UI/MachineInterface/Templates/SmesUnitInterface.uss";

        public const string VendingTemplate =
            "Assets/Content/Systems/UI/MachineInterface/Templates/VendingMachineInterface.uxml";

        public const string VendingTemplateStyle =
            "Assets/Content/Systems/UI/MachineInterface/Templates/VendingMachineInterface.uss";

        public const string IdConsoleTemplate =
            "Assets/Content/Systems/UI/MachineInterface/Templates/IdConsoleInterface.uxml";

        public const string IdConsoleTemplateStyle =
            "Assets/Content/Systems/UI/MachineInterface/Templates/IdConsoleInterface.uss";

        public const string GasPumpTemplate =
            "Assets/Content/Systems/UI/MachineInterface/Templates/PumpUnitInterface.uxml";

        public const string GasPumpTemplateStyle =
            "Assets/Content/Systems/UI/MachineInterface/Templates/PumpUnitInterface.uss";

        public const string AirAlarmTemplate =
            "Assets/Content/Systems/UI/MachineInterface/Templates/AirAlarmInterface.uxml";

        public const string AirAlarmTemplateStyle =
            "Assets/Content/Systems/UI/MachineInterface/Templates/AirAlarmInterface.uss";

        public const string ScrubberTemplate =
            "Assets/Content/Systems/UI/MachineInterface/Templates/ScrubberUnitInterface.uxml";

        public const string ScrubberTemplateStyle =
            "Assets/Content/Systems/UI/MachineInterface/Templates/ScrubberUnitInterface.uss";

        public const string VentTemplate =
            "Assets/Content/Systems/UI/MachineInterface/Templates/VentUnitInterface.uxml";

        public const string VentTemplateStyle =
            "Assets/Content/Systems/UI/MachineInterface/Templates/VentUnitInterface.uss";

        public static readonly string[] ApcComponentStyles =
        {
            "Assets/Content/Systems/UI/MachineInterface/Components/DiegeticDeviceShell.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/StatusDot.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ConnectionStatusRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/DeviceIdentityBlock.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/GlanceableStatusChip.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/PanelSection.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/PowerFlowRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/BatteryBar.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/DiagnosticsList.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ChannelRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ToggleSwitch.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/AccessGatePanel.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/AccessGatedRegion.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/AccessStrip.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/SteelButton.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/Badge.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/StatusBadge.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/DeviceFooter.uss",
        };

        public static readonly string[] SmesComponentStyles =
        {
            "Assets/Content/Systems/UI/MachineInterface/Components/DiegeticDeviceShell.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/StatusDot.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ConnectionStatusRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/DeviceIdentityBlock.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/GlanceableStatusChip.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/PanelSection.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/StorageCellRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/SmesPowerFlowRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/RateControlSection.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ToggleSwitch.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/AccessGatePanel.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/AccessGatedRegion.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/AccessStrip.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/SteelButton.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/Badge.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/StatusBadge.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/DeviceFooter.uss",
        };

        public static readonly string[] VendingComponentStyles =
        {
            "Assets/Content/Systems/UI/MachineInterface/Components/DiegeticDeviceShell.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/StatusDot.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ConnectionStatusRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/DeviceIdentityBlock.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/PanelSection.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/AtmosIdReaderRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/SteelButton.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/InventorySlot.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ProductCard.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ProductGrid.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/DispenseTray.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ActionLog.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/DeviceFooter.uss",
        };

        public static readonly string[] AtmosBaseComponentStyles =
        {
            "Assets/Content/Systems/UI/MachineInterface/Components/DiegeticDeviceShell.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/StatusDot.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ConnectionStatusRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/DeviceIdentityBlock.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/GlanceableStatusChip.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/PanelSection.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ReadoutMetricTile.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/GasBarRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/AtmosIdReaderRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ToggleSwitch.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/CompactFilterToggle.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/NumericStepper.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/SteelButton.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/StatusBadge.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/DeviceFooter.uss",
        };

        public static readonly string[] AirAlarmExtraComponentStyles =
        {
            "Assets/Content/Systems/UI/MachineInterface/Components/PresetModeButton.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/ConnectedDeviceRow.uss",
            "Assets/Content/Systems/UI/MachineInterface/Components/AirAlarmDeviceDetailPanel.uss",
        };

        public static readonly string[] VentExtraComponentStyles =
        {
            "Assets/Content/Systems/UI/MachineInterface/Components/PressureFlowReadout.uss",
        };

        public static string[] BuildAtmosComponentStyles(bool includeAirAlarm, bool includeVent)
        {
            int count = AtmosBaseComponentStyles.Length
                + (includeAirAlarm ? AirAlarmExtraComponentStyles.Length : 0)
                + (includeVent ? VentExtraComponentStyles.Length : 0);

            string[] paths = new string[count];
            int index = 0;
            for (int i = 0; i < AtmosBaseComponentStyles.Length; i++)
            {
                paths[index++] = AtmosBaseComponentStyles[i];
            }

            if (includeAirAlarm)
            {
                for (int i = 0; i < AirAlarmExtraComponentStyles.Length; i++)
                {
                    paths[index++] = AirAlarmExtraComponentStyles[i];
                }
            }

            if (includeVent)
            {
                for (int i = 0; i < VentExtraComponentStyles.Length; i++)
                {
                    paths[index++] = VentExtraComponentStyles[i];
                }
            }

            return paths;
        }
    }
}
