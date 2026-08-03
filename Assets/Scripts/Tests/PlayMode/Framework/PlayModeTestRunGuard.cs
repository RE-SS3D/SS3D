using System;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(SS3D.Tests.PlayModeTestRunGuard))]

namespace SS3D.Tests
{
    public sealed class PlayModeTestRunGuard : ITestRunCallback
    {
        public void RunStarted(ITest testsToRun)
        {
            ConfigureEditorForPlayModeTests();
            KillBuiltExecutables();
        }

        public void RunFinished(ITestResult testResults)
        {
            KillBuiltExecutables();
        }

        public void TestStarted(ITest test)
        {
        }

        public void TestFinished(ITestResult result)
        {
        }

        internal static void ConfigureEditorForPlayModeTests()
        {
#if UNITY_EDITOR
            DisableFastScriptReloadHotReload();
#endif
        }

        internal static void KillBuiltExecutables()
        {
            foreach (Process process in Process.GetProcessesByName(PlayModeTest.ExecutableName))
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogWarning($"Failed to stop {PlayModeTest.ExecutableName} process {process.Id}: {exception.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

#if UNITY_EDITOR
        private static void DisableFastScriptReloadHotReload()
        {
            Type preferenceType = FindType("FastScriptReload.Editor.FastScriptReloadPreference");
            if (preferenceType == null)
            {
                return;
            }

            SetFastScriptReloadPreference(preferenceType, "EnableAutoReloadForChangedFiles", false);
            SetFastScriptReloadPreference(preferenceType, "EnableOnDemandReload", false);
            SetFastScriptReloadPreference(preferenceType, "EnableExperimentalEditorHotReloadSupport", false);
        }

        private static void SetFastScriptReloadPreference(Type preferenceType, string fieldName, bool value)
        {
            FieldInfo field = preferenceType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            object preference = field?.GetValue(null);
            if (preference == null)
            {
                return;
            }

            MethodInfo setter = preference.GetType().GetMethod("SetEditorPersistedValue", BindingFlags.Public | BindingFlags.Instance);
            setter?.Invoke(preference, new object[] { value });
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
#endif
    }

}
