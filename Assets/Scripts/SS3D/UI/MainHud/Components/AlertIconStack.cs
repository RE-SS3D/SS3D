using DG.Tweening;
using SS3D.UI.MainHud;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Hazards the alert icon stack can show. Base set matches main-hud.md §9; Bleeding and
    /// CardiacArrest are fork additions (health vitals) not yet in that design table.
    /// Health hazards (LowOxygen, Dying, Bleeding, CardiacArrest) are live via
    /// <see cref="SS3D.Systems.Health.HealthAlertStackMapper"/>; turf Hot/Cold/pressure/Fire
    /// (+ turf LowOxygen), SealBreach and TankLow via <see cref="SS3D.Systems.Health.AtmosAlertStackMapper"/>. Remaining
    /// hazards stay None until those systems exist (F4 / <c>alertstack</c> full-stack override).
    /// </summary>
    public enum AlertHazard
    {
        Fire,
        Hot,
        Cold,
        LowPressure,
        HighPressure,
        Radiation,
        Hunger,
        Thirst,
        Pulling,
        Restrained,
        LowOxygen,
        Dying,
        Bleeding,
        CardiacArrest,
        SealBreach,
        Invincible,
        TankLow,
    }

    /// <summary>
    /// How urgently a hazard reads: hidden, a plain amber warning, or a pulsing red critical.
    /// <see cref="AlertHazard.Dying"/> and <see cref="AlertHazard.CardiacArrest"/> never use
    /// <see cref="Warning"/> — they either aren't happening or they're critical.
    /// </summary>
    public enum AlertSeverity
    {
        None,
        Warning,
        Critical,
    }

    /// <summary>
    /// Which hazards are currently active on the local player, and at what severity. Every field defaults to
    /// <see cref="AlertSeverity.None"/> (a healthy, unencumbered character shows an empty stack).
    /// <see cref="MainHudSubSystem"/> fills health fields from <see cref="SS3D.Systems.Health.HealthAlertStackMapper"/>
    /// unless a debug override is set; other systems should write into this struct the same way when they exist.
    /// </summary>
    public struct AlertStackState
    {
        public AlertSeverity Fire;
        public AlertSeverity Hot;
        public AlertSeverity Cold;
        public AlertSeverity LowPressure;
        public AlertSeverity HighPressure;
        public AlertSeverity Radiation;
        public AlertSeverity Hunger;
        public AlertSeverity Thirst;
        public AlertSeverity Pulling;
        public AlertSeverity Restrained;
        public AlertSeverity LowOxygen;
        public AlertSeverity Dying;
        public AlertSeverity Bleeding;
        public AlertSeverity CardiacArrest;
        public AlertSeverity SealBreach;
        public AlertSeverity Invincible;
        public AlertSeverity TankLow;

        public AlertSeverity this[AlertHazard hazard] => hazard switch
        {
            AlertHazard.Fire => Fire,
            AlertHazard.Hot => Hot,
            AlertHazard.Cold => Cold,
            AlertHazard.LowPressure => LowPressure,
            AlertHazard.HighPressure => HighPressure,
            AlertHazard.Radiation => Radiation,
            AlertHazard.Hunger => Hunger,
            AlertHazard.Thirst => Thirst,
            AlertHazard.Pulling => Pulling,
            AlertHazard.Restrained => Restrained,
            AlertHazard.LowOxygen => LowOxygen,
            AlertHazard.Dying => Dying,
            AlertHazard.Bleeding => Bleeding,
            AlertHazard.CardiacArrest => CardiacArrest,
            AlertHazard.SealBreach => SealBreach,
            AlertHazard.Invincible => Invincible,
            AlertHazard.TankLow => TankLow,
            _ => AlertSeverity.None,
        };
    }

    /// <summary>
    /// Top-right icon-only hazard stack. Only active hazards render; hovering any icon reveals its label.
    /// Each chip re-styles between the plain "warning" border and the pulsing "critical" glow based on its
    /// live <see cref="AlertSeverity"/>, matching the Main HUD mockup.
    /// </summary>
    [UxmlElement]
    public partial class AlertIconStack : VisualElement
    {
        private static readonly (AlertHazard Hazard, string Label)[] Chips =
        {
            (AlertHazard.Fire, "Fire"),
            (AlertHazard.Hot, "Hot"),
            (AlertHazard.Cold, "Cold"),
            (AlertHazard.LowPressure, "Low Pressure"),
            (AlertHazard.HighPressure, "High Pressure"),
            (AlertHazard.Radiation, "Radiation"),
            (AlertHazard.Hunger, "Hunger"),
            (AlertHazard.Thirst, "Thirst"),
            (AlertHazard.Pulling, "Pulling"),
            (AlertHazard.Restrained, "Restrained"),
            (AlertHazard.LowOxygen, "Low Oxygen"),
            (AlertHazard.Dying, "Dying / Critical"),
            (AlertHazard.Bleeding, "Bleeding"),
            (AlertHazard.CardiacArrest, "Cardiac Arrest"),
            (AlertHazard.SealBreach, "Seal Breach"),
            (AlertHazard.Invincible, "Invincible (Map Editor)"),
            (AlertHazard.TankLow, "Tank Low"),
        };

        private readonly AlertChip[] _chips;

        // Parameterless ctor required by [UxmlElement]; real construction happens via the icon-set overload,
        // called from MainHudView once the catalog's AlertIconSet is available.
        public AlertIconStack()
            : this(default)
        {
        }

        public AlertIconStack(AlertIconSet icons)
        {
            AddToClassList("alert-icon-stack");

            _chips = new AlertChip[Chips.Length];
            for (int i = 0; i < Chips.Length; i++)
            {
                (AlertHazard hazard, string label) = Chips[i];
                AlertChip chip = new(hazard, label, icons[hazard]);
                _chips[i] = chip;
                Add(chip);
            }
        }

        public void SetState(AlertStackState state)
        {
            foreach (AlertChip chip in _chips)
            {
                chip.SetSeverity(state[chip.Hazard]);
            }
        }

        private sealed class AlertChip : VisualElement
        {
            private const float BorderPulseDuration = 0.5f;

            // #b94848 (--ss3d-feedback-danger) at low vs full alpha — only the stroke blinks.
            private static readonly Color BorderCriticalDim = new(0.725f, 0.282f, 0.282f, 0.25f);
            private static readonly Color BorderCriticalBright = new(0.9f, 0.32f, 0.32f, 1f);

            public AlertHazard Hazard { get; }

            private readonly VisualElement _box;
            private Tween _borderTween;
            private float _borderPulse;
            private AlertSeverity _severity;

            public AlertChip(AlertHazard hazard, string label, Sprite icon)
            {
                Hazard = hazard;
                AddToClassList("alert-chip");
                style.display = DisplayStyle.None;

                _box = new VisualElement();
                _box.AddToClassList("alert-chip__box");

                VisualElement glyph = new();
                glyph.AddToClassList("alert-chip__glyph");
                glyph.pickingMode = PickingMode.Ignore;
                if (icon != null)
                {
                    glyph.style.backgroundImage = new StyleBackground(icon);
                }

                _box.Add(glyph);

                Label chipLabel = new(label);
                chipLabel.AddToClassList("alert-chip__label");
                chipLabel.AddToClassList("font-arcade");
                chipLabel.pickingMode = PickingMode.Ignore;

                // A chosen state, not a passing hazard: say where it came from without a hover.
                EnableInClassList("alert-chip--labelled", hazard == AlertHazard.Invincible);

                Add(_box);
                Add(chipLabel);

                RegisterCallback<PointerEnterEvent>(_ => AddToClassList("alert-chip--hovered"));
                RegisterCallback<PointerLeaveEvent>(_ => RemoveFromClassList("alert-chip--hovered"));
                RegisterCallback<DetachFromPanelEvent>(_ => StopBorderPulse());
            }

            public void SetSeverity(AlertSeverity severity)
            {
                if (_severity == severity)
                {
                    return;
                }

                _severity = severity;
                style.display = severity == AlertSeverity.None ? DisplayStyle.None : DisplayStyle.Flex;
                EnableInClassList("alert-chip--critical", severity == AlertSeverity.Critical);

                if (severity == AlertSeverity.Critical)
                {
                    StartBorderPulse();
                }
                else
                {
                    StopBorderPulse();
                }
            }

            // Pulses only the box's rounded border color. UITK has no CSS keyframe equivalent, so DOTween
            // drives it the same way MainHudView drives show/hide.
            private void StartBorderPulse()
            {
                StopBorderPulse();
                _borderPulse = 0f;
                ApplyBorderColor(BorderCriticalDim);
                _borderTween = DOTween.To(
                        () => _borderPulse,
                        value =>
                        {
                            _borderPulse = value;
                            ApplyBorderColor(Color.Lerp(BorderCriticalDim, BorderCriticalBright, value));
                        },
                        1f,
                        BorderPulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true);
            }

            private void StopBorderPulse()
            {
                _borderTween?.Kill();
                _borderTween = null;
                _borderPulse = 0f;
                // Drop inline overrides so USS warning/critical border-color applies again.
                _box.style.borderTopColor = StyleKeyword.Null;
                _box.style.borderRightColor = StyleKeyword.Null;
                _box.style.borderBottomColor = StyleKeyword.Null;
                _box.style.borderLeftColor = StyleKeyword.Null;
            }

            private void ApplyBorderColor(Color color)
            {
                _box.style.borderTopColor = color;
                _box.style.borderRightColor = color;
                _box.style.borderBottomColor = color;
                _box.style.borderLeftColor = color;
            }
        }
    }
}
