using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>Physical seven-segment LEDs on the charge, no world canvas or gameplay HUD.</summary>
    public sealed class ChargeCountdownDisplay : MonoBehaviour
    {
        [SerializeField] private Renderer[] _segments;
        [SerializeField] private Material _lit;
        [SerializeField] private Material _dim;
        private int _last = -1;
        private static readonly int[] Masks = { 0x3f, 0x06, 0x5b, 0x4f, 0x66, 0x6d, 0x7d, 0x07, 0x7f, 0x6f };
        public void Show(float seconds, bool armed, double time)
        {
            int value = Mathf.Clamp(Mathf.CeilToInt(seconds), 0, 99);
            bool blink = !armed || seconds > 3f || time % 0.2 < 0.13;
            int state = value + (armed ? 100 : 0) + (blink ? 200 : 0);
            if (_last == state || _segments == null || _segments.Length != 14) return;
            _last = state;
            for (int digit = 0; digit < 2; digit++)
            {
                int mask = Masks[digit == 0 ? value / 10 : value % 10];
                for (int segment = 0; segment < 7; segment++)
                    _segments[digit * 7 + segment].sharedMaterial = armed && blink && (mask & (1 << segment)) != 0 ? _lit : _dim;
            }
        }
    }
}
