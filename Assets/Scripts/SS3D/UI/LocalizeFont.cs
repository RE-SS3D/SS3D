using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SS3D.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeFont : MonoBehaviour
    {
        private TMP_FontAsset _fontAsset;
        [SerializeField] private string _fontKey;
        private TextMeshProUGUI _text;
        private Locale _currentLocale;

        public void Start()
        {
            GetTextFieldIfRequired();
            _fontAsset = _text.font;
            _currentLocale = LocalizationSettings.SelectedLocale;
            UpdateFont();
        }

        public void UpdateFont()
        {
            ChangeHandler(LocalizationSettings.SelectedLocale);
        }

        protected void OnEnable()
        {
            GetTextFieldIfRequired();
            LocalizationSettings.SelectedLocaleChanged += ChangeHandler;
        }

        private IEnumerator LoadAssetTable()
        {
            _currentLocale = LocalizationSettings.SelectedLocale;

            AsyncOperationHandle<TMP_FontAsset> assetLoading = LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<TMP_FontAsset>("Font", _fontKey);
            while (!assetLoading.IsDone)
            {
                yield return null;
            }

            _fontAsset = assetLoading.Result;
            GetTextFieldIfRequired();
            _text.font = _fontAsset;
        }

        protected void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= ChangeHandler;
        }

        protected virtual void ChangeHandler(Locale value = null)
        {
            StartCoroutine(LoadAssetTable());
        }

        private void GetTextFieldIfRequired()
        {
            if (_text == null)
            {
                _text = GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
