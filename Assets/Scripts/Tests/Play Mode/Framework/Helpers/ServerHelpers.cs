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
            filePath += "Builds";                                    // Needed to add the "Builds" folder.

            for (int i = 0; i < amount; i++)
            {
                // Fire up the client.
                result[i] = new Process();
                result[i].StartInfo.WindowStyle = windowStyle;
                result[i].StartInfo.Arguments = $"-ip=localhost -skipintro -ckey=player_{i}";
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
            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
            int currentOnlineSouls = 0;
            float startTime = Time.time;

            // This loop simply waits until all players have joined.
            while (currentOnlineSouls < amountOfClients)
            {
                // Allow timeout if needed.
                Assert.IsTrue(Time.time < startTime + timeout, $"Only {currentOnlineSouls} of {amountOfClients} clients loaded after timeout of {timeout} seconds.");

                // Check whether all souls are online.
                currentOnlineSouls = 0;
                foreach (var soul in playerSystem.OnlineSouls)
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
            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
            ReadyPlayersSystem readyPlayersSystem = Subsystems.Get<ReadyPlayersSystem>();
            ChangePlayerReadyMessage msg;
                
            foreach (Soul soul in playerSystem.OnlineSouls)
            {
                msg = new ChangePlayerReadyMessage(soul.Ckey, true);
                readyPlayersSystem.ChangePlayerReadyMessageStubBroadcast(soul.LocalConnection, msg);
            }
        }

        public static void SetPlayerReadiness(string Ckey, bool readiness)
        {
            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
            ReadyPlayersSystem readyPlayersSystem = Subsystems.Get<ReadyPlayersSystem>();
            Soul soul = playerSystem.OnlineSouls.ToList().Find(soul => soul.Ckey == Ckey);
            ChangePlayerReadyMessage msg = new ChangePlayerReadyMessage(Ckey, readiness);
            readyPlayersSystem.ChangePlayerReadyMessageStubBroadcast(soul.LocalConnection, msg);
        }

        /// <summary>
        /// Turn the round on or off.
        /// </summary>
        public static void ChangeRoundState(bool running)
        {
            RoundSystem roundSystem = Subsystems.Get<RoundSystem>();
            ChangeRoundStateMessage msg = new ChangeRoundStateMessage(running);
            roundSystem.ChangeRoundStateMessageStubBroadcast(msg);
        }

        public static void SpawnLatePlayer(string Ckey)
        {
            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
            EntitySystem entitySystem = Subsystems.Get<EntitySystem>();

            Soul soul = playerSystem.GetSoul(Ckey);
            entitySystem.CmdSpawnLatePlayer(soul);
        }


    }
}