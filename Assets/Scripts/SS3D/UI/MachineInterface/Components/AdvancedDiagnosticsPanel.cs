using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class AdvancedDiagnosticsPanel : VisualElement
    {
        public event System.Action HideRequested;

        private readonly VisualElement _metricsRoot;
        private readonly Label _maintenanceValue;

        private readonly Button _hideButton;

        public AdvancedDiagnosticsPanel()
        {
            AddToClassList("advanced-diagnostics");

            VisualElement header = new();
            header.AddToClassList("advanced-diagnostics__header");

            Label title = new("ADVANCED DIAGNOSTICS");
            title.AddToClassList("advanced-diagnostics__title");
            title.AddToClassList("font-titling");

            _hideButton = new Button { text = "hide ✕" };
            _hideButton.AddToClassList("advanced-diagnostics__hide");
            _hideButton.AddToClassList("font-terminal");
            _hideButton.clicked += () => HideRequested?.Invoke();

            header.Add(title);
            header.Add(_hideButton);

            _metricsRoot = new VisualElement();
            _metricsRoot.AddToClassList("advanced-diagnostics__metrics");

            VisualElement maintenance = new();
            maintenance.AddToClassList("advanced-diagnostics__maintenance");

            Label maintenanceLabel = new("MAINTENANCE STATE");
            maintenanceLabel.AddToClassList("advanced-diagnostics__maintenance-label");
            maintenanceLabel.AddToClassList("font-arcade");

            _maintenanceValue = new Label("No maintenance due.");
            _maintenanceValue.AddToClassList("advanced-diagnostics__maintenance-value");
            _maintenanceValue.AddToClassList("font-terminal");

            maintenance.Add(maintenanceLabel);
            maintenance.Add(_maintenanceValue);

            Add(header);
            Add(_metricsRoot);
            Add(maintenance);
        }

        public void SetMetrics(IReadOnlyList<SmesMetricLine> metrics, string maintenanceText, StatusTone maintenanceTone)
        {
            _metricsRoot.Clear();

            if (metrics != null)
            {
                foreach (SmesMetricLine metric in metrics)
                {
                    VisualElement cell = new();
                    cell.AddToClassList("advanced-diagnostics__metric");

                    Label label = new(metric.Label.ToUpperInvariant());
                    label.AddToClassList("advanced-diagnostics__metric-label");
                    label.AddToClassList("font-arcade");

                    Label value = new(metric.Value);
                    value.AddToClassList("advanced-diagnostics__metric-value");
                    value.AddToClassList("font-terminal");
                    StatusToneUtility.ApplyTone(value, metric.Tone);

                    cell.Add(label);
                    cell.Add(value);
                    _metricsRoot.Add(cell);
                }
            }

            _maintenanceValue.text = maintenanceText;
            StatusToneUtility.ApplyTone(_maintenanceValue, maintenanceTone);
        }
    }
}
