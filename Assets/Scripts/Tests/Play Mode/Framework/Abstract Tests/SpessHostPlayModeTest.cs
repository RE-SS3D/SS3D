using System.Collections;
using NUnit.Framework;
using SS3D.Core.Settings;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Tests
{
    public abstract class SpessHostPlayModeTest : SpessPlayModeTest
    {
        

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {

            // Set to run as host
            SetApplicationSettings(NetworkType.Host);

            // Load the startup scene (which will subsequently load the lobby once connected)
            LoadStartupScene();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {

        }

        protected IEnumerator StartAndEnterRound()
        {
            yield return TestHelpers.StartAndEnterRound();
            yield return GetHumanoidController();
        }

        protected IEnumerator FinishAndExitRound()
        {
            yield return TestHelpers.FinishAndExitRound();

        }

        [UnitySetUp]
        public override IEnumerator UnitySetUp()
        {
            yield return base.UnitySetUp();

            // TODO: Create our fake input and assign it to the player

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
    }
}
