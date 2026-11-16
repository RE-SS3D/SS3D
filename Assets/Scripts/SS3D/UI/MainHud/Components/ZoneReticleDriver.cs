using UnityEngine;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Composes a single <see cref="ZoneReticleFrame"/> per tick from aim + recovery inputs
    /// and the connect-hit flash clock. Owns color priority so the view never reconciles
    /// competing CSS modifiers.
    /// </summary>
    public sealed class ZoneReticleDriver
    {
        private const float CrossFlashSeconds = 0.5f;
        private const float CrossFlashShakeFraction = 0.4f;
        private const float CrossFlashShakePx = 2.5f;

        private bool _visible;
        private Vector2 _cursorScreen;
        private string _zoneLabel = string.Empty;
        private bool _inRange;
        private float _ready01 = 1f;
        private bool _recovering;
        private float _crossFlashStartedAt = -1f;
        private float _bloom01;

        public void SetVisible(bool visible)
        {
            if (_visible == visible)
            {
                return;
            }

            _visible = visible;
            if (!visible)
            {
                // Drop leftover flash so a later show does not resume mid-animation.
                _crossFlashStartedAt = -1f;
            }
        }

        public void SetAimInput(Vector2 cursor, string zoneLabel, bool inRange)
        {
            _cursorScreen = cursor;
            _zoneLabel = zoneLabel ?? string.Empty;
            _inRange = inRange;
        }

        public void SetRecoveryInput(float ready01, bool recovering)
        {
            _ready01 = Mathf.Clamp01(ready01);
            _recovering = recovering;
        }

        public void SetBloomInput(float bloom01)
        {
            _bloom01 = Mathf.Clamp01(bloom01);
        }

        public void NotifyConnectHit()
        {
            _crossFlashStartedAt = Time.unscaledTime;
        }

        public void Tick(out ZoneReticleFrame frame)
        {
            float crossFlashT = -1f;
            Vector2 shake = Vector2.zero;

            if (_crossFlashStartedAt >= 0f)
            {
                float elapsed = Time.unscaledTime - _crossFlashStartedAt;
                if (elapsed >= CrossFlashSeconds)
                {
                    _crossFlashStartedAt = -1f;
                }
                else
                {
                    crossFlashT = elapsed / CrossFlashSeconds;
                    if (crossFlashT < CrossFlashShakeFraction)
                    {
                        shake = ComputeShake(crossFlashT / CrossFlashShakeFraction);
                    }
                }
            }

            ZoneReticleColorMode color = ZoneReticleColorMode.Idle;
            if (_recovering)
            {
                color = ZoneReticleColorMode.Recharging;
            }
            else if (_inRange)
            {
                color = ZoneReticleColorMode.Valid;
            }

            float bracketReady01 = _recovering ? _ready01 : 1f;

            frame = new ZoneReticleFrame(
                _visible,
                _cursorScreen,
                _zoneLabel,
                color,
                bracketReady01,
                crossFlashT,
                shake,
                _bloom01);
        }

        private static Vector2 ComputeShake(float shakeT)
        {
            // Deterministic jitter matching design ss3d-hit-shake keyframes.
            float x = shakeT < 0.2f ? -1f
                : shakeT < 0.4f ? 1f
                : shakeT < 0.6f ? -0.67f
                : shakeT < 0.8f ? 0.67f
                : 0f;
            float y = shakeT < 0.2f ? 0.67f
                : shakeT < 0.4f ? -0.67f
                : shakeT < 0.6f ? 0.67f
                : shakeT < 0.8f ? -0.33f
                : 0f;
            return new Vector2(x, y) * CrossFlashShakePx;
        }
    }
}
