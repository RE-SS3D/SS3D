using SS3D.Core;
using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    public class ExamineUI : Actor
    {
        [SerializeField] private TMP_Text HoverName;

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
            Debug.Log($"UpdateHoverText called!!");
            if (examinable == null)
            {
                HoverName.text = "";
            }
            else
            {
                HoverName.text = examinable.GetData().NameKey;
            }
        }
    }
}