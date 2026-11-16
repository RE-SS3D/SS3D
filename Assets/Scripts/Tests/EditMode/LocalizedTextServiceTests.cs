using NUnit.Framework;
using SS3D.Localization;
using SS3D.Tests;
using UnityEngine.Localization;

namespace EditorTests
{
    public class LocalizedTextServiceTests : EditModeTest
    {
        [TearDown]
        public void TearDownServiceTests()
        {
            LocalizedTextService.ResetForTests();
        }

        [Test]
        public void GetStringReturnsEmptyForEmptyKey()
        {
            string result = LocalizedTextService.GetString((LocalizedStringTable)null, string.Empty);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void EnsureInitializedCanBeCalledMultipleTimes()
        {
            LocalizedTextService.EnsureInitialized();
            LocalizedTextService.EnsureInitialized();

            Assert.Pass();
        }

        [Test]
        public void ResetForTestsAllowsReinitialization()
        {
            LocalizedTextService.EnsureInitialized();
            LocalizedTextService.ResetForTests();
            LocalizedTextService.EnsureInitialized();

            Assert.Pass();
        }
    }
}
