using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SS3D.UI
{
    /// <summary>
    /// Attach this to any UI element to automatically apply a color from the central palette.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("SS3D/UI/UI Color Applier")]
    public class UIColorApplier : MonoBehaviour
    {
        [SerializeField] private UIColorSettings _settings;
        [SerializeField] private UIColorSettings.UIColorType _colorType;

        private void OnValidate()
        {
            ApplyColor();
        }

        private void Start()
        {
            ApplyColor();
        }

        [ContextMenu("Apply Color Now")]
        public void ApplyColor()
        {
            if (_settings == null) return;

            Color targetColor = _settings.GetColor(_colorType);

            // Try to apply to Image
            if (TryGetComponent<Image>(out var image))
            {
                image.color = targetColor;
            }
            
            // Try to apply to TMP Text
            if (TryGetComponent<TMP_Text>(out var text))
            {
                text.color = targetColor;
            }
        }
    }
}
