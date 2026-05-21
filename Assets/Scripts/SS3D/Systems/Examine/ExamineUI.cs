using SS3D.Core;
using SS3D.Core.Behaviours;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Tables;

namespace SS3D.Systems.Examine
{
    public class ExamineUI : Actor
    {
        [SerializeField] private TMP_Text HoverName;

        private StringTable _currentStringTable;
        private IExaminable _currentExaminable;
        private bool _isShowingDetailedExamine;

        protected override void OnEnabled()
        {
            base.OnEnabled();
            SubSystems.Get<ExamineSubSystem>().OnExaminableChanged += UpdateHoverText;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            SubSystems.Get<ExamineSubSystem>().OnExaminableChanged -= UpdateHoverText;
        }

        private void Update()
        {
            if (_currentExaminable is null)
            {
                return;
            }

            bool shouldShowDetailedExamine = IsDetailedExaminePressed();
            if (shouldShowDetailedExamine == _isShowingDetailedExamine)
            {
                return;
            }

            _isShowingDetailedExamine = shouldShowDetailedExamine;
            UpdateHoverText(_currentExaminable);
        }

        /// <summary>
        /// Updates the hover text with the appropriate localized string.
        /// </summary>
        /// <param name="examinable">The object that is being examined</param>
        private void UpdateHoverText(IExaminable examinable)
        {
            string hoverTextToDisplay = string.Empty;
            _currentExaminable = examinable;
            _isShowingDetailedExamine = IsDetailedExaminePressed();

            if (examinable?.GetData())
            {
                ExamineData data = examinable.GetData();
                _currentStringTable = data.LocalizationTable?.GetTable();

                string name = GetLocalizedText(data.NameKey);
                string description = _isShowingDetailedExamine ? GetLocalizedText(data.DescriptionKey) : string.Empty;

                hoverTextToDisplay = string.IsNullOrWhiteSpace(description)
                    ? name
                    : $"{name}\n{description}";
            }

            HoverName.text = hoverTextToDisplay;
        }

        private static bool IsDetailedExaminePressed()
        {
            return Keyboard.current?.leftShiftKey.isPressed == true
                || Keyboard.current?.rightShiftKey.isPressed == true;
        }

        private string GetLocalizedText(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string localizedValue = _currentStringTable?[key]?.LocalizedValue;
            return string.IsNullOrWhiteSpace(localizedValue)
                ? $"{key} *[to be localized]*"
                : localizedValue;
        }
    }
}
