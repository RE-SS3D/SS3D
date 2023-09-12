using System.Collections;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Core.Settings;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Diagnostics;
using SS3D.Systems.Entities.Humanoid;
using System;
using SS3D.Systems.PlayerControl;
using System.Linq;

namespace SS3D.Tests
{
    public class Issue1002_LateJoinFails_HostPerspective : SpessPlayModeTest
    {

        protected Process clientProcess;

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
           yield return LoadAndSetInLobby(NetworkType.Host);
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            // Wait for a bit, to get some temporal separation.
            yield return new WaitForSeconds(1f);

            // Shut down the client
            clientProcess.CloseMainWindow();
            clientProcess.Close();

            TestHelpers.FinishAndExitRound();

            // Wait for a bit more
            yield return new WaitForSeconds(1f);
        }

        [UnityTest]
        public IEnumerator ClientCanEmbarkAfterRoundStartWhenNoOneElseHasEmbarked()
        {
            string clientCkey = "client";

            // Start the client running, and wait until they have entered the lobby.
            clientProcess = LoadFileHelpers.OpenCompiledBuild(NetworkType.Client, clientCkey);
            yield return WaitForClientSoulToAppearInLobby(clientCkey);


            // Set the round to begin, and wait a few seconds for the countdown etc to finish.
            yield return TestHelpers.StartRound();
            yield return new WaitForSeconds(5f);

            // Set the client as ready.
            ServerHelpers.SpawnLatePlayer(clientCkey);
            yield return new WaitForSeconds(3f);

            // We test that this is working by attempting to get the player mind.
            // This contains an assertion to confirm the mind is in the scene.
            yield return CheckMindIsInScene(clientCkey);
            yield return new WaitForSeconds(1f);
        }

        [UnityTest]
        public IEnumerator ClientCanEmbarkAfterRoundStartWhenHostHasAlreadyEmbarked()
        {
            string clientCkey = "client";

            // Start the client running, and wait until they have entered the lobby.
            clientProcess = LoadFileHelpers.OpenCompiledBuild(NetworkType.Client, clientCkey);
            yield return WaitForClientSoulToAppearInLobby(clientCkey);


            // Set the round to begin, and wait a few seconds for the countdown etc to finish.
            yield return TestHelpers.StartAndEnterRound();
            yield return new WaitForSeconds(5f);

            // Set the client as ready.
            ServerHelpers.SpawnLatePlayer(clientCkey);
            yield return new WaitForSeconds(3f);

            // We test that this is working by attempting to get the player mind.
            // This contains an assertion to confirm the mind is in the scene.
            yield return CheckMindIsInScene(clientCkey);
            yield return new WaitForSeconds(1f);
        }

        [UnityTest]
        public IEnumerator ClientCanJoinAfterRoundStartWhenHostHasAlreadyEmbarked()
        {
            string clientCkey = "client";

            // Set the round to begin, and wait a few seconds for the countdown etc to finish.
            yield return TestHelpers.StartAndEnterRound();
            yield return new WaitForSeconds(5f);

            // Start the client running, and wait until they have entered the lobby.
            // This contains assertion with timeout to confirm the soul has connected.
            clientProcess = LoadFileHelpers.OpenCompiledBuild(NetworkType.Client, clientCkey);
            yield return WaitForClientSoulToAppearInLobby(clientCkey);
            yield return new WaitForSeconds(1f);
        }

        [UnityTest]
        public IEnumerator ClientCanJoinAfterRoundStartWhenNoOneHasEmbarked()
        {
            string clientCkey = "client";

            // Set the round to begin, and wait a few seconds for the countdown etc to finish.
            yield return TestHelpers.StartRound();
            yield return new WaitForSeconds(5f);

            // Start the client running, and wait until they have entered the lobby.
            // This contains assertion with timeout to confirm the soul has connected.
            clientProcess = LoadFileHelpers.OpenCompiledBuild(NetworkType.Client, clientCkey);
            yield return WaitForClientSoulToAppearInLobby(clientCkey);
            yield return new WaitForSeconds(1f);

        }

        protected IEnumerator WaitForClientSoulToAppearInLobby(string ckey, float timeout = 15f)
        {
            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
            float startTime = Time.time;

            while (playerSystem.OnlinePlayers.ToList().Find(soul => soul.Ckey == ckey) == null)
            {
                yield return new WaitForSeconds(1f);
                Assert.IsTrue(Time.time < startTime + timeout, $"Client '{ckey}' not loaded after timeout of {timeout} seconds.");
            }
        }

        public IEnumerator CheckMindIsInScene(string ckey, float timeout = 10f)
        {
            string mind_prefix = "Mind - ";
            GameObject mindGO = null;

            float startTime = Time.time;

            while (mindGO == null)
            {
                yield return null;
                mindGO = GameObject.Find($"{mind_prefix}{ckey}");
                if (Time.time - startTime > timeout)
                {
                    throw new Exception($"{mind_prefix}{ckey} not found within timeout of {timeout} seconds.");
                }
            }
        }

        protected override bool UseMockUpInputs()
        {
            return false;
        }
    }
}
