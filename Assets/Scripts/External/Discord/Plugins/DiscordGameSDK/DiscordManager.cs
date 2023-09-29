#region Discord References

using System;
using SS3D.Logging;
using UnityEngine;
using UnityEngine.Events;
using LogType = SS3D.Logging.LogType;

#endregion

namespace UDiscord
{
    public class DiscordManager : DiscordBehaviour
    {
        #region Settings
        public static DiscordManager App;
        public long Discord_AppID;
        public string Discord_SteamID;
        public bool Discord_Stay = false;
        public bool Discord_Start = false;
        public RichPresence Richpresence = new RichPresence();

        [Header("Events Settings")]
        public UnityEvent OnJoin = new UnityEvent();
        public static UnityEvent OnConnect = new UnityEvent();
        public UnityEvent OnDisconnect = new UnityEvent();
        public UnityEvent OnDestroy = new UnityEvent();

        #endregion

        #region Start/Update

        private void Awake()
        {
            OnConnect.AddListener(CallDiscord);
            App = this;
        }

        public static void Initialize()
        {
            
            OnConnect?.Invoke();
        }

        private void Update()
        {
            discord?.RunCallbacks();
        }

        #endregion

        #region Discord Fuctions

        // CreateRich : is for Get ID and Activities of Discord to Show Rich in discord
        public void CreateRich(long ID , ulong flag,string detail, string state = null, long start = -1, long end = -1, string largeKey = null,string largeText = null, string smallKey = null, string smallText = null, string partyId = null, int size = -1, int max = -1, string match = null, string join = null, string spectate = null)
        {
            GetAppID(ID);
            var activityManager = discord.GetActivityManager();

            var activity = new Activity 
            {
                Details = detail ?? Richpresence.details,
                State = state ?? Richpresence.state,
                Timestamps =
                {
                    Start = (start == -1) ? Richpresence.startTimestamp : start,
                    End = (end == -1) ? Richpresence.endTimestamp : end
                },

                Assets =
                {
                    LargeImage = largeKey ?? Richpresence.largeImageKey,  // Larger Image Asset Key
                    LargeText = largeText ?? Richpresence.largeImageText,  // Large Image Tooltip

                    SmallImage = smallKey ?? Richpresence.smallImageKey,  // Small Image Asset Key
                    SmallText = smallText ?? Richpresence.smallImageText,  // Small Image Tooltip
                },

                Party =
                {
                    Id = partyId ?? Richpresence.partyId,

                    Size = {
                        CurrentSize = (size == -1) ? Richpresence.partySize : size,
                        MaxSize = (max == -1) ? Richpresence.partyMax : max
                    },
                },

                Secrets =
                {
                    Match = match ?? Richpresence.matchSecret,
                    Join = join ?? Richpresence.joinSecret,
                    Spectate = spectate ?? Richpresence.spectateSecret
                }
            };

            activityManager.UpdateActivity(activity, (res) => 
            {
                if(res == Result.Ok)
                {
                    Debug.Log($"[{typeof(DiscordManager)}] - Discord Status Is On!");
                }
                else
                {
                    Debug.LogError($"[{typeof(DiscordManager)}] - Discord Status Failed!");
                }
            });
        }

        //UpdateRich : Is Fuction to Update Rich by Typing : UpdateRich(detail: "Detail text" , state : "State text");
        public void UpdateRich(string detail = null, string state = null, long start = -1, long end = -1, string largeKey = null,string largeText = null, string smallKey = null, string smallText = null, string partyId = null, int size = -1, int max = -1, string match = null, string join = null, string spectate = null)
        {
            var activityManager = discord.GetActivityManager();
            
            var activity = new Activity 
            {
                Details = detail ?? Richpresence.details,
                State = state ?? Richpresence.state,

                Timestamps =
                {
                    Start = (start == -1) ? Richpresence.startTimestamp : start,
                    End = (end == -1) ? Richpresence.endTimestamp : end
                },

                Assets =
                {
                    LargeImage = largeKey ?? Richpresence.largeImageKey,  // Larger Image Asset Key
                    LargeText = largeText ?? Richpresence.largeImageText,  // Large Image Tooltip

                    SmallImage = smallKey ?? Richpresence.smallImageKey,  // Small Image Asset Key
                    SmallText = smallText ?? Richpresence.smallImageText,  // Small Image Tooltip
                },

                Party =
                {
                    Id = partyId ?? Richpresence.partyId,

                    Size = 
                    {
                        CurrentSize = (size == -1) ? Richpresence.partySize : size,
                        MaxSize = (max == -1) ? Richpresence.partyMax : max
                    },
                },

                Secrets =
                {
                    Match = match ?? Richpresence.matchSecret,
                    Join = join ?? Richpresence.joinSecret,
                    Spectate = spectate ?? Richpresence.spectateSecret
                }
            };

            activityManager.UpdateActivity(activity, (res) => 
            {
                if(res == Result.Ok)
                {
                    Debug.Log($"[{typeof(DiscordManager)}] - Discord Status Is On!");
                }
                else
                {
                    Debug.LogError($"[{typeof(DiscordManager)}] - Discord Status Failed!");
                }
            });
        }

        // CallDiscord : Is for Connect Discord with unity (Template) , Just Use Bool Discord_Start to See Template on Action after Filling RichPresence    
        void CallDiscord()
        {
            //getting Discord User ID and Default Stats
            discord = new UDiscord(Discord_AppID , (UInt64)CreateFlags.Default);

            var activityManager = discord.GetActivityManager();
            var lobbyManager = discord.GetLobbyManager();

            var activity = new Activity 
            {
                Details = Richpresence.details,
                State = Richpresence.state,
                Timestamps =
                {
                    Start = Richpresence.startTimestamp,
                    End = Richpresence.endTimestamp
                },
                Assets =
                {
                    LargeImage = Richpresence.largeImageKey,  // Larger Image Asset Key
                    LargeText = Richpresence.largeImageText,  // Large Image Tooltip
                    SmallImage = Richpresence.smallImageKey,  // Small Image Asset Key
                    SmallText = Richpresence.smallImageText,  // Small Image Tooltip
                },
                Party =
                {
                    Id = Richpresence.partyId,
                    Size = {
                        CurrentSize = Richpresence.partySize,
                        MaxSize = Richpresence.partyMax,
                    },
                },
                Secrets =
                {
                    Match = Richpresence.matchSecret,
                    Join = Richpresence.joinSecret,
                    Spectate = Richpresence.spectateSecret
                }
            };       


            activityManager.UpdateActivity(activity, (res) => 
            {
                activityManager.SendInvite(485905734618447895, ActivityActionType.Join, "", (inviteUserResult) =>
                {
                    Console.WriteLine("Invite User {0}", inviteUserResult);
                    Log.Information(nameof(DiscordManager), "How is This Working >???", Logs.External);
                });

                if(res == Result.Ok)
                {
                    Log.Information(this, "Discord Status Is On!", Logs.External);
                }
                else
                {
                    Log.Error(nameof(DiscordManager), "Discord Status Failed!", Logs.External);
                }
            });
        }
        #endregion

        #region Others

        void OnDisable()
        {
            Log.Warning(nameof(DiscordManager), "Discord Shutdown after 10 seconds", Logs.External);

            Shutdown();
            discord?.Dispose();
        }

        #endregion

    }
}
