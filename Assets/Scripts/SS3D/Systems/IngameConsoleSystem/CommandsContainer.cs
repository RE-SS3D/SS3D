using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using UnityEngine.Device;

namespace SS3D.Systems.IngameConsoleSystem
{
    public static class CommandsContainer
    {
        private const string wrongArgsText = "Wrong args. Type \"(command) help\"";
        [ShortDescription("Repeat your string")]
        [LongDescription("echo (number) (your string)")]
        // TODO: actually take da string
        public static string Echo(string times, params object[] a)
        {
            string[] target = Array.ConvertAll(a, item => (string)item);
            return String.Concat(Enumerable.Repeat(String.Join(" ", a), Int32.Parse(times)));
            }
        [ShortDescription("Close app")]
        [LongDescription("Close app")]
        public static string Quit()
        {
            Application.Quit();
            return "Done";
        }
        [ShortDescription("Restart app")]
        [LongDescription("Restart app")]
        public static string Reconnect()
        {
            Process.Start(Application.dataPath.Replace("_Data", ".exe"));
            Application.Quit();
            return "Done";
        }
        [ShortDescription("Show all players online")]
        [LongDescription("Show all players online")]
        public static string PlayerList()
        {
            string ret = "";
            List<Soul> souls = GameSystems.Get<PlayerControlSystem>().OnlineSouls.ToList();
            foreach (Soul i in souls)
            {
                ret += i.Ckey + "\t";
            }
            return ret;
        }
        [ShortDescription("Show all souls")]
        [LongDescription("Show all souls")]
        public static string SoulList()
        {
            string ret = "";
            List<Soul> souls = GameSystems.Get<PlayerControlSystem>().ServerSouls.ToList();
            foreach (Soul i in souls)
            {
                ret += i.Ckey + "\t";
            }
            return ret;
        }
    }
}