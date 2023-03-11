using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Coimbra;
using NUnit.Framework;
using SS3D.Core.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    [TestFixture]
    public abstract class SpessPlayModeTest : InputTestFixture
    {
        protected const string ExecutableName = "SS3D";

        // Whether the lobby scene has been loaded
        protected bool lobbySceneLoaded = false;
        protected bool emptySceneLoaded = false;

        // Used to simulate input
        protected Keyboard keyboard;
        protected Mouse mouse;

        public override void Setup()
        {
            UnityEngine.Debug.Log("Calling InputTestFixture.Setup");
            base.Setup();
            keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<Keyboard>();
            mouse = UnityEngine.InputSystem.InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            base.TearDown();
        }

        [UnitySetUp]
        public virtual IEnumerator UnitySetUp()
        {
            // We need to wait until the lobby scene is loaded before anything can be done.
            while (!lobbySceneLoaded) yield return new WaitForSeconds(1f);
        }

        [UnityTearDown]
        public virtual IEnumerator UnityTearDown()
        {
            // Wait for a bit, to get some temporal separation.
            yield return new WaitForSeconds(1f);
        }

        protected void SetApplicationSettings(NetworkType type)
        {
            // Create new settings so that tests are run in the correct context.
            ApplicationSettings originalSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();
            ApplicationSettings newSettings = ScriptableObject.Instantiate(originalSettings);
            newSettings.NetworkType = type;
            newSettings.Ckey = "john";

            // Apply the new settings
            ScriptableSettings.SetOrOverwrite<ApplicationSettings>(newSettings);
        }

        protected void ClientSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.Equals("Lobby"))
            {
                lobbySceneLoaded = true;
                SceneManager.sceneLoaded -= ClientSceneLoaded;
            }

            if (scene.name.Equals("Empty"))
            {
                emptySceneLoaded = true;
                SceneManager.sceneLoaded -= ClientSceneLoaded;
            }
        }

        protected IEnumerator WaitForLobbyLoaded(float timeout = 20f)
        {
            float startTime = Time.time;
            while (!lobbySceneLoaded)
            {
                yield return new WaitForSeconds(1f);

                if (Time.time - startTime > timeout)
                {
                    throw new Exception($"Lobby not loaded within timeout of {timeout} seconds.");
                }
            }
        }

        protected IEnumerator WaitForEmptySceneLoaded(float timeout = 10f)
        {
            float startTime = Time.time;
            while (!emptySceneLoaded)
            {
                yield return new WaitForSeconds(1f);

                if (Time.time - startTime > timeout)
                {
                    throw new Exception($"Empty scene not loaded within timeout of {timeout} seconds.");
                }
            }
        }

        protected void LoadStartupScene()
        {
            // Start up the game.
            lobbySceneLoaded = false;
            SceneManager.sceneLoaded += ClientSceneLoaded;
            SceneManager.LoadScene("Startup", LoadSceneMode.Single);
        }

        protected void LoadEmptyScene()
        {
            // Close down the game.
            emptySceneLoaded = false;
            SceneManager.sceneLoaded += ClientSceneLoaded;
            SceneManager.LoadScene("Empty", LoadSceneMode.Single);
        }

        protected void KillAllBuiltExecutables()
        {
            foreach (var process in Process.GetProcessesByName(ExecutableName))
            {
                process.Kill();
            }
        }


        /// <summary>
        /// A simple means of running a UnityTest multiple times to see if it consistently works.
        /// To use this, simply paste "[ValueSource("Iterations")] int iteration" as the argument
        /// to the UnityTest you want to repeat.
        /// </summary>
        /// <returns>An array of the size specified by the Repetition constant.</returns>
        protected static int[] Iterations()
        {
            const int Repetitions = 10;

            int[] result = new int[Repetitions];
            for (int i = 0; i < Repetitions; i++)
            {
                result[i] = i;
            }
            return result;
        }
    }
}