using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SS3D.Core;
using UnityEngine.Device;

namespace SS3D.UI.IngameConsole
{
    public static class CommandsContainer
    {
        [ShortDescription("isnt used for showing players")]
        [LongDescription("isnt used for showing players but long")]
        public static string Echo(string b, int a)
        {
            return String.Concat(Enumerable.Repeat(b, a));
        }
        [ShortDescription("Close your game")]
        [LongDescription("Close your game")]
        public static string Quit()
        {
            Application.Quit();
            return "Done";
        }
        [ShortDescription("Restart your game")]
        [LongDescription("Restart your game")]
        public static string Reconnect()
        {
            Process.Start(Application.dataPath.Replace("_Data", ".exe"));
            Application.Quit();
            return "Done";
        }
        [ShortDescription("Restart your game")]
        [LongDescription("Restart your game")]
        public static string Fish()
        {
            return "Done";
        }
        /*[ShortDescription("Restart your game")]
        [LongDescription("Restart your game")]
        public static string PlayerList()
        {
            string ret = "";
            List<Soul> souls = GameSystems.Get<PlayerControlSystem>()._onlineSouls.ToList();
            foreach (Soul i in souls)
            {
                ret += i.Ckey + "\t";
            }
            return ret;
        }
        [ShortDescription("Restart your game")]
        [LongDescription("Restart your game")]
        public static string SoulList()
        {
            string ret = "";
            List<Soul> souls = GameSystems.Get<PlayerControlSystem>()._serverSouls.ToList();
            foreach (Soul i in souls)
            {
                ret += i.Ckey + "\t";
            }
            return ret;
        }*/
    }
}