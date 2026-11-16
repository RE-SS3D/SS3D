using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Comms.UI
{
    /// <summary>
    /// UI Toolkit view for floating local-speech subtitle chips and the crowd-cap overflow chip.
    /// Visual language follows the Claude Design "weighted chips" mock (option 1a).
    /// </summary>
    public sealed class LocalSpeechBubbleView
    {
        private static readonly string[] ModeClasses =
        {
            "comms-subtitle--speak",
            "comms-subtitle--whisper",
            "comms-subtitle--shout",
            "comms-subtitle--emote",
            "comms-subtitle--action",
            "comms-subtitle--radio",
            "comms-subtitle--announce",
        };

        private readonly StyleSheet _bubbleStyleSheet;
        private readonly List<VisualElement> _subtitlePool = new();

        private VisualElement _overlayRoot;
        private VisualElement _overflowChip;
        private Label _overflowLabel;

        private VisualElement _draftChip;
        private Label _draftName;
        private Label _draftMeasure;
        private TextField _draftField;
        private SpeechMode _draftMode = SpeechMode.Speak;
        private bool _draftShown;
        private bool _retainDraftFocus;
        private float _draftAnchorLeft;
        private float _draftAnchorBottom;
        private float _draftChipWidth;

        private static readonly string[] DraftModeClasses =
        {
            "comms-draft--speak",
            "comms-draft--whisper",
            "comms-draft--shout",
        };

        private sealed class SubtitleLabels
        {
            public Label Name;
            public Label Line;
        }

        public TextField DraftField => _draftField;
        public bool IsDraftVisible => _draftChip != null && _draftChip.resolvedStyle.display != DisplayStyle.None;

        public LocalSpeechBubbleView(StyleSheet bubbleStyleSheet)
        {
            _bubbleStyleSheet = bubbleStyleSheet;
        }

        public void Attach(VisualElement overlayRoot)
        {
            _overlayRoot = overlayRoot;
            _overlayRoot.style.flexGrow = 1;
            _overlayRoot.pickingMode = PickingMode.Ignore;

            if (_bubbleStyleSheet != null)
            {
                _overlayRoot.styleSheets.Add(_bubbleStyleSheet);
            }

            BuildDraftChip();

            _overflowChip = new VisualElement();
            _overflowChip.AddToClassList("comms-overflow-chip");
            _overflowChip.pickingMode = PickingMode.Ignore;
            _overflowChip.style.display = DisplayStyle.None;

            _overflowLabel = new Label();
            _overflowLabel.AddToClassList("font-body");
            _overflowLabel.AddToClassList("comms-overflow-chip__label");
            _overflowChip.Add(_overflowLabel);

            _overlayRoot.Add(_overflowChip);
        }

        public void Detach()
        {
            foreach (VisualElement subtitle in _subtitlePool)
            {
                subtitle.RemoveFromHierarchy();
            }

            _subtitlePool.Clear();

            _draftChip?.RemoveFromHierarchy();
            if (_draftField != null)
            {
                _draftField.UnregisterValueChangedCallback(OnDraftValueChanged);
            }

            _draftChip = null;
            _draftName = null;
            _draftMeasure = null;
            _draftField = null;
            _draftShown = false;
            _retainDraftFocus = false;

            _overflowChip?.RemoveFromHierarchy();
            _overflowChip = null;
            _overflowLabel = null;
            _overlayRoot = null;
        }

        /// <summary>
        /// Positions and fills a pooled subtitle. Screen coords are bottom-left origin.
        /// <paramref name="stackAge"/> is 0 for the newest line, 1+ for older stacked lines.
        /// </summary>
        public void ShowBubble(
            int poolIndex,
            float left,
            float bottom,
            string speakerName,
            string displayText,
            SpeechMode mode,
            AudibilityTier tier,
            float opacity,
            int stackAge)
        {
            VisualElement subtitle = GetOrCreateSubtitle(poolIndex);
            subtitle.style.display = DisplayStyle.Flex;
            subtitle.style.left = left;
            subtitle.style.bottom = bottom;
            subtitle.style.opacity = opacity;

            subtitle.EnableInClassList("comms-subtitle--clear", tier == AudibilityTier.Clear);
            subtitle.EnableInClassList("comms-subtitle--muffled", tier == AudibilityTier.Muffled);
            subtitle.EnableInClassList("comms-subtitle--aged", stackAge > 0);
            subtitle.EnableInClassList("comms-subtitle--aged-far", stackAge > 1);
            ApplyModeClass(subtitle, mode);

            SubtitleLabels labels = (SubtitleLabels)subtitle.userData;
            bool showName = !string.IsNullOrEmpty(speakerName);
            string nameText = showName ? speakerName.ToUpperInvariant() : string.Empty;
            labels.Name.style.display = showName ? DisplayStyle.Flex : DisplayStyle.None;
            labels.Name.text = nameText;
            labels.Line.text = displayText;
            labels.Line.style.unityFontStyleAndWeight = mode switch
            {
                SpeechMode.Shout => FontStyle.Bold,
                SpeechMode.Whisper or SpeechMode.Emote or SpeechMode.Action => FontStyle.Italic,
                _ => FontStyle.Normal,
            };

            FitChipWidth(subtitle, labels, nameText, displayText, mode, stackAge, showName);
        }

        public void HideBubblesFrom(int fromIndex)
        {
            for (int i = fromIndex; i < _subtitlePool.Count; i++)
            {
                _subtitlePool[i].style.display = DisplayStyle.None;
            }
        }

        public void ShowOverflowChip(int hiddenCount)
        {
            if (_overflowChip == null)
            {
                return;
            }

            _overflowChip.style.display = DisplayStyle.Flex;
            _overflowLabel.text = $"+{hiddenCount} more talking nearby";
        }

        public void HideOverflowChip()
        {
            if (_overflowChip != null)
            {
                _overflowChip.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Positions the live draft chip at the same screen anchor a finished line would use.
        /// Always shows channel chrome: LOCAL, radio header, or ALL-STATION for announcements.
        /// </summary>
        public void ShowDraft(
            float left,
            float bottom,
            string speakerName,
            SpeechMode mode,
            string channelHeader = null,
            bool isAnnouncement = false)
        {
            if (_draftChip == null)
            {
                return;
            }

            _ = speakerName;

            bool justOpened = !_draftShown;
            _draftShown = true;
            _draftChip.style.display = DisplayStyle.Flex;
            _draftAnchorLeft = left;
            _draftAnchorBottom = bottom;

            bool isChannelled = isAnnouncement || !string.IsNullOrEmpty(channelHeader);
            string nameText = isAnnouncement
                ? (string.IsNullOrEmpty(channelHeader) ? "ALL-STATION" : channelHeader.ToUpperInvariant())
                : isChannelled
                    ? channelHeader.ToUpperInvariant()
                    : "LOCAL";

            SpeechMode effectiveMode = isAnnouncement
                ? SpeechMode.Announcement
                : isChannelled
                    ? SpeechMode.Speak
                    : mode;
            if (justOpened || _draftMode != effectiveMode)
            {
                ApplyDraftModeClass(effectiveMode);
            }

            ApplyDraftChannelChrome(nameText, isRadio: isChannelled && !isAnnouncement, isAnnouncement);
            FitDraftChipWidth();
        }

        /// <summary>
        /// Force channel header styles in code — USS alone is easy to lose under .font-body /
        /// TextField defaults, and the field's negative margin used to cover the name.
        /// </summary>
        private void ApplyDraftChannelChrome(string header, bool isRadio, bool isAnnouncement)
        {
            if (_draftName == null)
            {
                return;
            }

            _draftName.text = header;
            _draftName.style.display = DisplayStyle.Flex;
            _draftName.style.visibility = Visibility.Visible;
            _draftName.style.opacity = 1f;
            _draftName.style.flexGrow = 0;
            _draftName.style.flexShrink = 0;
            _draftName.style.minHeight = 14f;
            _draftName.style.marginTop = 0;
            _draftName.style.marginBottom = 2f;
            _draftName.style.paddingTop = 0;
            _draftName.style.paddingBottom = 0;
            _draftName.style.fontSize = isRadio || isAnnouncement ? 13f : 14f;
            _draftName.style.letterSpacing = isAnnouncement ? 2f : 1f;
            _draftName.style.unityTextAlign = isRadio || isAnnouncement
                ? TextAnchor.MiddleLeft
                : TextAnchor.MiddleCenter;
            _draftName.style.color = isAnnouncement
                ? new Color(240f / 255f, 196f / 255f, 125f / 255f, 1f)
                : isRadio
                    ? new Color(95f / 255f, 134f / 255f, 179f / 255f, 1f)
                    : new Color(200f / 255f, 200f / 255f, 190f / 255f, 1f);

            _draftName.EnableInClassList("font-titling", false);
            _draftName.EnableInClassList("font-terminal", !isAnnouncement);
            _draftName.EnableInClassList("font-arcade", isAnnouncement);
            _draftName.EnableInClassList("comms-draft__name--radio", isRadio);
            _draftName.EnableInClassList("comms-draft__name--announce", isAnnouncement);

            _draftChip.EnableInClassList("comms-draft--radio", isRadio);
            _draftChip.EnableInClassList("comms-draft--announce", isAnnouncement);
            _draftChip.EnableInClassList("comms-draft--channelled", isRadio || isAnnouncement);

            if (_draftField != null)
            {
                _draftField.style.marginTop = 0;
            }
        }

        /// <summary>
        /// Head-anchored position without USS translate:-50%. UITK keeps a stale translate
        /// transform while draft width changes every keystroke (worldBound left sticks; center drifts).
        /// </summary>
        private void ApplyDraftScreenPosition()
        {
            if (_draftChip == null)
            {
                return;
            }

            _draftChip.style.translate = new Translate(0, 0);
            float width = _draftChipWidth > 0f ? _draftChipWidth : _draftChip.resolvedStyle.width;
            if (float.IsNaN(width) || width < 0f)
            {
                width = 0f;
            }

            _draftChip.style.left = _draftAnchorLeft - (width * 0.5f);
            _draftChip.style.bottom = _draftAnchorBottom;
        }

        public void HideDraft()
        {
            if (_draftChip == null)
            {
                return;
            }

            _retainDraftFocus = false;
            _draftShown = false;
            _draftChip.style.display = DisplayStyle.None;
            if (_draftField != null)
            {
                _draftField.value = string.Empty;
                _draftField.Blur();
            }
        }

        /// <summary>
        /// While true, world clicks that blur the field immediately restore focus so TextEntry
        /// cannot leave the player unable to type or move.
        /// </summary>
        public void SetRetainDraftFocus(bool retain)
        {
            _retainDraftFocus = retain;
        }

        public void FocusDraft()
        {
            if (_draftField == null)
            {
                return;
            }

            // Delay past the T press that opened compose so it isn't typed into the field.
            _draftField.schedule.Execute(() =>
            {
                if (_draftField == null || !_draftShown)
                {
                    return;
                }

                _draftField.Focus();
                int len = _draftField.value?.Length ?? 0;
                _draftField.SelectRange(len, len);
                FitDraftChipWidth();
            }).ExecuteLater(1);
        }

        private void BuildDraftChip()
        {
            _draftChip = new VisualElement();
            _draftChip.AddToClassList("comms-draft");
            _draftChip.AddToClassList("comms-draft--speak");
            _draftChip.pickingMode = PickingMode.Position;
            _draftChip.style.position = Position.Absolute;
            _draftChip.style.display = DisplayStyle.None;
            _draftChip.style.flexGrow = 0;
            _draftChip.style.flexShrink = 0;
            _draftChip.style.translate = new Translate(0, 0);
            _draftChip.generateVisualContent += PaintDashedOutline;

            _draftName = new Label("LOCAL");
            _draftName.AddToClassList("font-terminal");
            _draftName.AddToClassList("comms-draft__name");
            _draftName.pickingMode = PickingMode.Ignore;
            _draftChip.Add(_draftName);

            // Off-screen measure proxy — TextField.MeasureTextSize returns the *current* laid-out
            // width once style.width is set, so hug-sizing never updates until a mode/wrap change.
            _draftMeasure = new Label();
            _draftMeasure.AddToClassList("font-body");
            _draftMeasure.AddToClassList("comms-draft__measure");
            _draftMeasure.pickingMode = PickingMode.Ignore;
            _draftChip.Add(_draftMeasure);

            // Visible TextField — real UITK caret, same wrap budget as finished chips.
            // multiline allows soft wrap; Enter is intercepted (never inserts a newline).
            _draftField = new TextField { multiline = true, maxLength = 256, value = string.Empty };
            _draftField.AddToClassList("font-body");
            _draftField.AddToClassList("comms-draft__field");
            // Exclude from UITK Tab focus ring so Tab can cycle comms channels instead.
            _draftField.tabIndex = -1;
            _draftField.focusable = true;
            VisualElement textInput = _draftField.Q(TextField.textInputUssName);
            if (textInput != null)
            {
                textInput.tabIndex = -1;
            }

            _draftField.RegisterValueChangedCallback(OnDraftValueChanged);
            _draftField.RegisterCallback<FocusOutEvent>(HandleDraftFocusOut);
            _draftChip.Add(_draftField);

            _overlayRoot.Add(_draftChip);
        }

        private void HandleDraftFocusOut(FocusOutEvent _)
        {
            if (!_retainDraftFocus || !_draftShown || _draftField == null)
            {
                return;
            }

            // Defer past the click that stole focus; otherwise Focus() is a no-op.
            _draftField.schedule.Execute(() =>
            {
                if (!_retainDraftFocus || !_draftShown || _draftField == null)
                {
                    return;
                }

                _draftField.Focus();
                int len = _draftField.value?.Length ?? 0;
                _draftField.SelectRange(len, len);
            }).ExecuteLater(0);
        }

        private void ApplyDraftModeClass(SpeechMode mode)
        {
            _draftMode = mode;
            for (int i = 0; i < DraftModeClasses.Length; i++)
            {
                _draftChip.EnableInClassList(DraftModeClasses[i], false);
            }

            string modeClass = mode switch
            {
                SpeechMode.Whisper => "comms-draft--whisper",
                SpeechMode.Shout => "comms-draft--shout",
                _ => "comms-draft--speak",
            };

            _draftChip.EnableInClassList(modeClass, true);
            ApplyDraftTypeStyles(mode);

            _draftChip.MarkDirtyRepaint();
            FitDraftChipWidth();
        }

        /// <summary>
        /// TextField ignores many nested USS text rules (same as unityTextAlign). Force size/color
        /// in code so .font-body's 11px secondary gray cannot win.
        /// </summary>
        private void ApplyDraftTypeStyles(SpeechMode mode)
        {
            if (_draftField == null)
            {
                return;
            }

            float fontSize = mode switch
            {
                SpeechMode.Whisper => 16f,
                SpeechMode.Shout => 23f,
                _ => 21f,
            };
            FontStyle fontStyle = mode switch
            {
                SpeechMode.Shout => FontStyle.Bold,
                SpeechMode.Whisper => FontStyle.Italic,
                _ => FontStyle.Normal,
            };
            Color textColor = mode switch
            {
                SpeechMode.Whisper => new Color(235f / 255f, 235f / 255f, 235f / 255f, 1f),
                SpeechMode.Shout => Color.white,
                _ => new Color(245f / 255f, 245f / 255f, 245f / 255f, 1f),
            };
            float letterSpacing = mode is SpeechMode.Whisper or SpeechMode.Shout ? 1f : 0f;

            ApplyTypeToElement(_draftField, fontSize, fontStyle, textColor, letterSpacing);
            if (_draftMeasure != null)
            {
                ApplyTypeToElement(_draftMeasure, fontSize, fontStyle, textColor, letterSpacing);
            }

            VisualElement textInput = _draftField.Q(className: "unity-base-text-field__input");
            if (textInput != null)
            {
                ApplyTypeToElement(textInput, fontSize, fontStyle, textColor, letterSpacing);
                textInput.style.unityTextAlign = TextAnchor.MiddleCenter;
            }

            // Multiline TextField hosts text in nested TextElement / Label children.
            _draftField.Query(className: "unity-base-text-field__input").ForEach(input =>
            {
                input.Query<TextElement>().ForEach(te =>
                    ApplyTypeToElement(te, fontSize, fontStyle, textColor, letterSpacing));
                input.Query<Label>().ForEach(label =>
                    ApplyTypeToElement(label, fontSize, fontStyle, textColor, letterSpacing));
            });
        }

        private static void ApplyTypeToElement(
            VisualElement element, float fontSize, FontStyle fontStyle, Color color, float letterSpacing)
        {
            element.style.fontSize = fontSize;
            element.style.unityFontStyleAndWeight = fontStyle;
            element.style.color = color;
            element.style.letterSpacing = letterSpacing;
        }

        private void OnDraftValueChanged(ChangeEvent<string> evt)
        {
            FitDraftChipWidth();
            // Second pass after Yoga applies the new width (TextField measure is layout-sensitive).
            _draftField?.schedule.Execute(FitDraftChipWidth).ExecuteLater(0);
        }

        /// <summary>
        /// Same hug/wrap rules as finished chips, applied to the draft TextField.
        /// Measures via an off-screen Label so prior style.width cannot poison the result.
        /// </summary>
        private void FitDraftChipWidth()
        {
            if (_draftChip == null || _draftField == null || _draftMeasure == null)
            {
                return;
            }

            float maxChip = _draftMode switch
            {
                SpeechMode.Whisper => 300f,
                SpeechMode.Shout => 480f,
                _ => 420f,
            };

            float padX = _draftMode switch
            {
                SpeechMode.Whisper => 20f,
                SpeechMode.Shout => 32f,
                _ => 28f,
            };

            float maxContent = Mathf.Max(8f, maxChip - padX);
            float letterSpacing = _draftMode is SpeechMode.Whisper or SpeechMode.Shout ? 1f : 0f;
            string lineText = _draftField.value ?? string.Empty;
            // Prefer inline display (resolvedStyle can still be None the frame the chip opens).
            bool showName = _draftName != null
                && _draftName.style.display != DisplayStyle.None
                && !string.IsNullOrEmpty(_draftName.text);
            string nameText = showName ? _draftName.text : string.Empty;

            _draftName.style.maxWidth = maxContent;
            _draftChip.style.maxWidth = maxChip;

            if (string.IsNullOrEmpty(lineText))
            {
                _draftField.style.whiteSpace = WhiteSpace.NoWrap;
                _draftField.style.width = 12f;
                _draftField.style.maxWidth = maxContent;
                // Explicit width so ApplyDraftScreenPosition can center with left = headX - w/2
                // (USS translate:-50% keeps a stale render transform while width changes each keystroke).
                float emptyNameW = 0f;
                if (showName)
                {
                    emptyNameW = _draftName.MeasureTextSize(
                        nameText, 4096f, VisualElement.MeasureMode.AtMost, 0f, VisualElement.MeasureMode.Undefined).x;
                    if (!IsPlausibleTextWidth(nameText, emptyNameW))
                    {
                        emptyNameW = nameText.Length * 8f;
                    }
                }

                // Keep enough width for the channel header even with an empty caret.
                float minInner = showName ? Mathf.Max(emptyNameW, 56f) : 12f;
                _draftChip.style.width = Mathf.Min(Mathf.Ceil(minInner + padX), maxChip);
                _draftChipWidth = _draftChip.style.width.value.value;
                ApplyDraftScreenPosition();
                return;
            }

            _draftMeasure.text = lineText;
            // Unconstrained horizontal measure (AtMost large), independent of the field's current width.
            Vector2 naturalLine = _draftMeasure.MeasureTextSize(
                lineText, 4096f, VisualElement.MeasureMode.AtMost, 0f, VisualElement.MeasureMode.Undefined);

            float contentWidth;
            if (IsPlausibleTextWidth(lineText, naturalLine.x))
            {
                contentWidth = naturalLine.x + LetterSpacingExtra(lineText, letterSpacing);
            }
            else
            {
                float fontSize = _draftMeasure.resolvedStyle.fontSize;
                if (fontSize <= 0f)
                {
                    fontSize = _draftMode switch
                    {
                        SpeechMode.Whisper => 16f,
                        SpeechMode.Shout => 23f,
                        _ => 21f,
                    };
                }

                contentWidth = lineText.Length * fontSize * 0.55f + LetterSpacingExtra(lineText, letterSpacing);
            }

            // Do NOT widen the field to the name width — finished chips hug each line separately and
            // center via align-items. Widening + UpperLeft text made short drafts look left-aligned.
            // Force type + center align in code: USS text rules do not stick on TextField (font-body
            // otherwise wins at 11px secondary gray).
            ApplyDraftTypeStyles(_draftMode);
            _draftField.style.unityTextAlign = TextAnchor.MiddleCenter;
            VisualElement textInput = _draftField.Q(className: "unity-base-text-field__input");
            if (textInput != null)
            {
                textInput.style.unityTextAlign = TextAnchor.MiddleCenter;
            }

            float nameMeasured = 0f;
            if (showName)
            {
                nameMeasured = _draftName.MeasureTextSize(
                    nameText, 4096f, VisualElement.MeasureMode.AtMost, 0f, VisualElement.MeasureMode.Undefined).x;
            }

            const float MeasureSlack = 10f;
            bool hug = contentWidth + MeasureSlack <= maxContent;
            float assignedFieldWidth;
            if (hug)
            {
                assignedFieldWidth = Mathf.Ceil(contentWidth + 4f);
                _draftField.style.whiteSpace = WhiteSpace.NoWrap;
                _draftField.style.width = assignedFieldWidth;
                _draftField.style.maxWidth = maxContent;
                float hugInner = Mathf.Max(nameMeasured, assignedFieldWidth);
                _draftChip.style.width = Mathf.Min(Mathf.Ceil(hugInner + padX), maxChip);
            }
            else
            {
                assignedFieldWidth = maxContent;
                _draftField.style.whiteSpace = WhiteSpace.Normal;
                _draftField.style.width = maxContent;
                _draftField.style.maxWidth = maxContent;
                _draftChip.style.width = maxChip;
            }

            _draftChipWidth = _draftChip.style.width.value.value;
            ApplyDraftScreenPosition();
        }

        /// <summary>
        /// UITK has no border-style:dashed — paint mock 2a's live/unsent outline on the
        /// outer padding-box edge (not inset on contentRect inside the padding).
        /// </summary>
        private static void PaintDashedOutline(MeshGenerationContext context)
        {
            VisualElement element = context.visualElement;
            Rect content = element.contentRect;
            if (content.width < 1f || content.height < 1f)
            {
                return;
            }

            IResolvedStyle style = element.resolvedStyle;
            // contentRect is inside padding; expand back out to the padding-box / visual plate edge.
            Rect outer = new(
                content.xMin - style.paddingLeft,
                content.yMin - style.paddingTop,
                content.width + style.paddingLeft + style.paddingRight,
                content.height + style.paddingTop + style.paddingBottom);

            if (outer.width < 2f || outer.height < 2f)
            {
                return;
            }

            float radius = style.borderTopLeftRadius;
            Color color = ResolveDraftOutlineColor(element);

            Painter2D painter = context.painter2D;
            painter.strokeColor = color;
            painter.lineWidth = 1f;
            painter.lineCap = LineCap.Butt;

            const float dash = 4f;
            const float gap = 3f;
            DrawDashedRoundedRect(painter, outer, radius, dash, gap);
        }

        private static Color ResolveDraftOutlineColor(VisualElement element)
        {
            if (element.ClassListContains("comms-draft--whisper"))
            {
                return new Color(0.45f, 0.45f, 0.45f, 0.85f);
            }

            // --ss3d-border-strong
            return new Color(0.24f, 0.25f, 0.29f, 1f);
        }

        private static void DrawDashedRoundedRect(
            Painter2D painter, Rect rect, float radius, float dash, float gap)
        {
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            float left = rect.xMin + 0.5f;
            float right = rect.xMax - 0.5f;
            float top = rect.yMin + 0.5f;
            float bottom = rect.yMax - 0.5f;

            // Flattened perimeter: top, right, bottom, left (straight runs), plus four corner arcs.
            StrokeDashedLine(painter, new Vector2(left + radius, top), new Vector2(right - radius, top), dash, gap);
            StrokeDashedArc(painter, new Vector2(right - radius, top + radius), radius, -90f, 0f, dash, gap);
            StrokeDashedLine(painter, new Vector2(right, top + radius), new Vector2(right, bottom - radius), dash, gap);
            StrokeDashedArc(painter, new Vector2(right - radius, bottom - radius), radius, 0f, 90f, dash, gap);
            StrokeDashedLine(painter, new Vector2(right - radius, bottom), new Vector2(left + radius, bottom), dash, gap);
            StrokeDashedArc(painter, new Vector2(left + radius, bottom - radius), radius, 90f, 180f, dash, gap);
            StrokeDashedLine(painter, new Vector2(left, bottom - radius), new Vector2(left, top + radius), dash, gap);
            StrokeDashedArc(painter, new Vector2(left + radius, top + radius), radius, 180f, 270f, dash, gap);
        }

        private static void StrokeDashedLine(
            Painter2D painter, Vector2 from, Vector2 to, float dash, float gap)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            Vector2 dir = delta / length;
            float cursor = 0f;
            bool draw = true;
            while (cursor < length)
            {
                float segment = draw ? dash : gap;
                float next = Mathf.Min(cursor + segment, length);
                if (draw && next > cursor)
                {
                    painter.BeginPath();
                    painter.MoveTo(from + dir * cursor);
                    painter.LineTo(from + dir * next);
                    painter.Stroke();
                }

                cursor = next;
                draw = !draw;
            }
        }

        private static void StrokeDashedArc(
            Painter2D painter, Vector2 center, float radius, float startDeg, float endDeg, float dash, float gap)
        {
            if (radius < 0.5f)
            {
                return;
            }

            float startRad = startDeg * Mathf.Deg2Rad;
            float endRad = endDeg * Mathf.Deg2Rad;
            float arcLen = Mathf.Abs(endRad - startRad) * radius;
            float cursor = 0f;
            bool draw = true;
            float sign = endRad >= startRad ? 1f : -1f;

            while (cursor < arcLen)
            {
                float segment = draw ? dash : gap;
                float next = Mathf.Min(cursor + segment, arcLen);
                if (draw && next > cursor)
                {
                    float a0 = startRad + sign * (cursor / radius);
                    float a1 = startRad + sign * (next / radius);
                    painter.BeginPath();
                    painter.MoveTo(center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius);
                    const int steps = 4;
                    for (int i = 1; i <= steps; i++)
                    {
                        float t = i / (float)steps;
                        float a = Mathf.Lerp(a0, a1, t);
                        painter.LineTo(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                    }

                    painter.Stroke();
                }

                cursor = next;
                draw = !draw;
            }
        }

        /// <summary>
        /// Hug content for short lines; only pin the chip to max-width when text must wrap.
        /// Never shrink-fit via binary search — that under-measured (esp. with letter-spacing)
        /// and forced breaks like "NEED / OXYGEN".
        /// </summary>
        private static void FitChipWidth(
            VisualElement subtitle,
            SubtitleLabels labels,
            string nameText,
            string lineText,
            SpeechMode mode,
            int stackAge,
            bool showName)
        {
            float maxChip = mode switch
            {
                SpeechMode.Whisper => 300f,
                SpeechMode.Shout => 480f,
                SpeechMode.Announcement => 520f,
                _ => 420f,
            };

            float padX = stackAge > 0
                ? 24f
                : mode switch
                {
                    SpeechMode.Whisper => 20f,
                    SpeechMode.Shout => 32f,
                    SpeechMode.Emote or SpeechMode.Action => 24f,
                    _ => 28f,
                };

            float maxContent = Mathf.Max(8f, maxChip - padX);
            float letterSpacing = mode switch
            {
                SpeechMode.Whisper => 1f,
                SpeechMode.Shout => 1f,
                _ => 0f,
            };

            // Clear prior constraints so Undefined measure is truly unconstrained.
            labels.Line.style.maxWidth = StyleKeyword.None;
            labels.Name.style.maxWidth = StyleKeyword.None;

            Vector2 naturalLine = labels.Line.MeasureTextSize(
                lineText, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);

            labels.Line.style.maxWidth = maxContent;
            labels.Name.style.maxWidth = maxContent;
            subtitle.style.maxWidth = maxChip;

            // Failed metrics → let Yoga size; never pin a tiny width.
            if (!IsPlausibleTextWidth(lineText, naturalLine.x))
            {
                labels.Line.style.whiteSpace = WhiteSpace.Normal;
                subtitle.style.width = StyleKeyword.Auto;
                return;
            }

            float contentWidth = naturalLine.x + LetterSpacingExtra(lineText, letterSpacing);
            if (showName)
            {
                Vector2 nameSize = labels.Name.MeasureTextSize(
                    nameText, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
                if (IsPlausibleTextWidth(nameText, nameSize.x))
                {
                    // Names always use letter-spacing: 1px in USS.
                    contentWidth = Mathf.Max(
                        contentWidth,
                        nameSize.x + LetterSpacingExtra(nameText, 1f));
                }
            }

            // Slack for measure vs render rounding — without this the last word wraps.
            const float MeasureSlack = 10f;

            if (contentWidth + MeasureSlack <= maxContent)
            {
                // Hard-stop premature wraps from Yoga / measure slack.
                labels.Line.style.whiteSpace = WhiteSpace.NoWrap;
                subtitle.style.width = StyleKeyword.Auto;
                return;
            }

            // Truly long: allow wrap at maxContent, chip fills max.
            labels.Line.style.whiteSpace = WhiteSpace.Normal;
            subtitle.style.width = maxChip;
        }

        private static bool IsPlausibleTextWidth(string text, float measuredWidth)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            float minExpected = Mathf.Max(12f, text.Length * 3.5f);
            return measuredWidth >= minExpected;
        }

        private static float LetterSpacingExtra(string text, float letterSpacingPx)
        {
            if (letterSpacingPx <= 0f || string.IsNullOrEmpty(text) || text.Length < 2)
            {
                return 0f;
            }

            return letterSpacingPx * (text.Length - 1);
        }

        private static void ApplyModeClass(VisualElement subtitle, SpeechMode mode)
        {
            for (int i = 0; i < ModeClasses.Length; i++)
            {
                subtitle.EnableInClassList(ModeClasses[i], false);
            }

            string modeClass = mode switch
            {
                SpeechMode.Whisper => "comms-subtitle--whisper",
                SpeechMode.Shout => "comms-subtitle--shout",
                SpeechMode.Emote => "comms-subtitle--emote",
                SpeechMode.Action => "comms-subtitle--action",
                SpeechMode.Radio => "comms-subtitle--radio",
                SpeechMode.Announcement => "comms-subtitle--announce",
                _ => "comms-subtitle--speak",
            };

            subtitle.EnableInClassList(modeClass, true);
        }

        private VisualElement GetOrCreateSubtitle(int index)
        {
            while (_subtitlePool.Count <= index)
            {
                VisualElement subtitle = new();
                subtitle.AddToClassList("comms-subtitle");
                subtitle.AddToClassList("comms-subtitle--speak");
                subtitle.pickingMode = PickingMode.Ignore;
                subtitle.style.position = Position.Absolute;
                subtitle.style.display = DisplayStyle.None;
                subtitle.style.flexGrow = 0;
                subtitle.style.flexShrink = 0;

                Label nameLabel = new();
                nameLabel.AddToClassList("font-titling");
                nameLabel.AddToClassList("comms-subtitle__name");
                nameLabel.pickingMode = PickingMode.Ignore;
                subtitle.Add(nameLabel);

                Label lineLabel = new();
                lineLabel.AddToClassList("font-body");
                lineLabel.AddToClassList("comms-subtitle__line");
                lineLabel.pickingMode = PickingMode.Ignore;
                subtitle.Add(lineLabel);

                subtitle.userData = new SubtitleLabels
                {
                    Name = nameLabel,
                    Line = lineLabel,
                };

                _overlayRoot.Add(subtitle);
                _subtitlePool.Add(subtitle);
            }

            return _subtitlePool[index];
        }
    }
}
