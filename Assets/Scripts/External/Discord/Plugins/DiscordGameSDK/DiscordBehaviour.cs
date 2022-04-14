using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UDiscord
{
    public class DiscordBehaviour : MonoBehaviour
    {
        public static UDiscord discord;

        #region Behaviours

        public void GetAppID(long ID)
        {
            // Default is 0 you can 
            discord = new UDiscord(ID , (UInt64)CreateFlags.Default);
        }

        public void GetAppID(long ID , ulong flag)
        {
            // Default is 0 you can 
            UDiscord discord = new UDiscord(ID , flag);
        }

        // GetUserName : Will Give you String with your Discord Name
        public string GetUserName()
        {
            discord.GetUserManager();

            var userN = discord.UserManagerInstance.GetCurrentUser();

            return userN.Username;
        }

        public Int64 GetUserID()
        {
            discord.GetUserManager();

            var userid = discord.UserManagerInstance.GetCurrentUser();

            return userid.Id;
        }
        public static void OpenUrl(string url)
        {
            Application.OpenURL(url);
        }

        #region Dll

        [DllImport("RichDiscord", EntryPoint = "Discord_Shutdown", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Shutdown();

        #endregion

        #endregion
    }
}

#region Rich Presence Childerns

    //Activity Type : Means the Type of Discord like :
    /*
        .Playing : Discord do , Playing A Game
        .Streaming : Discord do , Streaming A Game
        .Listening : Discord do , Listening A Song
        .Watching : Discord do , Watching A Movie
    */
    public enum ActivityType
    {
        Playing,
        Streaming,
        Listening,
        Watching,
    }


    //CreateFlags : Just Make it 0 ;)
    public enum CreateFlags
    {
        Default = 0,
        NoRequireDiscord = 1,
    }


    //Activities : Is All the Activities you can add in Discord like type , id , name , state
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public partial struct Activities
    {
        public ActivityType Type;

        public Int64 ApplicationId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string State;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Details;

        public ActivityTimestamps Timestamps;

        public ActivityAssets Assets;

        public ActivityParty Party;

        public ActivitySecrets Secrets;

        public bool Instance;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public partial struct ActivitySecrets
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Match;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Join;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Spectate;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public partial struct ActivityTimestamps
    {
        public Int64 Start;

        public Int64 End;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public partial struct ActivityAssets
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string LargeImage;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string LargeText;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string SmallImage;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string SmallText;
    }



    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public partial struct PartySize
    {
        public Int32 CurrentSize;

        public Int32 MaxSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public partial struct ActivityParty
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Id;

        public PartySize Size;
    }
    #endregion


#region RichPresence
            
    [Serializable]
    public class RichPresence
    {
        [Header("Main Settings")]
        public string details;           // max 128 bytes
        public string state;            // max 128 bytes
        public long startTimestamp;
        public long endTimestamp;

        [Header("Image Settings")]
        public string largeImageKey;    // max 32 bytes
        public string largeImageText;   // max 128 bytes
        public string smallImageKey;    // max 32 bytes
        public string smallImageText; 

        [Header("Party Settings")] // max 128 bytes
        public string partyId;          // max 128 bytes
        public int partySize;
        public int partyMax;
                
        [Header("Match Settings")]
        public string matchSecret;      // max 128 bytes
        public string joinSecret;       // max 128 bytes
        public string spectateSecret;   // max 128 bytes
    }

#endregion
