using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core;
using SS3D.Core.Behaviours;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SS3D.Systems.ScreenEffects
{
    /// <summary>
    /// Dev-only panel to trigger and tune every <see cref="ScreenEffectType"/> plus the hit-flash event,
    /// without needing the health/atmospherics systems that will eventually drive these for real.
    /// Toggle with F2. Built entirely at runtime - no prefab/scene dependency.
    /// </summary>
    public sealed class ScreenEffectsDebugMenuView : View
    {
        private static bool s_bootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_bootstrapped)
            {
                return;
            }

            s_bootstrapped = true;

            GameObject host = new(nameof(ScreenEffectsDebugMenuView));
            DontDestroyOnLoad(host);
            host.AddComponent<ScreenEffectsDebugMenuView>();
        }

        private GameObject _panel;

        protected override void OnAwake()
        {
            base.OnAwake();

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (Keyboard.current == null || !Keyboard.current[Key.F2].wasPressedThisFrame)
            {
                return;
            }

            // Built lazily on first use rather than in OnAwake, which - since this view bootstraps itself very
            // early via RuntimeInitializeOnLoadMethod - can run before the real scene's own EventSystem exists,
            // causing our GraphicRaycaster to trigger uGUI's auto-created one and duplicate it.
            if (_panel == null)
            {
                BuildUi();
                _panel.SetActive(false);
            }

            _panel.SetActive(!_panel.activeSelf);
        }

        private void BuildUi()
        {
            GameObject canvasHost = new("ScreenEffectsDebugCanvas");
            canvasHost.transform.SetParent(Transform, false);

            Canvas canvas = canvasHost.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasHost.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasHost.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasHost.transform, false);

            RectTransform panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-16f, -16f);
            panelRect.sizeDelta = new Vector2(380f, 0f);

            Image background = _panel.AddComponent<Image>();
            background.color = new Color(0.05f, 0.05f, 0.06f, 0.85f);

            VerticalLayoutGroup layout = _panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = _panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddLabel(_panel.transform, "Screen Effects (F2)", 18);

            foreach (ScreenEffectType type in Enum.GetValues(typeof(ScreenEffectType)))
            {
                AddEffectRow(type);
            }

            AddHitFlashButton();
        }

        private void AddEffectRow(ScreenEffectType type)
        {
            GameObject row = new(type.ToString());
            row.transform.SetParent(_panel.transform, false);

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;

            LayoutElement rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 24f;

            Text label = AddLabel(row.transform, type.ToString(), 13);
            LayoutElement labelElement = label.gameObject.AddComponent<LayoutElement>();
            labelElement.preferredWidth = 150f;

            GameObject sliderHost = new("Slider");
            sliderHost.transform.SetParent(row.transform, false);
            LayoutElement sliderElement = sliderHost.AddComponent<LayoutElement>();
            sliderElement.flexibleWidth = 1f;

            Slider slider = BuildSlider(sliderHost);

            Text valueLabel = AddLabel(row.transform, "0.00", 13);
            LayoutElement valueElement = valueLabel.gameObject.AddComponent<LayoutElement>();
            valueElement.preferredWidth = 40f;

            slider.onValueChanged.AddListener(value =>
            {
                valueLabel.text = value.ToString("0.00");
                SubSystems.Get<ScreenEffectsSubSystem>()?.SetEffect(type, value);
            });
        }

        private void AddHitFlashButton()
        {
            GameObject buttonHost = new("HitFlashButton");
            buttonHost.transform.SetParent(_panel.transform, false);

            LayoutElement buttonElement = buttonHost.AddComponent<LayoutElement>();
            buttonElement.preferredHeight = 28f;

            Image background = buttonHost.AddComponent<Image>();
            background.color = new Color(0.6f, 0.15f, 0.15f, 1f);

            Button button = buttonHost.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => SubSystems.Get<ScreenEffectsSubSystem>()?.TriggerHitFlash());

            AddLabel(buttonHost.transform, "Hit Flash", 14);
        }

        private static Slider BuildSlider(GameObject host)
        {
            Slider slider = host.AddComponent<Slider>();

            RectTransform hostRect = host.GetComponent<RectTransform>();
            hostRect.sizeDelta = new Vector2(0f, 20f);

            GameObject background = new("Background");
            background.transform.SetParent(host.transform, false);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.22f, 1f);
            StretchToParent(background.GetComponent<RectTransform>());

            GameObject fillArea = new("Fill Area");
            fillArea.transform.SetParent(host.transform, false);
            StretchToParent(fillArea.AddComponent<RectTransform>());

            GameObject fill = new("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.5f, 0.7f, 0.9f, 1f);
            StretchToParent(fill.GetComponent<RectTransform>());

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;

            return slider;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text AddLabel(Transform parent, string text, int fontSize)
        {
            GameObject host = new("Label");
            host.transform.SetParent(parent, false);

            Text label = host.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = Color.white;
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            return label;
        }
    }
}
