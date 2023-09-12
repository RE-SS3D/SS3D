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
using SS3D.Systems.Entities.Humanoid;
using System.Text;

namespace SS3D.Tests
{
    public class ServerGameActions : SpessPlayModeTest
    {

        protected Process[] clientProcess;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            // Set to run as server
            SetApplicationSettings(NetworkType.DedicatedServer);

            // Load the startup scene (which will subsequently load the lobby once connected)
            LoadStartupScene();
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            // Make several clients
            const int clientsToCreate = 8;
            clientProcess = ServerHelpers.CreateClients(clientsToCreate, ProcessWindowStyle.Normal);
            yield return ServerHelpers.SetWindowPositions(clientProcess);
            yield return ServerHelpers.SetWindowPositions(clientProcess);
            yield return ServerHelpers.WaitUntilClientsLoaded(clientsToCreate);
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            ServerHelpers.ChangeRoundState(false);
            KillClientProcesses();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayersRemainAboveStationLevelAfterSpawn()
        {
            // Get everyone into the round, and wait till it is properly loaded.
            ServerHelpers.SetAllPlayersReady();
            yield return new WaitForSeconds(1f);
            ServerHelpers.ChangeRoundState(true);
            yield return new WaitForSeconds(5f);

            // See if the players are below where they should be.
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            yield return PlaymodeTestRepository.PlayersRemainAboveStationLevelAfterSpawn(players);
        }

        [UnityTest]
        public IEnumerator FreePlayMultiplayer()
        {
            // Get everyone into the round, and wait till it is properly loaded.
            ServerHelpers.SetAllPlayersReady();
            yield return new WaitForSeconds(1f);
            ServerHelpers.ChangeRoundState(true);
            yield return new WaitForSeconds(5f);

            yield return TestHelpers.ContinueFreePlayUntilControlAltBackspacePressed();
        }

        protected void KillClientProcesses()
        {
            foreach (Process process in clientProcess)
            {
                process.CloseMainWindow();
                process.Close();
            }
        }

        protected override bool UseMockUpInputs()
        {
            return false;
        }
    }
}
