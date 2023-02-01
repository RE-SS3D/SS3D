using FishNet.Managing.Server;
using FishNet.Broadcast;
using FishNet.Connection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.InputHandling;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using SS3D.UI.Buttons;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FishNet;

namespace SS3D.Tests
{
    /// <summary>
    /// This class is simply a container for helper methods for the Server, for use
    /// in UnityTests.
    /// </summary>
    public static class ServerHelpers
    {
        public static IEnumerator CreateClients(int amount)
        {
            string filePath;
            Process cmdLineProcess;

            // Get relevant executable file path
            filePath = Application.dataPath;
            filePath = filePath.Substring(0, filePath.Length - 6);     // Needed to remove the "Assets" folder.
            filePath += "Builds";                                    // Needed to add the "Builds" folder.

            for (int i = 0; i < amount; i++)
            {
                // Fire up the client.
                cmdLineProcess = new Process();
                cmdLineProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                cmdLineProcess.StartInfo.Arguments = $"-ip=localhost -skipintro -ckey=player_{Random.Range(0, 1000000)}";
                cmdLineProcess.StartInfo.FileName = "SS3D.exe";
                cmdLineProcess.StartInfo.WorkingDirectory = filePath;
                cmdLineProcess.Start();
                //cmdLineProcess.WaitForExit();

                yield return new WaitForSeconds(5f);
            }

            PlayerSystem playerSystem = SystemLocator.Get<PlayerSystem>();
            int currentOnlineSouls = 0;

            // This loop simply waits until all players have joined.
            while (currentOnlineSouls < amount)
            {
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
            PlayerSystem playerSystem = SystemLocator.Get<PlayerSystem>();
            ReadyPlayersSystem readyPlayersSystem = SystemLocator.Get<ReadyPlayersSystem>();
            ChangePlayerReadyMessage msg;
                
            foreach (Soul soul in playerSystem.OnlineSouls)
            {
                msg = new ChangePlayerReadyMessage(soul.Ckey, true);
                readyPlayersSystem.ChangePlayerReadyMessageStubBroadcast(soul.LocalConnection, msg);
            }
        }

        /// <summary>
        /// Turn the round on or off.
        /// </summary>
        public static void ChangeRoundState(bool running)
        {
            RoundSystem roundSystem = SystemLocator.Get<RoundSystem>();
            ChangeRoundStateMessage msg = new ChangeRoundStateMessage(running);
            roundSystem.ChangeRoundStateMessageStubBroadcast(msg);
        }


    }
}