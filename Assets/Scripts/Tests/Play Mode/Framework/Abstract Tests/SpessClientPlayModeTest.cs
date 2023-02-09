using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Core.Settings;
using SS3D.Systems.Rounds;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Coimbra;
using System.Diagnostics;
using FishNet.Managing;
using FishNet;
using SS3D.Systems.InputHandling;
using SS3D.Systems.Entities.Humanoid;
using System.Runtime.InteropServices;
using System;

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
        protected const string StartServerCommand = "Start SS3D Server.bat";

        protected Process cmdLineProcess;

        protected ScriptedInput input;
        protected HumanoidController controller;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            // Get relevant batch file path
            string batPath = Application.dataPath;
            batPath = batPath.Substring(0, batPath.Length - 6);     // Needed to remove the "Assets" folder.
            batPath += "Builds";                                    // Needed to add the "Builds" folder.

            // Fire up the server.
            cmdLineProcess = new Process();
            cmdLineProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            cmdLineProcess.StartInfo.FileName = StartServerCommand;
            cmdLineProcess.StartInfo.WorkingDirectory = batPath;
            cmdLineProcess.Start();
            cmdLineProcess.WaitForExit();

            UnityEngine.Debug.Log(SceneManager.GetActiveScene().name);

            // Force wait for 10 seconds - this should be long enough for the server to load
            System.Threading.Thread.Sleep(10000);

            // Set to run as client
            SetApplicationSettings(NetworkType.Client);

            // Load the startup scene (which will subsequently load the lobby once connected)
            LoadStartupScene();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            KillAllBuiltExecutables();
        }

        [UnitySetUp]
        public override IEnumerator UnitySetUp()
        {
            yield return base.UnitySetUp();

            // Create our fake input and assign it to the player
            input = new ScriptedInput();
            UserInput.SetInputService(input);

            // We need to wait until the lobby scene is loaded before anything can be done.
            while (!lobbySceneLoaded) yield return new WaitForSeconds(1f);
        }



        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            // Wait for a bit, to get some temporal separation.
            yield return new WaitForSeconds(1f);

            yield return base.UnityTearDown();
        }

        public IEnumerator GetController()
        {
            const string characterName = "Human_Temporary(Clone)";
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
    }
}
