using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Shell.Animation
{
    /// <summary>Tuning for <see cref="PanelAnimator"/>. Each surface keeps its own visual tuning.</summary>
    public struct PanelAnimatorConfig
    {
        public float Duration;
        public float OpenScaleFrom;
        public float OpenTranslateYFrom;
        public Ease Ease;

        public static PanelAnimatorConfig Default => new()
        {
            Duration = 0.22f,
            OpenScaleFrom = 0.92f,
            OpenTranslateYFrom = 20f,
            Ease = Ease.OutCirc,
        };
    }

    /// <summary>
    /// Shared opacity/scale/translateY open/close tween for UI Toolkit panels, with an optional
    /// scrim-alpha join for diegetic/modal backdrops. Owns the "close must finish its tween before
    /// invoking the completion callback" invariant in one place instead of per-surface — see the
    /// Pitfalls section of Documents/architecture/systems/machine-interface.md.
    /// </summary>
    public sealed class PanelAnimator
    {
        private readonly PanelAnimatorConfig _config;

        private Sequence _sequence;
        private bool _isClosing;
        private Action _pendingCloseComplete;
        private float _scale;
        private float _translateY;

        public PanelAnimator(PanelAnimatorConfig config = default)
        {
            _config = config.Duration > 0f ? config : PanelAnimatorConfig.Default;
        }

        public bool IsAnimating => _sequence != null;

        public void PlayOpen(VisualElement target, Action<float> onScrimAlphaChanged = null, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return;
            }

            KillSequence();
            _isClosing = false;
            _pendingCloseComplete = null;

            _scale = _config.OpenScaleFrom;
            _translateY = _config.OpenTranslateYFrom;
            target.style.opacity = 0f;
            target.style.scale = new Scale(new Vector3(_scale, _scale, 1f));
            target.style.translate = new Translate(0f, _translateY);
            onScrimAlphaChanged?.Invoke(0f);

            _sequence = DOTween.Sequence();
            _sequence.Append(DOTween.To(
                    () => target.style.opacity.value,
                    value => target.style.opacity = value,
                    1f,
                    _config.Duration)
                .SetEase(_config.Ease));
            _sequence.Join(DOTween.To(
                    () => _scale,
                    value =>
                    {
                        _scale = value;
                        target.style.scale = new Scale(new Vector3(value, value, 1f));
                    },
                    1f,
                    _config.Duration)
                .SetEase(_config.Ease));
            _sequence.Join(DOTween.To(
                    () => _translateY,
                    value =>
                    {
                        _translateY = value;
                        target.style.translate = new Translate(0f, value);
                    },
                    0f,
                    _config.Duration)
                .SetEase(_config.Ease));

            if (onScrimAlphaChanged != null)
            {
                float scrimAlpha = 0f;
                _sequence.Join(DOTween.To(
                        () => scrimAlpha,
                        value =>
                        {
                            scrimAlpha = value;
                            onScrimAlphaChanged(value);
                        },
                        1f,
                        _config.Duration)
                    .SetEase(_config.Ease));
            }

            if (onComplete != null)
            {
                _sequence.OnComplete(() => onComplete());
            }
        }

        public void PlayClose(VisualElement target, Action<float> onScrimAlphaChanged = null, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (_isClosing)
            {
                // Already animating a close: replace the pending callback, don't restart the tween —
                // starting a new close must not drop the callback already promised by the first Close().
                _pendingCloseComplete = onComplete;
                return;
            }

            KillSequence();
            _isClosing = true;
            _pendingCloseComplete = onComplete;

            _sequence = DOTween.Sequence();
            _sequence.Append(DOTween.To(
                    () => target.style.opacity.value,
                    value => target.style.opacity = value,
                    0f,
                    _config.Duration)
                .SetEase(_config.Ease));
            _sequence.Join(DOTween.To(
                    () => _scale,
                    value =>
                    {
                        _scale = value;
                        target.style.scale = new Scale(new Vector3(value, value, 1f));
                    },
                    _config.OpenScaleFrom,
                    _config.Duration)
                .SetEase(_config.Ease));
            _sequence.Join(DOTween.To(
                    () => _translateY,
                    value =>
                    {
                        _translateY = value;
                        target.style.translate = new Translate(0f, value);
                    },
                    _config.OpenTranslateYFrom,
                    _config.Duration)
                .SetEase(_config.Ease));

            if (onScrimAlphaChanged != null)
            {
                float scrimAlpha = 1f;
                _sequence.Join(DOTween.To(
                        () => scrimAlpha,
                        value =>
                        {
                            scrimAlpha = value;
                            onScrimAlphaChanged(value);
                        },
                        0f,
                        _config.Duration)
                    .SetEase(_config.Ease));
            }

            _sequence.OnComplete(CompleteClose);
        }

        /// <summary>Kills any running tween and drops a pending close callback without invoking it.</summary>
        public void CancelImmediate()
        {
            KillSequence();
            _pendingCloseComplete = null;
            _isClosing = false;
        }

        /// <summary>Synchronously sets the end-of-close pose, no tween — for teardown/destroy paths.</summary>
        public void CloseImmediate(VisualElement target)
        {
            KillSequence();
            _isClosing = false;
            _pendingCloseComplete = null;

            if (target == null)
            {
                return;
            }

            target.style.opacity = 0f;
            target.style.scale = new Scale(new Vector3(_config.OpenScaleFrom, _config.OpenScaleFrom, 1f));
            target.style.translate = new Translate(0f, _config.OpenTranslateYFrom);
        }

        private void CompleteClose()
        {
            _isClosing = false;
            Action callback = _pendingCloseComplete;
            _pendingCloseComplete = null;
            callback?.Invoke();
        }

        private void KillSequence()
        {
            _sequence?.Kill();
            _sequence = null;
        }
    }
}
