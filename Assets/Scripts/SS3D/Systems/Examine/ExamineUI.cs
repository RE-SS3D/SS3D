using SS3D.Core;
using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SS3D.Systems.Examine
{
    public class ExamineUI : Actor
    {
        [SerializeField] private TMP_Text HoverName;
        [SerializeField] private TableReference _tableReference;

        protected override void OnEnabled()
        {
            base.OnEnabled();
            Subsystems.Get<ExamineSystem>().OnExaminableChanged += UpdateHoverText;
        }

        protected override void OnDisabled()
        {
            base.OnEnabled();
            Subsystems.Get<ExamineSystem>().OnExaminableChanged -= UpdateHoverText;
        }

        private void UpdateHoverText(IExaminable examinable)
        {
            if (examinable?.GetData() == null)
            {
                HoverName.text = "";
            }
            else
            {
                LocalizedString displayText = new LocalizedString(_tableReference, examinable.GetData().NameKey);
                //HoverName.text = examinable.GetData().NameKey;
                HoverName.text = displayText.GetLocalizedString();
            }
        }
    }
}