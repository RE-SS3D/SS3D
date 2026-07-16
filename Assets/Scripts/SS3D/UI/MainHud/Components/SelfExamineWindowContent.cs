using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Components
{
    public readonly struct LimbReadout
    {
        public readonly string Label;
        public readonly string BruteBurnText;

        public LimbReadout(string label, string bruteBurnText)
        {
            Label = label;
            BruteBurnText = bruteBurnText;
        }
    }

    public enum OrganSeverity
    {
        Normal,
        Warning,
        Critical,
    }

    public readonly struct OrganReadout
    {
        public readonly string Name;
        public readonly int Percent;
        public readonly OrganSeverity Severity;

        public OrganReadout(string name, int percent, OrganSeverity severity)
        {
            Name = name;
            Percent = percent;
            Severity = severity;
        }
    }

    /// <summary>
    /// On-demand per-limb + organ readout shown while the self-examine key is held (design doc §5/§15).
    /// Hosted inside a <see cref="SS3D.UI.MachineInterface.Components.MachineWindow"/>'s content area.
    /// Purely presentational - <see cref="SS3D.UI.MainHud.MainHudSubSystem"/> supplies the data.
    /// </summary>
    [UxmlElement]
    public partial class SelfExamineWindowContent : VisualElement
    {
        private readonly VisualElement _limbColumn;
        private readonly VisualElement _organColumn;

        public SelfExamineWindowContent()
        {
            AddToClassList("self-examine");

            _limbColumn = BuildColumn("Per-Limb Damage");
            VisualElement divider = new();
            divider.AddToClassList("self-examine__divider");
            _organColumn = BuildColumn("Organ Function");

            Add(_limbColumn);
            Add(divider);
            Add(_organColumn);
        }

        public void SetLimbs(IReadOnlyList<LimbReadout> limbs)
        {
            RemoveRowsExceptHeading(_limbColumn);

            foreach (LimbReadout limb in limbs)
            {
                VisualElement row = new();
                row.AddToClassList("self-examine__limb-row");
                row.AddToClassList("font-terminal");

                Label nameLabel = new(limb.Label);
                Label valueLabel = new(limb.BruteBurnText);

                row.Add(nameLabel);
                row.Add(valueLabel);
                _limbColumn.Add(row);
            }
        }

        public void SetOrgans(IReadOnlyList<OrganReadout> organs)
        {
            RemoveRowsExceptHeading(_organColumn);

            foreach (OrganReadout organ in organs)
            {
                VisualElement wrapper = new();
                wrapper.AddToClassList("self-examine__organ");

                VisualElement labelRow = new();
                labelRow.AddToClassList("self-examine__organ-label-row");
                labelRow.AddToClassList(SeverityClass(organ.Severity, "self-examine__organ-label-row"));

                labelRow.Add(new Label(organ.Name));
                labelRow.Add(new Label($"{organ.Percent}%"));

                VisualElement track = new();
                track.AddToClassList("self-examine__organ-bar-track");

                VisualElement fill = new();
                fill.AddToClassList("self-examine__organ-bar-fill");
                fill.AddToClassList(SeverityClass(organ.Severity, "self-examine__organ-bar-fill"));
                fill.style.width = new Length(organ.Percent, LengthUnit.Percent);
                track.Add(fill);

                wrapper.Add(labelRow);
                wrapper.Add(track);
                _organColumn.Add(wrapper);
            }
        }

        private static string SeverityClass(OrganSeverity severity, string baseClass) => severity switch
        {
            OrganSeverity.Critical => $"{baseClass}--critical",
            OrganSeverity.Warning => $"{baseClass}--warning",
            _ => baseClass,
        };

        private static VisualElement BuildColumn(string heading)
        {
            VisualElement column = new();
            column.AddToClassList("self-examine__column");

            Label headingLabel = new(heading);
            headingLabel.AddToClassList("self-examine__heading");
            headingLabel.AddToClassList("font-titling");
            column.Add(headingLabel);

            return column;
        }

        private static void RemoveRowsExceptHeading(VisualElement column)
        {
            for (int i = column.childCount - 1; i >= 1; i--)
            {
                column.RemoveAt(i);
            }
        }
    }
}
