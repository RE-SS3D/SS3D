using SS3D.Core;
using SS3D.Core.Behaviours;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace SS3D.Systems.Examine
{
    public class ExamineUI : Actor
    {
        [SerializeField] private TMP_Text HoverName;
        [SerializeField] private ExamineDetailedView DetailedViewPrefab;
        [SerializeField] private KeyCode DetailedExamineKey = KeyCode.LeftShift;
        [SerializeField] private Vector2 DetailedTextOffset = new Vector2(16f, -16f);

        private StringTable _currentStringTable;
        private IExaminable _currentExaminable;
        private bool _wasDetailedExamineHeld;
        private ExamineDetailedView _detailedView;

        protected override void OnEnabled()
        {
            base.OnEnabled();
            EnsureDetailedView();
            SubSystems.Get<ExamineSubSystem>().OnExaminableChanged += UpdateHoverText;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            SubSystems.Get<ExamineSubSystem>().OnExaminableChanged -= UpdateHoverText;
            SetDetailedViewVisible(false);
        }

        private void Update()
        {
            bool isDetailedExamineHeld = IsDetailedExamineHeld();
            if (isDetailedExamineHeld != _wasDetailedExamineHeld)
            {
                _wasDetailedExamineHeld = isDetailedExamineHeld;
                UpdateHoverText(_currentExaminable);
            }
            else if (isDetailedExamineHeld && _detailedView != null && _detailedView.gameObject.activeSelf)
            {
                PositionDetailedPanel();
            }
        }

        /// <summary>
        /// Updates the hover text with the appropriate localized string.
        /// </summary>
        /// <param name="examinable">The object that is being examined</param>
        private void UpdateHoverText(IExaminable examinable)
        {
            _currentExaminable = examinable;
            _wasDetailedExamineHeld = IsDetailedExamineHeld();

            if (examinable?.GetData() == null)
            {
                HoverName.text = string.Empty;
                SetDetailedViewVisible(false);
                return;
            }

            if (_wasDetailedExamineHeld && TryGetDetailedTexts(examinable, out string name, out string description))
            {
                HoverName.text = string.Empty;
                ShowDetailedView(name, description);
                return;
            }

            SetDetailedViewVisible(false);
            HoverName.text = GetLocalizedName(examinable);
        }

        private void EnsureDetailedView()
        {
            if (_detailedView != null || DetailedViewPrefab == null || HoverName == null)
            {
                return;
            }

            _detailedView = Instantiate(DetailedViewPrefab, HoverName.rectTransform.parent);
            _detailedView.name = "Examinable Detailed View";
            _detailedView.transform.SetAsLastSibling();
            _detailedView.SetVisible(false);
        }

        private void ShowDetailedView(string name, string description)
        {
            EnsureDetailedView();
            if (_detailedView == null)
            {
                return;
            }

            _detailedView.SetContent(name, description);
            SetDetailedViewVisible(true);
            PositionDetailedPanel();
        }

        private void SetDetailedViewVisible(bool visible)
        {
            if (_detailedView != null)
            {
                _detailedView.SetVisible(visible);
            }
        }

        private void PositionDetailedPanel()
        {
            RectTransform panel = _detailedView.Panel;
            Vector2 position = (Vector2)Input.mousePosition + DetailedTextOffset;
            float width = panel.rect.width;
            float height = panel.rect.height;

            position.x = Mathf.Clamp(position.x, 0f, Screen.width - width);
            position.y = Mathf.Clamp(position.y, height, Screen.height);

            panel.position = position;
        }

        private bool TryGetDetailedTexts(IExaminable examinable, out string name, out string description)
        {
            name = string.Empty;
            description = string.Empty;

            ExamineData data = examinable?.GetData();
            if (data == null || data.LocalizationTable == null)
            {
                return false;
            }

            _currentStringTable = data.LocalizationTable.GetTable();
            if (_currentStringTable == null)
            {
                return false;
            }

            name = GetLocalizedValue(data.NameKey);
            description = GetLocalizedValue(data.DescriptionKey);

            return !string.IsNullOrEmpty(description);
        }

        private string GetLocalizedName(IExaminable examinable)
        {
            ExamineData data = examinable?.GetData();
            if (data == null || data.LocalizationTable == null)
            {
                return string.Empty;
            }

            _currentStringTable = data.LocalizationTable.GetTable();
            if (_currentStringTable == null)
            {
                return string.Empty;
            }

            return GetLocalizedValue(data.NameKey);
        }

        private string GetLocalizedValue(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (_currentStringTable[key]?.LocalizedValue is null)
            {
                return key + " *[to be localized]*";
            }

            return _currentStringTable[key].LocalizedValue;
        }

        private bool IsDetailedExamineHeld()
        {
            return Input.GetKey(DetailedExamineKey) || Input.GetKey(KeyCode.RightShift);
        }
    }
}
