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
    public abstract class SpessClientPlayModeTest : SpessPlayModeTest
    {
        protected const string HorizontalAxis = "Horizontal";
        protected const string VerticalAxis = "Vertical";
        protected const string CancelButton = "Cancel";
        protected const string ReadyButtonName = "Ready";
        protected const string ServerSettingsTabName = "Server Settings";
        protected const string StartRoundButtonName = "Start Round";

        protected Process cmdLineProcess;
        protected HumanoidController HumanoidController;
        protected InteractionController InteractionController;

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

        public override void Setup()
        {
            base.Setup();
        }


        public override void TearDown()
        {
            base.TearDown();
            KillAllBuiltExecutables();
        }



        //TODO: Add timeout
        public IEnumerator GetHumanoidController()
        {
            HumanoidController = null;
            while (HumanoidController == null)
            {
                yield return null;
                HumanoidController = GameObject.FindWithTag("Player")?.GetComponent<HumanoidController>();
            }
        }

        //TODO: Add timeout
        public IEnumerator GetInteractionController()
        {
            InteractionController = null;
            while (InteractionController == null)
            {
                yield return null;
                InteractionController = TestHelpers.GetLocalInteractionController();
            }
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
