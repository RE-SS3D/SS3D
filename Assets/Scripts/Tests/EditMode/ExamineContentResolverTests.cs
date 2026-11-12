using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Localization;
using SS3D.Systems.Examine;
using SS3D.Tests;
using UnityEngine;
using UnityEngine.Localization;

namespace EditorTests
{
    public class ExamineContentResolverTests : EditModeTest
    {
        [TearDown]
        public void TearDownResolverTests()
        {
            LocalizedTextService.ResetForTests();
        }

        [Test]
        public void ResolveReturnsEmptyForNullExaminable()
        {
            ExamineContentResolver resolver = new();

            ExamineContent content = resolver.Resolve(null);

            Assert.AreEqual(ExamineContent.Empty.Name, content.Name);
            Assert.AreEqual(ExamineContent.Empty.Description, content.Description);
            Assert.IsEmpty(content.Sections);
        }

        [Test]
        public void ResolveReturnsEmptyWhenDataMissing()
        {
            ExamineContentResolver resolver = new();
            StubExaminable examinable = new(null);

            ExamineContent content = resolver.Resolve(examinable);

            Assert.AreEqual(string.Empty, content.Name);
            Assert.AreEqual(string.Empty, content.Description);
            Assert.IsFalse(content.HasDescription);
        }

        [Test]
        public void ResolveUsesLocalizedStringReferences()
        {
            ExamineContentResolver resolver = new();
            ExamineData data = ScriptableObject.CreateInstance<ExamineData>();
            // Use keys that are never in the Examine table. Real keys like
            // items.tools.engineering.wrench.name resolve to "Wrench" whenever
            // another EditMode test (or AssetAudit) has already loaded the table.
            const string missingNameKey = "test.examine_content_resolver.missing.name";
            const string missingDescKey = "test.examine_content_resolver.missing.desc";
            data.Name = new LocalizedString(
                ExamineCanonicalKeyGenerator.ExamineTableName,
                missingNameKey);
            data.Description = new LocalizedString(
                ExamineCanonicalKeyGenerator.ExamineTableName,
                missingDescKey);

            StubExaminable examinable = new(data);
            ExamineContent content = resolver.Resolve(examinable);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Assert.AreEqual($"[MISSING: {missingNameKey}]", content.Name);
            Assert.AreEqual($"[MISSING: {missingDescKey}]", content.Description);
#else
            Assert.AreEqual(missingNameKey, content.Name);
            Assert.AreEqual(missingDescKey, content.Description);
#endif
            Assert.IsFalse(content.Name.Contains("*[to be localized]*"));
            Assert.IsFalse(content.Description.Contains("*[to be localized]*"));
            Assert.IsTrue(content.HasDescription);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void ResolveCollectsAllProvidersOnSameGameObject()
        {
            ExamineContentResolver resolver = new();
            ExamineData data = ScriptableObject.CreateInstance<ExamineData>();
            data.Name = new LocalizedString(
                ExamineCanonicalKeyGenerator.ExamineTableName,
                "test.examine_multi_provider.name");
            data.Description = new LocalizedString(
                ExamineCanonicalKeyGenerator.ExamineTableName,
                "test.examine_multi_provider.desc");

            GameObject host = new("ExamineMultiProviderHost");
            try
            {
                StubExaminableWithProvider primary = host.AddComponent<StubExaminableWithProvider>();
                primary.Data = data;
                primary.SectionText = "Primary section";

                StubExtraProvider secondary = host.AddComponent<StubExtraProvider>();
                secondary.SectionText = "Secondary section";

                ExamineContent content = resolver.Resolve(primary);

                Assert.AreEqual(2, content.Sections.Count);
                Assert.AreEqual("Primary section", content.Sections[0].Text);
                Assert.AreEqual("Secondary section", content.Sections[1].Text);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void BareItemResolvesToItsNameWithSizeAndStackLines()
        {
            ExamineContentResolver resolver = new();
            GameObject host = new("BareItem");
            try
            {
                SS3D.Systems.Inventory.Items.Item item = host.AddComponent<SS3D.Systems.Inventory.Items.Item>();
                item.Init("Steel sheet", 1f, new List<SS3D.Systems.Trait>());
                UnityEditor.SerializedObject serialized = new(item);
                serialized.FindProperty("_maxStackSize").intValue = 50;
                serialized.FindProperty("_sizeClass").enumValueIndex = (int)SS3D.Systems.Inventory.Items.SizeClass.Small;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                ExamineContent content = resolver.Resolve(new ItemFallbackExaminable(item));

                Assert.AreEqual("Steel sheet", content.Name);
                Assert.IsFalse(content.HasDescription);
                Assert.AreEqual(2, content.Sections.Count);
                Assert.AreEqual(ExamineBand.Any, content.Sections[0].Band);
                StringAssert.Contains("small", content.Sections[0].Text);
                Assert.AreEqual(ExamineBand.Close, content.Sections[1].Band);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DestroyedFallbackItemResolvesEmpty()
        {
            Assert.AreEqual(string.Empty, new ExamineContentResolver().Resolve(new ItemFallbackExaminable(null)).Name);
        }

        private sealed class StubExaminable : IExaminable
        {
            private readonly ExamineData _data;

            public StubExaminable(ExamineData data)
            {
                _data = data;
            }

            public ExamineData GetData()
            {
                return _data;
            }
        }

        private sealed class StubExaminableWithProvider : MonoBehaviour, IExaminable, IExamineContentProvider
        {
            public ExamineData Data;
            public string SectionText;

            public ExamineData GetData()
            {
                return Data;
            }

            public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
            {
                sections.Add(new ExamineSection(SectionText));
            }
        }

        private sealed class StubExtraProvider : MonoBehaviour, IExamineContentProvider
        {
            public string SectionText;

            public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
            {
                sections.Add(new ExamineSection(SectionText));
            }
        }
    }
}
