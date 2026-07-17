using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// The seven hazards the main HUD design doc (§9) and mockup wire up: fire, ambient heat, low pressure,
    /// ambient cold, hunger, thirst and restrained. Pressure/radiation/pulling and the "warning vs critical"
    /// variant per hazard exist in the full "Alert Icon Stack" spec but aren't driven by this HUD yet.
    /// </summary>
    public enum AlertHazard
    {
        Fire,
        Hot,
        LowPressure,
        Cold,
        Hunger,
        Thirst,
        Restrained,
    }

    /// <summary>
    /// Which hazards are currently active on the local player. Every field defaults to false (a healthy,
    /// unencumbered character shows an empty stack) - hunger/thirst/restrained/pressure trackers don't exist
    /// in <c>SS3D.Systems</c> yet, so <see cref="SS3D.UI.MainHud.MainHudSubSystem"/> currently never sets these
    /// true. Wire real trackers in here once they exist instead of adding a parallel state model.
    /// </summary>
    public struct AlertStackState
    {
        public bool Fire;
        public bool Hot;
        public bool LowPressure;
        public bool Cold;
        public bool Hunger;
        public bool Thirst;
        public bool Restrained;

        public bool this[AlertHazard hazard] => hazard switch
        {
            AlertHazard.Fire => Fire,
            AlertHazard.Hot => Hot,
            AlertHazard.LowPressure => LowPressure,
            AlertHazard.Cold => Cold,
            AlertHazard.Hunger => Hunger,
            AlertHazard.Thirst => Thirst,
            AlertHazard.Restrained => Restrained,
            _ => false,
        };
    }

    /// <summary>
    /// Top-right icon-only hazard stack. Only active hazards render; hovering any icon reveals its label.
    /// Fire/LowPressure/Restrained render with the "critical" glow border, the rest with the plain "warning"
    /// border - matching the fixed severities used in the Main HUD mockup.
    /// </summary>
    [UxmlElement]
    public partial class AlertIconStack : VisualElement
    {
        private static readonly (AlertHazard Hazard, string Label, bool Critical)[] Chips =
        {
            (AlertHazard.Fire, "Fire", true),
            (AlertHazard.Hot, "Hot", false),
            (AlertHazard.LowPressure, "Low Pressure", true),
            (AlertHazard.Cold, "Cold", false),
            (AlertHazard.Hunger, "Hunger", false),
            (AlertHazard.Thirst, "Thirst", false),
            (AlertHazard.Restrained, "Restrained", true),
        };

        private readonly AlertChip[] _chips;

        public AlertIconStack()
        {
            AddToClassList("alert-icon-stack");

            _chips = new AlertChip[Chips.Length];
            for (int i = 0; i < Chips.Length; i++)
            {
                (AlertHazard hazard, string label, bool critical) = Chips[i];
                AlertChip chip = new(hazard, label, critical);
                _chips[i] = chip;
                Add(chip);
            }
        }

        public void SetState(AlertStackState state)
        {
            foreach (AlertChip chip in _chips)
            {
                chip.style.display = state[chip.Hazard] ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private sealed class AlertChip : VisualElement
        {
            public AlertHazard Hazard { get; }

            public AlertChip(AlertHazard hazard, string label, bool critical)
            {
                Hazard = hazard;
                AddToClassList("alert-chip");
                EnableInClassList("alert-chip--critical", critical);
                style.display = DisplayStyle.None;

                VisualElement box = new();
                box.AddToClassList("alert-chip__box");
                box.Add(new AlertGlyph(hazard));

                Label chipLabel = new(label);
                chipLabel.AddToClassList("alert-chip__label");
                chipLabel.AddToClassList("font-arcade");
                chipLabel.pickingMode = PickingMode.Ignore;

                Add(box);
                Add(chipLabel);

                RegisterCallback<PointerEnterEvent>(_ => AddToClassList("alert-chip--hovered"));
                RegisterCallback<PointerLeaveEvent>(_ => RemoveFromClassList("alert-chip--hovered"));
            }
        }

        /// <summary>
        /// Draws a small flat single-color silhouette per hazard directly via Painter2D, so the alert stack
        /// doesn't depend on new binary icon assets - consistent with the project's "flat, single-color, tinted
        /// at runtime" icon convention, just generated instead of imported.
        /// </summary>
        private sealed class AlertGlyph : VisualElement
        {
            private const float Size = 26f;

            private readonly AlertHazard _hazard;

            public AlertGlyph(AlertHazard hazard)
            {
                _hazard = hazard;
                style.width = Size;
                style.height = Size;
                pickingMode = PickingMode.Ignore;
                generateVisualContent += OnGenerateVisualContent;
            }

            private void OnGenerateVisualContent(MeshGenerationContext context)
            {
                Color color = resolvedStyle.color;
                Painter2D painter = context.painter2D;
                painter.strokeColor = color;
                painter.fillColor = color;
                painter.lineWidth = 1.6f;

                switch (_hazard)
                {
                    case AlertHazard.Fire:
                        DrawFlame(painter);
                        break;
                    case AlertHazard.Hot:
                        DrawThermometer(painter);
                        break;
                    case AlertHazard.LowPressure:
                        DrawChevronsDown(painter);
                        break;
                    case AlertHazard.Cold:
                        DrawSnowflake(painter);
                        break;
                    case AlertHazard.Hunger:
                        DrawFood(painter);
                        break;
                    case AlertHazard.Thirst:
                        DrawDroplet(painter);
                        break;
                    case AlertHazard.Restrained:
                        DrawCuffs(painter);
                        break;
                }
            }

            private static void DrawFlame(Painter2D p)
            {
                p.BeginPath();
                p.MoveTo(new Vector2(13, 3));
                p.BezierCurveTo(new Vector2(16, 9), new Vector2(9, 12), new Vector2(9, 17));
                p.BezierCurveTo(new Vector2(9, 21), new Vector2(12, 23), new Vector2(15, 23));
                p.BezierCurveTo(new Vector2(19, 23), new Vector2(22, 20), new Vector2(20, 15));
                p.BezierCurveTo(new Vector2(19.5f, 18), new Vector2(17, 19), new Vector2(16, 17));
                p.BezierCurveTo(new Vector2(15, 15), new Vector2(17, 12), new Vector2(13, 3));
                p.ClosePath();
                p.Fill();
            }

            private static void DrawThermometer(Painter2D p)
            {
                p.BeginPath();
                p.MoveTo(new Vector2(11, 5));
                p.LineTo(new Vector2(11, 16));
                p.Arc(new Vector2(13, 18), 3.2f, new Angle(210, AngleUnit.Degree), new Angle(120, AngleUnit.Degree));
                p.LineTo(new Vector2(15, 5));
                p.Arc(new Vector2(13, 5), 2, new Angle(0, AngleUnit.Degree), new Angle(180, AngleUnit.Degree));
                p.ClosePath();
                p.Stroke();

                p.BeginPath();
                p.Arc(new Vector2(13, 18), 2.4f, new Angle(0, AngleUnit.Degree), new Angle(360, AngleUnit.Degree));
                p.Fill();
            }

            private static void DrawChevronsDown(Painter2D p)
            {
                p.BeginPath();
                p.MoveTo(new Vector2(6, 8));
                p.LineTo(new Vector2(13, 14));
                p.LineTo(new Vector2(20, 8));
                p.Stroke();

                p.BeginPath();
                p.MoveTo(new Vector2(6, 15));
                p.LineTo(new Vector2(13, 21));
                p.LineTo(new Vector2(20, 15));
                p.Stroke();
            }

            private static void DrawSnowflake(Painter2D p)
            {
                Vector2 center = new(13, 13);
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * 60f * Mathf.Deg2Rad;
                    Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
                    p.BeginPath();
                    p.MoveTo(center - dir * 9f);
                    p.LineTo(center + dir * 9f);
                    p.Stroke();
                }
            }

            private static void DrawFood(Painter2D p)
            {
                p.BeginPath();
                p.Arc(new Vector2(13, 13), 8f, new Angle(30, AngleUnit.Degree), new Angle(330, AngleUnit.Degree));
                p.LineTo(new Vector2(13, 13));
                p.ClosePath();
                p.Fill();
            }

            private static void DrawDroplet(Painter2D p)
            {
                p.BeginPath();
                p.MoveTo(new Vector2(13, 3));
                p.BezierCurveTo(new Vector2(19, 11), new Vector2(21, 15), new Vector2(21, 17.5f));
                p.Arc(new Vector2(13, 17.5f), 8f, new Angle(0, AngleUnit.Degree), new Angle(180, AngleUnit.Degree));
                p.BezierCurveTo(new Vector2(5, 15), new Vector2(7, 11), new Vector2(13, 3));
                p.ClosePath();
                p.Fill();
            }

            private static void DrawCuffs(Painter2D p)
            {
                p.BeginPath();
                p.Arc(new Vector2(9, 15), 4.2f, new Angle(0, AngleUnit.Degree), new Angle(360, AngleUnit.Degree));
                p.Stroke();

                p.BeginPath();
                p.Arc(new Vector2(18, 15), 4.2f, new Angle(0, AngleUnit.Degree), new Angle(360, AngleUnit.Degree));
                p.Stroke();

                p.BeginPath();
                p.MoveTo(new Vector2(12.6f, 15));
                p.LineTo(new Vector2(14.4f, 15));
                p.Stroke();
            }
        }
    }
}
