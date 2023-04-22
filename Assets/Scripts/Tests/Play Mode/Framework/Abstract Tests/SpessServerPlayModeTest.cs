using System.Collections;
using NUnit.Framework;
using SS3D.Core.Settings;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Diagnostics;
using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Tests
{
    /// <summary>
    /// This should be run to test server only in the Unity editor, and for all tests that necessitates one or multiple clients only.
    /// </summary>
    public abstract class SpessServerPlayModeTest : SpessPlayModeTest
    {
        protected const string HorizontalAxis = "Horizontal";
        protected const string VerticalAxis = "Vertical";
        protected const string CancelButton = "Cancel";
        protected const string ReadyButtonName = "Ready";
        protected const string ServerSettingsTabName = "Server Settings";
        protected const string StartRoundButtonName = "Start Round";
        protected const string StartClientCommand = "Start SS3D Server.bat";

        protected Process[] clientProcess;


        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            // Set to run as server
            SetApplicationSettings(NetworkType.ServerOnly);

            // Load the startup scene (which will subsequently load the lobby once connected)
            LoadStartupScene();
        }

        public void PressButton(string buttonName)
        {
            GameObject.Find(buttonName)?.GetComponent<LabelButton>().Press();
        }

        public void SetTabActive(string tabName)
        {
            GameObject.Find(tabName)?.GetComponent<Button>().onClick.Invoke();
        }

        public void KillClientProcesses()
        {
            foreach (Process process in clientProcess)
            {
                process.CloseMainWindow();
                process.Close();
            }
        }


    }
}
