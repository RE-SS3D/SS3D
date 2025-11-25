using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.Serialization;

namespace SS3D.Systems.Examine
{
    public class ExamineUI : Actor
    {
        [FormerlySerializedAs("HoverName")]
        [SerializeField]
        private TMP_Text _hoverName;

        private StringTable _currentStringTable;

        protected override void OnEnabled()
        {
            base.OnEnabled();
            Subsystems.Get<ExamineSubSystem>().OnExaminableChanged += UpdateHoverText;
        }

        protected override void OnDisabled()
        {
            base.OnEnabled();
            Subsystems.Get<ExamineSubSystem>().OnExaminableChanged -= UpdateHoverText;
        }

        /// <summary>
        /// Updates the hover text with the appropriate localized string.
        /// </summary>
        /// <param name="examinable">The object that is being examined</param>
        private void UpdateHoverText([CanBeNull] AbstractExaminable examinable)
        {
            string hoverTextToDisplay = string.Empty;

            if (examinable && examinable.GetData())
            {
                ExamineData data = examinable.GetData();

                if (data.LocalizationTable != null && !data.LocalizationTable.IsEmpty)
                {
                    _currentStringTable = data.LocalizationTable?.GetTable();
                    if (!_currentStringTable || _currentStringTable[data.NameKey] is null || _currentStringTable[data.NameKey].LocalizedValue is null)
                    {
                        hoverTextToDisplay = data.NameKey + " *[to be localized]*";
                    }
                    else
                    {
                        hoverTextToDisplay = _currentStringTable[data.NameKey]?.LocalizedValue;
                    }
                }
            }

            _hoverName.text = hoverTextToDisplay;
        }
    }
}
