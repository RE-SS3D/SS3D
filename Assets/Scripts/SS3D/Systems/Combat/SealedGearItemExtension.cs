using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Localization;
using SS3D.Systems.Examine;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Environmental seal on a worn piece (Documents/design/armor.md §3). A suit body and a helmet
    /// seal together; the coverage that decides a breach comes from the piece's
    /// <see cref="ArmorItemExtension"/>.
    /// </summary>
    public class SealedGearItemExtension : NetworkBehaviour, IExamineContentProvider
    {
        [SerializeField, Range(0f, 1f)] private float _heatProtection = 0.7f;

        [SyncVar]
        private bool _breached;

        public bool IsBreached => _breached;

        /// <summary>Share of heat and fire exposure stopped while the seal holds.</summary>
        public float HeatProtection => _heatProtection;

        [Server]
        public void ServerBreach() => _breached = true;

        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            sections.Add(new ExamineSection(LocalizedTextService.GetFormattedString(
                ExamineCanonicalKeyGenerator.ExamineTableName,
                _breached ? ExamineSealKeys.Breached : ExamineSealKeys.Intact,
                null,
                _breached ? ExamineSealKeys.BreachedFallback : ExamineSealKeys.IntactFallback)));
        }
    }
}
