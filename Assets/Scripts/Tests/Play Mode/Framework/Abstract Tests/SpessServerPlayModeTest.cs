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
    public abstract class SpessServerPlayModeTest : SpessPlayModeTest
    {
        protected const string HorizontalAxis = "Horizontal";
        protected const string VerticalAxis = "Vertical";
        protected const string CancelButton = "Cancel";
        protected const string ReadyButtonName = "Ready";
        protected const string ServerSettingsTabName = "Server Settings";
        protected const string StartRoundButtonName = "Start Round";
        protected const string StartClientCommand = "Start SS3D Server.bat";

        protected HumanoidController controller;
        protected Process[] clientProcess;


        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            // Set to run as server
            SetApplicationSettings(NetworkType.ServerOnly);

            // Load the startup scene (which will subsequently load the lobby once connected)
            LoadStartupScene();
        }

        public IEnumerator GetController()
        {
            const string characterName = "HumanTemporary(Clone)";
            controller = null;

            while (controller == null)
            {
                yield return null;
                controller = GameObject.Find(characterName)?.GetComponent<HumanoidController>();
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
