using NUnit.Framework;
using SS3D.Localization;
using SS3D.Systems.Examine;
using SS3D.Tests;

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
        public void ResolveUsesLegacyKeysWithoutDevLocalizationMarker()
        {
            ExamineContentResolver resolver = new();
            ExamineData data = UnityEngine.ScriptableObject.CreateInstance<ExamineData>();
            data.NameKey = "Wrench";
            data.DescriptionKey = "Used to get leverage when torquing nuts.";

            StubExaminable examinable = new(data);
            ExamineContent content = resolver.Resolve(examinable);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Assert.AreEqual("[MISSING: Wrench]", content.Name);
            Assert.AreEqual("[MISSING: Used to get leverage when torquing nuts.]", content.Description);
#else
            Assert.AreEqual("Wrench", content.Name);
            Assert.AreEqual("Used to get leverage when torquing nuts.", content.Description);
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
