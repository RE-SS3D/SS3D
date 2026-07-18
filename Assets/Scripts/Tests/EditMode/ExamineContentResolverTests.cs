using NUnit.Framework;
using SS3D.Localization;
using SS3D.Systems.Examine;
using SS3D.Tests;
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
            ExamineData data = UnityEngine.ScriptableObject.CreateInstance<ExamineData>();
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

            UnityEngine.Object.DestroyImmediate(data);
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
    }
}
