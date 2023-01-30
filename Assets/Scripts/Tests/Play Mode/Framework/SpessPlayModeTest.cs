using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Coimbra;
using NUnit.Framework;
using SS3D.Core.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SS3D.Tests
{

    public abstract class SpessPlayModeTest : SpessTest
    {
        protected const string ExecutableName = "SS3D";


        // Whether the lobby scene has been loaded
        protected bool lobbySceneLoaded = false;


        // When overriding, use:
        //     yield return base.UnitySetUp();
        [UnitySetUp]
        public virtual IEnumerator UnitySetUp()
        {
            base.SetUp();
            yield return null;
        }

        // When overriding, use:
        //     yield return base.UnityTearDown();
        [UnityTearDown]
        public virtual IEnumerator UnityTearDown()
        {
            base.TearDown();
            yield return null;
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
            }
        }

        protected void LoadStartupScene()
        {
            // Start up the game.
            SceneManager.sceneLoaded += ClientSceneLoaded;
            SceneManager.LoadScene("Startup", LoadSceneMode.Single);
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