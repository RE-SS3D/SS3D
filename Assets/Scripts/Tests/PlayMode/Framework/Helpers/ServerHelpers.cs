using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using System.Linq;
using Tests.Play_Mode.Framework.Helpers;

namespace SS3D.Tests
{
    /// <summary>
    /// This class is simply a container for helper methods for the Server, for use
    /// in UnityTests.
    /// </summary>
    public static class ServerHelpers
    {
        public static Process[] CreateClients(int amount, ProcessWindowStyle windowStyle = ProcessWindowStyle.Minimized)
        {
            string filePath;
            Process[] result;

            // Initialize the process return array
            result = new Process[amount];

            // Get relevant executable file path
            filePath = Application.dataPath;
            filePath = filePath.Substring(0, filePath.Length - 6);     // Needed to remove the "Assets" folder.
            filePath += "Builds/Game";                                    // Needed to add the "Builds" folder.

            for (int i = 0; i < amount; i++)
            {
                // Fire up the client.
                result[i] = new Process();
                result[i].StartInfo.WindowStyle = windowStyle;
                result[i].StartInfo.Arguments = $"-ip=localhost -ckey=player_{i} -port=1151 -skipintro";
                result[i].StartInfo.FileName = "SS3D.exe";
                result[i].StartInfo.WorkingDirectory = filePath;
                result[i].Start();
            }

            

            return result;
        }



        public static IEnumerator SetWindowPositions(Process[] process)
        {
            for (int i = 0; i < process.Length;i++)
            {
                LoadFileHelpers.PlaceQuadWindow(process[i], i);
                yield return new WaitForSeconds(0.5f);
            }
        }

        public static IEnumerator WaitUntilClientsLoaded(int amountOfClients, float timeout = 60f)
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            int currentOnlineSouls = 0;
            float startTime = Time.time;

            // This loop simply waits until all players have joined.
            while (currentOnlineSouls < amountOfClients)
            {
                // Allow timeout if needed.
                Assert.IsTrue(Time.time < startTime + timeout, $"Only {currentOnlineSouls} of {amountOfClients} clients loaded after timeout of {timeout} seconds.");

                // Check whether all souls are online.
                currentOnlineSouls = 0;
                foreach (Player player in playerSystem.OnlinePlayers)
                {
                    currentOnlineSouls++;
                }
                
                yield return new WaitForSeconds(2f);
            }

            yield return new WaitForSeconds(2f);
        }


        /// <summary>
        /// Sets all players ready.
        /// </summary>
        public static void SetAllPlayersReady()
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            ReadyPlayersSubSystem readyPlayersSystem = SubSystems.Get<ReadyPlayersSubSystem>();
            ChangePlayerReadyMessage msg;
                
            foreach (Player player in playerSystem.OnlinePlayers)
            {
                msg = new ChangePlayerReadyMessage(player.Ckey, true);
                readyPlayersSystem.ChangePlayerReadyMessageStubBroadcast(player.LocalConnection, msg);
            }
        }

        public static void SetPlayerReadiness(string Ckey, bool readiness)
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            ReadyPlayersSubSystem readyPlayersSystem = SubSystems.Get<ReadyPlayersSubSystem>();
            Player player = playerSystem.OnlinePlayers.ToList().Find(soul => soul.Ckey == Ckey);
            ChangePlayerReadyMessage msg = new ChangePlayerReadyMessage(Ckey, readiness);
            readyPlayersSystem.ChangePlayerReadyMessageStubBroadcast(player.LocalConnection, msg);
        }

        /// <summary>
        /// Turn the round on or off.
        /// </summary>
        public static void ChangeRoundState(bool running)
        {
            RoundSubSystem roundSystem = SubSystems.Get<RoundSubSystem>();
            ChangeRoundStateMessage msg = new ChangeRoundStateMessage(running);
            roundSystem.ChangeRoundStateMessageStubBroadcast(msg);
        }

        public static void SpawnLatePlayer(string Ckey)
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();

            Player player = playerSystem.GetPlayer(Ckey);
            entitySystem.CmdSpawnLatePlayer(player);
        }


    }
}