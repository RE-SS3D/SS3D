using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Combat;
using SS3D.Systems.Electricity;
using SS3D.Systems.Examine;
using SS3D.Systems.Furniture.Disposal;
using SS3D.Systems.IdAccess;
using UnityEngine;

namespace EditorTests
{
    /// <summary>Phase 3 content lines (examine.md §7): bands and wording of the new providers.</summary>
    public class ExamineContentLineTests
    {
        [Test]
        public void GunLinesCountRoundsOrChargesAndFlagEmpty()
        {
            Assert.AreEqual("Rounds: 12.", RangedWeaponRoundsReadout.Line(energy: false, 12));
            Assert.AreEqual("Charge: 3 shots.", RangedWeaponRoundsReadout.Line(energy: true, 3));
            Assert.AreEqual("Magazine empty.", ExamineKeywordFormatter.StripTags(RangedWeaponRoundsReadout.Line(false, 0)));
            StringAssert.Contains("<k=warn>empty</k>", RangedWeaponRoundsReadout.Line(true, 0));
        }

        [Test]
        public void PowerConsumerSaysPoweredOrUnpoweredAtAnyRange()
        {
            GameObject host = new("Consumer");
            try
            {
                BasicPowerConsumer consumer = host.AddComponent<BasicPowerConsumer>();
                List<ExamineSection> sections = new();

                consumer.PowerStatus = PowerStatus.Inactive;
                consumer.AppendSections(null, sections);
                consumer.PowerStatus = PowerStatus.Powered;
                consumer.AppendSections(null, sections);

                Assert.AreEqual(2, sections.Count);
                Assert.AreEqual("<k=warn>Unpowered</k>.", sections[0].Text);
                Assert.AreEqual("<k=good>Powered</k>.", sections[1].Text);
                Assert.AreEqual(ExamineBand.Any, sections[0].Band);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DisposalTagReadsUpCloseOnlyWhenTagged()
        {
            GameObject host = new("Package");
            try
            {
                DisposalTag tag = host.AddComponent<DisposalTag>();
                List<ExamineSection> sections = new();
                tag.AppendSections(null, sections);
                Assert.IsEmpty(sections, "an untagged item says nothing");

                tag.SetDestination(Department.Medical);
                tag.AppendSections(null, sections);
                Assert.AreEqual(1, sections.Count);
                Assert.AreEqual("Disposal tag: Medical.", sections[0].Text);
                Assert.AreEqual(ExamineBand.Close, sections[0].Band);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
