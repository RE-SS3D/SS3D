using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using SS3D.Core;
using SS3D.Core.Behaviours;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace SS3D.Systems.ScreenEffects
{
    /// <summary>
    /// Drives the diegetic screen-space feedback described in the main HUD design doc (§5) and the
    /// "Screen-Space Effect" mockups: ambient temperature, fire/freezing, low oxygen, dying, blood loss,
    /// concussion and unconsciousness, plus a momentary melee hit flash.
    ///
    /// Not wired into the health/atmospherics systems yet - <see cref="SetEffect"/> and
    /// <see cref="TriggerHitFlash"/> are the integration points a future pass will call from there.
    /// For now they're only called by the screen-effects debug menu/commands.
    /// </summary>
    public sealed class ScreenEffectsSubSystem : SubSystem
    {
        // Bootstraps itself instead of living in Boot.unity like the other persistent subsystems, since hand-editing
        // scene YAML outside the Unity Editor isn't safe. Move this into Boot.unity later if preferred - the
        // behaviour is identical, this is just how it gets into the scene without an Editor session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SubSystems.TryGet(out ScreenEffectsSubSystem _))
            {
                return;
            }

            GameObject host = new(nameof(ScreenEffectsSubSystem));
            DontDestroyOnLoad(host);
            host.AddComponent<ScreenEffectsSubSystem>();
        }

        private const float HitFlashAttack = 0.03f;
        private const float HitFlashDecay = 0.35f;

        private readonly Dictionary<ScreenEffectType, float> _targetIntensity = new();
        private readonly Dictionary<ScreenEffectType, float> _currentIntensity = new();

        private Vignette _vignette;
        private ChromaticAberration _chromaticAberration;
        private ColorAdjustments _colorAdjustments;
        private DepthOfField _depthOfField;
        private Image _blackout;

        private float _hitFlashTimer = -1f;

        protected override void OnAwake()
        {
            base.OnAwake();

            foreach (ScreenEffectType type in Enum.GetValues(typeof(ScreenEffectType)))
            {
                _targetIntensity[type] = 0f;
                _currentIntensity[type] = 0f;
            }

            BuildVolume();
            BuildBlackout();

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        /// <summary>
        /// Sets the target strength (0..1) of a sustained screen-space effect. 0 turns it off.
        /// </summary>
        public void SetEffect(ScreenEffectType type, float intensity)
        {
            _targetIntensity[type] = Mathf.Clamp01(intensity);
        }

        public float GetEffectIntensity(ScreenEffectType type)
        {
            return _targetIntensity.GetValueOrDefault(type, 0f);
        }

        /// <summary>
        /// Fires a single sharp red flash that decays on its own - an event, not a state. Re-triggerable.
        /// </summary>
        public void TriggerHitFlash()
        {
            _hitFlashTimer = 0f;
        }

        private void BuildVolume()
        {
            GameObject volumeHost = new("ScreenEffectsVolume");
            volumeHost.transform.SetParent(Transform, false);

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _vignette = profile.Add<Vignette>(true);
            _chromaticAberration = profile.Add<ChromaticAberration>(true);
            _colorAdjustments = profile.Add<ColorAdjustments>(true);
            _depthOfField = profile.Add<DepthOfField>(true);

            Volume volume = volumeHost.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
        }

        private void BuildBlackout()
        {
            GameObject canvasHost = new("ScreenEffectsBlackout");
            canvasHost.transform.SetParent(Transform, false);

            Canvas canvas = canvasHost.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasHost.AddComponent<CanvasScaler>();

            GameObject imageHost = new("Blackout");
            imageHost.transform.SetParent(canvasHost.transform, false);

            _blackout = imageHost.AddComponent<Image>();
            _blackout.color = new Color(0f, 0f, 0f, 0f);
            _blackout.raycastTarget = false;

            RectTransform rect = _blackout.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            float deltaTime = Time.deltaTime;
            float time = Time.time;

            foreach (ScreenEffectType type in _targetIntensity.Keys)
            {
                float smoothSpeed = type == ScreenEffectType.Unconscious ? 0.3f : 3f;
                _currentIntensity[type] = Mathf.MoveTowards(_currentIntensity[type], _targetIntensity[type], smoothSpeed * deltaTime);
            }

            float vignetteIntensity = 0f;
            Color vignetteColorSum = Color.black;
            float vignetteWeightSum = 0f;
            float chromaticAberration = 0f;
            float saturation = 0f;
            float contrast = 0f;
            float blur = 0f;
            float blackoutAlpha = 0f;

            void AddVignette(float amount, Color color)
            {
                if (amount <= 0f)
                {
                    return;
                }

                vignetteIntensity += amount;
                vignetteColorSum += color * amount;
                vignetteWeightSum += amount;
            }

            float Breathe(float period, float phaseOffset = 0f)
            {
                return (Mathf.Sin((time / period + phaseOffset) * Mathf.PI * 2f) + 1f) * 0.5f;
            }

            float Flicker(float speed, float seed)
            {
                return Mathf.PerlinNoise(time * speed, seed);
            }

            float Heartbeat(float period)
            {
                float cycle = Mathf.Repeat(time, period) / period;
                float Beat(float center, float width) => Mathf.Exp(-Mathf.Pow((cycle - center) / width, 2f) * 8f);
                return Mathf.Clamp01(Beat(0.08f, 0.05f) + Beat(0.28f, 0.06f));
            }

            float hot = _currentIntensity[ScreenEffectType.HotRoom];
            if (hot > 0f)
            {
                AddVignette(hot * (0.25f + 0.15f * Breathe(3.8f)), new Color(0.85f, 0.6f, 0.25f));
            }

            float fire = _currentIntensity[ScreenEffectType.OnFire];
            if (fire > 0f)
            {
                AddVignette(fire * (0.45f + 0.35f * Flicker(6f, 1.1f)), new Color(0.8f, 0.25f, 0.1f));
            }

            float cold = _currentIntensity[ScreenEffectType.ColdRoom];
            if (cold > 0f)
            {
                AddVignette(cold * (0.25f + 0.15f * Breathe(4f)), new Color(0.35f, 0.6f, 0.85f));
                saturation -= cold * 15f;
            }

            float freezing = _currentIntensity[ScreenEffectType.Freezing];
            if (freezing > 0f)
            {
                AddVignette(freezing * (0.45f + 0.35f * Flicker(5f, 2.7f)), new Color(0.3f, 0.55f, 0.85f));
                saturation -= freezing * 25f;
            }

            float lowOxygen = _currentIntensity[ScreenEffectType.LowOxygen];
            if (lowOxygen > 0f)
            {
                float breathe = Breathe(4.4f);
                AddVignette(lowOxygen * (0.4f + 0.2f * breathe), new Color(0.3f, 0.45f, 0.65f));
                saturation -= lowOxygen * (60f + 20f * breathe);
            }

            float dying = _currentIntensity[ScreenEffectType.DyingCritical];
            if (dying > 0f)
            {
                float beat = Heartbeat(1.6f);
                AddVignette(dying * (0.55f + 0.35f * beat), new Color(0.45f, 0.08f, 0.08f));
                blur += dying * (0.3f + 0.7f * beat);
            }

            float bloodLoss = _currentIntensity[ScreenEffectType.BloodLossTunnelVision];
            if (bloodLoss > 0f)
            {
                float breathe = Breathe(5.2f);
                AddVignette(bloodLoss * (0.55f + 0.15f * breathe), new Color(0.35f, 0.06f, 0.06f));
                saturation -= bloodLoss * 45f;
            }

            float concussion = _currentIntensity[ScreenEffectType.Concussion];
            if (concussion > 0f)
            {
                blur += concussion * (0.35f + 0.4f * Flicker(1.1f, 4.3f));
                chromaticAberration += concussion * (0.3f + 0.3f * Flicker(0.9f, 8.6f));
            }

            float unconscious = _currentIntensity[ScreenEffectType.Unconscious];
            if (unconscious > 0f)
            {
                saturation -= unconscious * 100f;
                contrast -= unconscious * 40f;
                blackoutAlpha = Mathf.Clamp01(unconscious * 1.3f - 0.3f);
            }

            float hitFlash = ComputeHitFlash(deltaTime);
            if (hitFlash > 0f)
            {
                AddVignette(hitFlash, new Color(0.75f, 0.1f, 0.1f));
            }

            Color finalVignetteColor = vignetteWeightSum > 0f ? vignetteColorSum / vignetteWeightSum : Color.black;
            float finalVignetteIntensity = Mathf.Clamp01(vignetteIntensity);

            _vignette.active = finalVignetteIntensity > 0.001f;
            _vignette.color.value = finalVignetteColor;
            _vignette.intensity.value = finalVignetteIntensity;
            _vignette.smoothness.value = 0.45f;

            _chromaticAberration.active = chromaticAberration > 0.001f;
            _chromaticAberration.intensity.value = Mathf.Clamp01(chromaticAberration);

            _colorAdjustments.active = saturation < -0.001f || contrast < -0.001f;
            _colorAdjustments.saturation.value = Mathf.Clamp(saturation, -100f, 100f);
            _colorAdjustments.contrast.value = Mathf.Clamp(contrast, -100f, 100f);

            bool blurActive = blur > 0.001f;
            _depthOfField.active = blurActive;
            _depthOfField.mode.value = blurActive ? DepthOfFieldMode.Gaussian : DepthOfFieldMode.Off;
            _depthOfField.gaussianStart.value = Mathf.Lerp(50f, 0.01f, Mathf.Clamp01(blur));
            _depthOfField.gaussianEnd.value = Mathf.Lerp(60f, 0.05f, Mathf.Clamp01(blur));
            _depthOfField.gaussianMaxRadius.value = Mathf.Lerp(0.5f, 1.5f, Mathf.Clamp01(blur));

            Color blackoutColor = _blackout.color;
            blackoutColor.a = blackoutAlpha;
            _blackout.color = blackoutColor;
        }

        private float ComputeHitFlash(float deltaTime)
        {
            if (_hitFlashTimer < 0f)
            {
                return 0f;
            }

            _hitFlashTimer += deltaTime;
            const float total = HitFlashAttack + HitFlashDecay;

            if (_hitFlashTimer >= total)
            {
                _hitFlashTimer = -1f;
                return 0f;
            }

            if (_hitFlashTimer < HitFlashAttack)
            {
                return _hitFlashTimer / HitFlashAttack;
            }

            return 1f - (_hitFlashTimer - HitFlashAttack) / HitFlashDecay;
        }
    }
}
