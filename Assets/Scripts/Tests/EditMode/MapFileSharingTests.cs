using NUnit.Framework;
using SS3D.Data.Persistence;
using System.IO;

namespace SS3D.Tests.EditMode
{
    public sealed class MapFileSharingTests
    {
        private string _root;
        private string _templates;
        private string _exports;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "ss3d-mapsharing-" + System.Guid.NewGuid().ToString("N"));
            _templates = Path.Combine(_root, "StationTemplates");
            _exports = Path.Combine(_root, "Exports");
            Directory.CreateDirectory(_templates);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void ExportThenDropZip_ImportsUnderUniqueName()
        {
            File.WriteAllText(Path.Combine(_templates, "My Station.json"), "{\"a\":1}");

            Assert.IsTrue(MapFileSharing.TryExport(_templates, _exports, "My Station", out string zip, out _));
            Assert.IsTrue(File.Exists(zip));

            File.Copy(zip, Path.Combine(_templates, "My Station.zip"));
            var imported = MapFileSharing.ImportDroppedArchives(_templates);

            CollectionAssert.AreEqual(new[] { "My Station (2)" }, imported);
            Assert.AreEqual("{\"a\":1}", File.ReadAllText(Path.Combine(_templates, "My Station (2).json")));
            Assert.AreEqual("{\"a\":1}", File.ReadAllText(Path.Combine(_templates, "My Station.json")));
            Assert.IsFalse(File.Exists(Path.Combine(_templates, "My Station.zip")));
        }

        [Test]
        public void ExportUnsavedMap_Fails()
        {
            Assert.IsFalse(MapFileSharing.TryExport(_templates, _exports, "Missing", out _, out string error));
            StringAssert.Contains("not been saved", error);
        }

        [Test]
        public void ReExport_ReplacesOlderZip()
        {
            string json = Path.Combine(_templates, "m.json");
            File.WriteAllText(json, "1");
            Assert.IsTrue(MapFileSharing.TryExport(_templates, _exports, "m", out _, out _));
            File.WriteAllText(json, "22");
            Assert.IsTrue(MapFileSharing.TryExport(_templates, _exports, "m", out string zip, out _));

            File.Copy(zip, Path.Combine(_templates, "m.zip"));
            File.Delete(json);
            MapFileSharing.ImportDroppedArchives(_templates);
            Assert.AreEqual("22", File.ReadAllText(json));
        }
    }
}
