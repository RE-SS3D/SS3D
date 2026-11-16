using SS3D.Systems.Examine;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Adds live alarm readouts to the air alarm examine panel.
    /// </summary>
    [RequireComponent(typeof(AirAlarmController))]
    public sealed class AirAlarmExamineProvider : SimpleExaminable, IExamineContentProvider
    {
        private AirAlarmController _controller;

        private void Awake()
        {
            _controller = GetComponent<AirAlarmController>();
        }

        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            if (_controller == null)
                _controller = GetComponent<AirAlarmController>();

            _controller?.AppendExamineSections(sections);
        }
    }
}
