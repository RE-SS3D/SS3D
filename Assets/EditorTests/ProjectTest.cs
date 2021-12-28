using System.Reflection;
using NUnit.Framework;

namespace SS3D.EditorTests
{
    public class ProjectTest
    {
        [Test]
        public void ProjectShouldHaveNoWarnings()
        {
            //https://stackoverflow.com/questions/40577412/clear-editor-console-logs-from-script
            //https://github.com/GlitchEnzo/UnityExposer/blob/master/Assets/Unity%20Exposer/Editor/LogEntries.cs
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var getCounts = type.GetMethod("GetCountsByType");
            //{Errors, Warnings, Messages}
            var arguments = new object[] {0, 0, 0};
            getCounts?.Invoke(new object(), arguments);

            Assert.Zero((int)arguments[1]);
        }
    }
}