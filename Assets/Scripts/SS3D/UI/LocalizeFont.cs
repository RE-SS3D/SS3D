using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

namespace SS3D.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeFont : MonoBehaviour
    {
        private LocalizedAsset<TMP_FontAsset> _fontAsset;
        [SerializeField] private string _fontKey;
        private TextMeshProUGUI _text;

        public void Start()
        {
            ChangeHandler(LocalizationSettings.SelectedLocale);
        }

        protected void OnEnable()
        {
            _text = GetComponent<TextMeshProUGUI>();
            LocalizationSettings.SelectedLocaleChanged += ChangeHandler;
        }

        protected void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= ChangeHandler;
        }

        protected virtual void ChangeHandler(Locale value)
        {
            _text.font = LocalizationSettings.AssetDatabase.GetLocalizedAsset<TMP_FontAsset>("Font", _fontKey);
        }
    }
}
