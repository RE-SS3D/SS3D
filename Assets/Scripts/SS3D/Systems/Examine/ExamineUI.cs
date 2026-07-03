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
        [SerializeField] private KeyCode DetailedExamineKey = KeyCode.LeftShift;

        private StringTable _currentStringTable;
        private IExaminable _currentExaminable;
        private bool _wasDetailedExamineHeld;

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
            bool isDetailedExamineHeld = IsDetailedExamineHeld();
            if (isDetailedExamineHeld != _wasDetailedExamineHeld)
            {
                _wasDetailedExamineHeld = isDetailedExamineHeld;
                UpdateHoverText(_currentExaminable);
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
            HoverName.text = GetHoverText(examinable, _wasDetailedExamineHeld);
        }

        private string GetHoverText(IExaminable examinable, bool showDetailedText)
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

            string name = GetLocalizedValue(data.NameKey);

            if (!showDetailedText)
            {
                return name;
            }

            string description = GetLocalizedValue(data.DescriptionKey);
            if (string.IsNullOrEmpty(description))
            {
                return name;
            }

            if (string.IsNullOrEmpty(name))
            {
                return description;
            }

            return $"{name}\n{description}";
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
