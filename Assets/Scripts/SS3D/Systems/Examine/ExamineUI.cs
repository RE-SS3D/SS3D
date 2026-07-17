using System.Text;
using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Localization;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Inputs;
using SS3D.Systems.Inventory.Containers;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    public class ExamineUI : Actor
    {
        [SerializeField] private TMP_Text HoverName;
        [SerializeField] private ExamineDetailedView DetailedViewPrefab;
        [SerializeField] private ExamineImageDetailedView ImageDetailedViewPrefab;
        [SerializeField] private Vector2 DetailedTextOffset = new Vector2(16f, -16f);

        private readonly ExamineContentResolver _contentResolver = new();
        private IExaminable _currentExaminable;
        private IExaminable _cachedExaminable;
        private ExamineContent _cachedContent;
        private bool _hasCachedContent;
        private GameObject _localPlayer;
        private bool _wasDetailedExamineHeld;
        private bool _pinnedDetailedExamine;
        private InputSubSystem _inputSystem;
        private ExamineDetailedView _textDetailedView;
        private ExamineImageDetailedView _imageDetailedView;
        private RectTransform _activeDetailedPanel;

        protected override void OnStart()
        {
            base.OnStart();
            AddHandle(LocalPlayerObjectChanged.AddListener(HandleLocalPlayerObjectChanged));
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            LocalizedTextService.EnsureInitialized();
            LocalizedTextService.LocaleChanged += HandleLocaleChanged;
            _inputSystem = SubSystems.Get<InputSubSystem>();
            EnsureDetailedViews();
            SubSystems.Get<ExamineSubSystem>().OnExaminableChanged += UpdateHoverText;
            SubSystems.Get<ExamineSubSystem>().OnDetailedExamineRequested += ShowDetailedExamine;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            LocalizedTextService.LocaleChanged -= HandleLocaleChanged;
            SubSystems.Get<ExamineSubSystem>().OnExaminableChanged -= UpdateHoverText;
            SubSystems.Get<ExamineSubSystem>().OnDetailedExamineRequested -= ShowDetailedExamine;
            SetDetailedViewVisible(false);
            InvalidateContentCache();
        }

        private void HandleLocalPlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            _localPlayer = e.PlayerHasObject ? e.PlayerObject : null;
        }

        private void HandleLocaleChanged()
        {
            InvalidateContentCache();
            UpdateHoverText(_currentExaminable);
        }

        private void Update()
        {
            bool isDetailedExamineHeld = IsDetailedExamineHeld();
            if (isDetailedExamineHeld != _wasDetailedExamineHeld)
            {
                _wasDetailedExamineHeld = isDetailedExamineHeld;
                UpdateHoverText(_currentExaminable);
            }
            else if (isDetailedExamineHeld)
            {
                UpdateHoverText(_currentExaminable);

                if (_activeDetailedPanel != null)
                {
                    PositionDetailedPanel();
                }
            }
        }

        /// <summary>
        /// Shows the detailed examine panel until the hovered examinable changes.
        /// </summary>
        public void ShowDetailedExamine(IExaminable examinable)
        {
            _pinnedDetailedExamine = true;
            UpdateHoverText(examinable);
        }

        /// <summary>
        /// Updates the hover text with the appropriate localized string.
        /// </summary>
        /// <param name="examinable">The object that is being examined</param>
        private void UpdateHoverText(IExaminable examinable)
        {
            if (_pinnedDetailedExamine && examinable != _currentExaminable && examinable != null)
            {
                _pinnedDetailedExamine = false;
            }

            _currentExaminable = examinable;
            _wasDetailedExamineHeld = IsDetailedExamineHeld();

            if (examinable?.GetData() == null)
            {
                HoverName.text = string.Empty;
                SetDetailedViewVisible(false);
                InvalidateContentCache();
                _pinnedDetailedExamine = false;
                return;
            }

            ExamineContent content = GetCachedContent(examinable);

            if (_wasDetailedExamineHeld)
            {
                ExamineData data = examinable.GetData();
                if (data.Type == ExamineType.SIMPLE_IMAGE
                    && IsWithinDetailedImageRange(examinable, data)
                    && TryGetImageDetailedContent(examinable, content, out Sprite image, out string caption, out Vector2 imageSize))
                {
                    HoverName.text = string.Empty;
                    ShowImageDetailedView(image, caption, imageSize);
                    return;
                }

                if (content.HasDescription || content.Sections.Count > 0)
                {
                    HoverName.text = string.Empty;
                    ShowTextDetailedView(content.Name, BuildDetailedText(content));
                    return;
                }
            }

            SetDetailedViewVisible(false);
            HoverName.text = content.Name;
        }

        private ExamineContent GetCachedContent(IExaminable examinable)
        {
            if (_hasCachedContent && _cachedExaminable == examinable)
            {
                return _cachedContent;
            }

            _cachedContent = _contentResolver.Resolve(examinable);
            _cachedExaminable = examinable;
            _hasCachedContent = true;
            return _cachedContent;
        }

        private void InvalidateContentCache()
        {
            _hasCachedContent = false;
            _cachedExaminable = null;
            _cachedContent = ExamineContent.Empty;
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

        private static string BuildDetailedText(ExamineContent content)
        {
            if (content.Sections == null || content.Sections.Count == 0)
            {
                return content.Description;
            }

            StringBuilder builder = new(content.Description);
            foreach (ExamineSection section in content.Sections)
            {
                if (string.IsNullOrWhiteSpace(section.Text))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("\n\n");
                }

                builder.Append(section.Text);
            }

            return builder.ToString();
        }

        private bool TryGetImageDetailedContent(
            IExaminable examinable,
            ExamineContent content,
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
            caption = content.Description;
            return true;
        }

        private bool IsWithinDetailedImageRange(IExaminable examinable, ExamineData data)
        {
            if (!TryGetPlayerExamineOrigin(out Vector3 origin))
            {
                return false;
            }

            return ExamineRangeUtility.IsWithinRange(examinable, origin, data.DetailedImageRange);
        }

        private bool TryGetPlayerExamineOrigin(out Vector3 origin)
        {
            origin = default;

            if (_localPlayer == null)
            {
                return false;
            }

            Hands hands = _localPlayer.GetComponentInChildren<Hands>();
            Hand hand = hands?.SelectedHand;
            if (hand == null)
            {
                return false;
            }

            origin = hand.InteractionOrigin;
            return true;
        }

        private bool IsDetailedExamineHeld()
        {
            if (_pinnedDetailedExamine)
            {
                return true;
            }

            return _inputSystem != null && _inputSystem.DetailedExamine.IsPressed();
        }
    }
}
