using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Localization;
using SS3D.Systems.Examine;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// A finite gas supply for internals and oxygen treatment (Documents/design/armor.md §3).
    /// The server owns the moles; clients see only how full the tank is.
    /// </summary>
    public class GasTankItemExtension : NetworkBehaviour, IExamineContentProvider
    {
        [SerializeField] private GasTankMix _contents;

        [SyncVar]
        private float _fill = 1f;

        private float _capacity;

        public float Fill => _fill;

        public bool IsEmpty => _fill <= 0f;

        /// <summary>Current mix. Server only: clients keep the prefab's starting values.</summary>
        public GasTankMix Contents => _contents;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _capacity = _contents.TotalMoles;
        }

        [Server]
        public void ServerDraw(float moles)
        {
            _contents = _contents.Without(moles);
            _fill = _capacity > 0f ? _contents.TotalMoles / _capacity : 0f;
        }

        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            sections.Add(new ExamineSection(LocalizedTextService.GetFormattedString(
                ExamineCanonicalKeyGenerator.ExamineTableName,
                ExamineTankKeys.Fill,
                new object[] { Mathf.CeilToInt(_fill * 100f) },
                ExamineTankKeys.FillFallback)));
        }
    }
}
