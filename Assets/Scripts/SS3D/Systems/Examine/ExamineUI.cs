using SS3D.Core;
using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace SS3D.Systems.Examine
{
    public class ExamineUI : Actor
    {
        [SerializeField] private TMP_Text HoverName;

        private StringTable _currentStringTable;

        protected override void OnEnabled()
        {
            base.OnEnabled();
            SubSystems.Get<ExamineSubSystem>().OnExaminableChanged += UpdateHoverText;
        }

        protected override void OnDisabled()
        {
            base.OnEnabled();
            SubSystems.Get<ExamineSubSystem>().OnExaminableChanged -= UpdateHoverText;
        }

        /// <summary>
        /// Updates the hover text with the appropriate localized string.
        /// </summary>
        /// <param name="examinable">The object that is being examined</param>
        private void UpdateHoverText(IExaminable examinable)
        {
            string hoverTextToDisplay = string.Empty;

            if (examinable?.GetData())
            {
                ExamineData data = examinable.GetData();

                if (data.LocalizationTable != null)
                {
                    _currentStringTable = data.LocalizationTable?.GetTable();
                    if (_currentStringTable[data.NameKey]?.LocalizedValue is null)
                    {
                        hoverTextToDisplay = data.NameKey + " *[to be localized]*";
                    }
                    else
                    {
                        hoverTextToDisplay = _currentStringTable[data.NameKey]?.LocalizedValue;
                    }
                }
            }

            HoverName.text = hoverTextToDisplay;
        }
    }
}