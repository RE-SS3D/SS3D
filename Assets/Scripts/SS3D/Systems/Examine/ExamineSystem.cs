using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core.Behaviours;
using UnityEngine.UI;
using UnityEngine.Experimental.Rendering;
using System;
using SS3D.Systems.Selection;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// The Examine System allows additional detail of items to be displayed when
    /// the cursor hovers over them. The particular information displayed is item
    /// and requirement dependant, and may take different formats.
    /// </summary>
    public class ExamineSystem : NetworkSystem
    {
        private SelectionSystem _selectionSystem;
        private float MIN_UPDATES_PER_SECOND = 3f;
        private float _updateFrequency;
        private float updateTimer;

        protected override void OnStart()
        {
            base.OnStart();
            _selectionSystem = GetComponent<SelectionSystem>();


        }

        private void OnExaminableChanged(GameObject go)
        {
            if (go != null)
            {
                
            }
        }
    }
}
