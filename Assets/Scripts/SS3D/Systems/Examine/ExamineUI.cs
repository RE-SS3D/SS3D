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
        [SerializeField] private ExamineImageDetailedView ImageDetailedViewPrefab;
        [SerializeField] private KeyCode DetailedExamineKey = KeyCode.LeftShift;
        [SerializeField] private Vector2 DetailedTextOffset = new Vector2(16f, -16f);

        private StringTable _currentStringTable;
        private IExaminable _currentExaminable;
        private bool _wasDetailedExamineHeld;
        private ExamineDetailedView _textDetailedView;
        private ExamineImageDetailedView _imageDetailedView;
        private RectTransform _activeDetailedPanel;

        protected override void OnEnabled()
        {
            base.OnEnabled();
            EnsureDetailedViews();
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
            else if (isDetailedExamineHeld && _activeDetailedPanel != null)
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

            if (_wasDetailedExamineHeld)
            {
                ExamineData data = examinable.GetData();
                if (data.Type == ExamineType.SIMPLE_IMAGE
                    && TryGetImageDetailedContent(examinable, out Sprite image, out string caption, out Vector2 imageSize))
                {
                    HoverName.text = string.Empty;
                    ShowImageDetailedView(image, caption, imageSize);
                    return;
                }

                if (TryGetDetailedTexts(examinable, out string name, out string description))
                {
                    HoverName.text = string.Empty;
                    ShowTextDetailedView(name, description);
                    return;
                }
            }

            SetDetailedViewVisible(false);
            HoverName.text = GetLocalizedName(examinable);
        }

        private void EnsureDetailedViews()
        {
            if (HoverName == null)
            {
                return;
            }

            Transform parent = HoverName.rectTransform.parent;

            if (_textDetailedView == null && DetailedViewPrefab != null)
            {
                _textDetailedView = Instantiate(DetailedViewPrefab, parent);
                _textDetailedView.name = "Examinable Detailed View";
                _textDetailedView.transform.SetAsLastSibling();
                _textDetailedView.SetVisible(false);
            }

            if (_imageDetailedView == null && ImageDetailedViewPrefab != null)
            {
                _imageDetailedView = Instantiate(ImageDetailedViewPrefab, parent);
                _imageDetailedView.name = "Examinable Image Detailed View";
                _imageDetailedView.transform.SetAsLastSibling();
                _imageDetailedView.SetVisible(false);
            }
        }

        private void ShowTextDetailedView(string name, string description)
        {
            EnsureDetailedViews();
            if (_textDetailedView == null)
            {
                return;
            }

            SetDetailedViewVisible(false);
            _textDetailedView.SetContent(name, description);
            _textDetailedView.SetVisible(true);
            _activeDetailedPanel = _textDetailedView.Panel;
            PositionDetailedPanel();
        }

        private void ShowImageDetailedView(Sprite image, string caption, Vector2 imageSize)
        {
            EnsureDetailedViews();
            if (_imageDetailedView == null)
            {
                return;
            }

            SetDetailedViewVisible(false);
            _imageDetailedView.SetContent(image, caption, imageSize);
            _imageDetailedView.SetVisible(true);
            _activeDetailedPanel = _imageDetailedView.Panel;
            PositionDetailedPanel();
        }

        private void SetDetailedViewVisible(bool visible)
        {
            if (!visible)
            {
                _activeDetailedPanel = null;
            }

            if (_textDetailedView != null)
            {
                _textDetailedView.SetVisible(false);
            }

            if (_imageDetailedView != null)
            {
                _imageDetailedView.SetVisible(false);
            }
        }

        private void PositionDetailedPanel()
        {
            if (_activeDetailedPanel == null)
            {
                return;
            }

            Vector2 position = (Vector2)Input.mousePosition + DetailedTextOffset;
            float width = _activeDetailedPanel.rect.width;
            float height = _activeDetailedPanel.rect.height;

            position.x = Mathf.Clamp(position.x, 0f, Screen.width - width);
            position.y = Mathf.Clamp(position.y, height, Screen.height);

            _activeDetailedPanel.position = position;
        }

        private bool TryGetImageDetailedContent(
            IExaminable examinable,
            out Sprite image,
            out string caption,
            out Vector2 imageSize)
        {
            image = null;
            caption = string.Empty;
            imageSize = Vector2.zero;

            ExamineData data = examinable?.GetData();
            if (data == null || data.Type != ExamineType.SIMPLE_IMAGE || examinable is not IImageExaminable imageExaminable)
            {
                return false;
            }

            image = imageExaminable.GetDetailedImage();
            if (image == null)
            {
                return false;
            }

            imageSize = data.DetailedImageSize;

            if (data.LocalizationTable != null)
            {
                _currentStringTable = data.LocalizationTable.GetTable();
                if (_currentStringTable != null)
                {
                    caption = GetLocalizedValue(data.DescriptionKey);
                }
            }

            return true;
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
