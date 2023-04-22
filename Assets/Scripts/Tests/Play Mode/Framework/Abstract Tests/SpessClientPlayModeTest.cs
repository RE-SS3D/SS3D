using System.Collections;
using NUnit.Framework;
using SS3D.Core.Settings;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Diagnostics;
using SS3D.Systems.Entities.Humanoid;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using SS3D.Core;
using SS3D.Systems.Interactions;

namespace SS3D.Tests
{
    /// <summary>
    /// Inherit from this for each test that need to test something on client only, with a standalone server.
    /// </summary>
    public abstract class SpessClientPlayModeTest : SpessPlayModeTest
    {
        protected const string HorizontalAxis = "Horizontal";
        protected const string VerticalAxis = "Vertical";
        protected const string CancelButton = "Cancel";
        protected const string ReadyButtonName = "Ready";
        protected const string ServerSettingsTabName = "Server Settings";
        protected const string StartRoundButtonName = "Start Round";

        protected Process cmdLineProcess;


        public override IEnumerator UnitySetUp()
        {
            LoadFileHelpers.OpenCompiledBuild();

            // Force wait for 10 seconds - this should be long enough for the server to load
            yield return new WaitForSeconds(10f);

            // Set to run as client
            SetApplicationSettings(NetworkType.Client);

            // Load the startup scene (which will subsequently load the lobby once connected)
            LoadStartupScene();

            // We need to do the base method after StartUp scene loaded.
            yield return base.UnitySetUp();

            yield return new WaitForSeconds(3f);
        }

        public override IEnumerator UnityTearDown()
        {
            KillAllBuiltExecutables();
            yield return new WaitForSeconds(3f);
            yield return base.UnityTearDown();
        }

        public override void Setup()
        {
            base.Setup();
        }


        public override void TearDown()
        {
            base.TearDown();
        }


        public void PressButton(string buttonName)
        {
            GameObject.Find(buttonName)?.GetComponent<LabelButton>().Press();
        }

        public void SetTabActive(string tabName)
        {
            GameObject.Find(tabName)?.GetComponent<Button>().onClick.Invoke();
        }
    }
}
