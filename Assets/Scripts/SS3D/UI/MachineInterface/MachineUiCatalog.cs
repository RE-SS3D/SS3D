using SS3D.UI.MachineInterface.Bindings;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Serialized asset refs for UI registration (filled from <see cref="MachineUiAssetCatalog"/>).
    /// </summary>
    public readonly struct MachineUiCatalogAssets
    {
        public VisualTreeAsset ApcTemplate { get; init; }
        public StyleSheet ApcTemplateStyle { get; init; }
        public StyleSheet[] ApcComponentStyles { get; init; }

        public VisualTreeAsset SmesTemplate { get; init; }
        public StyleSheet SmesTemplateStyle { get; init; }
        public StyleSheet[] SmesComponentStyles { get; init; }

        public VisualTreeAsset VendingTemplate { get; init; }
        public StyleSheet VendingTemplateStyle { get; init; }
        public StyleSheet[] VendingComponentStyles { get; init; }

        public VisualTreeAsset IdConsoleTemplate { get; init; }
        public StyleSheet IdConsoleTemplateStyle { get; init; }

        public VisualTreeAsset GasPumpTemplate { get; init; }
        public StyleSheet GasPumpTemplateStyle { get; init; }
        public StyleSheet[] GasPumpComponentStyles { get; init; }

        public VisualTreeAsset AirAlarmTemplate { get; init; }
        public StyleSheet AirAlarmTemplateStyle { get; init; }
        public StyleSheet[] AirAlarmComponentStyles { get; init; }

        public VisualTreeAsset ScrubberTemplate { get; init; }
        public StyleSheet ScrubberTemplateStyle { get; init; }
        public StyleSheet[] ScrubberComponentStyles { get; init; }

        public VisualTreeAsset VentTemplate { get; init; }
        public StyleSheet VentTemplateStyle { get; init; }
        public StyleSheet[] VentComponentStyles { get; init; }

        public VisualTreeAsset FilterTemplate { get; init; }
        public StyleSheet FilterTemplateStyle { get; init; }
        public StyleSheet[] FilterComponentStyles { get; init; }

        public VisualTreeAsset MixerTemplate { get; init; }
        public StyleSheet MixerTemplateStyle { get; init; }
        public StyleSheet[] MixerComponentStyles { get; init; }

        public VisualTreeAsset MeterTemplate { get; init; }
        public StyleSheet MeterTemplateStyle { get; init; }
        public StyleSheet[] MeterComponentStyles { get; init; }
    }

    /// <summary>
    /// Registers machine UI entries into <see cref="MachineInterfaceRegistry"/>.
    /// Asset refs come from <see cref="MachineUiAssetCatalog"/> (path catalog), not scene SerializeFields.
    /// </summary>
    public static class MachineUiCatalog
    {
        public static void RegisterAll(in MachineUiCatalogAssets assets)
        {
            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Apc,
                Template = assets.ApcTemplate,
                TemplateStyle = assets.ApcTemplateStyle,
                ComponentStyles = assets.ApcComponentStyles,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new ApcPowerControllerBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Smes,
                Template = assets.SmesTemplate,
                TemplateStyle = assets.SmesTemplateStyle,
                ComponentStyles = assets.SmesComponentStyles,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new SmesUnitBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Vending,
                Template = assets.VendingTemplate,
                TemplateStyle = assets.VendingTemplateStyle,
                ComponentStyles = assets.VendingComponentStyles,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new VendingMachineBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.IdConsole,
                Template = assets.IdConsoleTemplate,
                TemplateStyle = assets.IdConsoleTemplateStyle,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new IdConsoleBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Pump,
                Template = assets.GasPumpTemplate,
                TemplateStyle = assets.GasPumpTemplateStyle,
                ComponentStyles = assets.GasPumpComponentStyles,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new PumpInterfaceBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.AirAlarm,
                Template = assets.AirAlarmTemplate,
                TemplateStyle = assets.AirAlarmTemplateStyle,
                ComponentStyles = assets.AirAlarmComponentStyles,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new AirAlarmInterfaceBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Scrubber,
                Template = assets.ScrubberTemplate,
                TemplateStyle = assets.ScrubberTemplateStyle,
                ComponentStyles = assets.ScrubberComponentStyles,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new ScrubberInterfaceBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Vent,
                Template = assets.VentTemplate,
                TemplateStyle = assets.VentTemplateStyle,
                ComponentStyles = assets.VentComponentStyles,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new VentInterfaceBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Filter,
                Template = assets.FilterTemplate,
                TemplateStyle = assets.FilterTemplateStyle,
                ComponentStyles = assets.FilterComponentStyles,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new FilterInterfaceBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Mixer,
                Template = assets.MixerTemplate,
                TemplateStyle = assets.MixerTemplateStyle,
                ComponentStyles = assets.MixerComponentStyles,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new MixerInterfaceBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Meter,
                Template = assets.MeterTemplate,
                TemplateStyle = assets.MeterTemplateStyle,
                ComponentStyles = assets.MeterComponentStyles,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new MeterInterfaceBinder(root),
            });
        }
    }
}
