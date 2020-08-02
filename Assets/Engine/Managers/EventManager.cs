using System;
using System.Collections.Generic;
using UnityEngine;

//For simple broadcasts:
public enum GlobalEvent
{
    UpdateFov,
    PowerNetSelfCheck,
    ChatFocused,
    ChatUnfocused,
    LoggedOut,
    RoundStarted,
    RoundEnded,
    DisableInternals,
    EnableInternals,
    PlayerSpawned,
    PlayerDied,
    GhostSpawned,
    LogLevelAdjusted,
    UpdateChatChannels,
    ToggleChatBubbles,
    PlayerRejoined,
    PreRoundStarted,
    MatrixManagerInit
} // + other events. Add them as you need them

[ExecuteInEditMode]
public class EventManager : MonoBehaviour
{
    // Stores the delegates that get called when an event is fired (Simple Events)
    private static readonly Dictionary<GlobalEvent, Action> eventTable
        = new Dictionary<GlobalEvent, Action>();

    private static EventManager eventManager;

    public static EventManager Instance
    {
        get
        {
            if (!eventManager)
            {
                eventManager = FindObjectOfType<EventManager>();
            }
            return eventManager;
        }
    }

    public static void UpdateLights()
    {
    }

    /*
		* Below is for the simple event handlers and broast methods:
		*/

    // Adds a delegate to get called for a specific event
    public static void AddHandler(GlobalEvent evnt, Action action)
    {
        if (!eventTable.ContainsKey(evnt))
        {
            eventTable[evnt] = action;
        }
        else
        {
            eventTable[evnt] += action;
        }
    }

    public static void RemoveHandler(GlobalEvent evnt, Action action)
    {
        if (eventTable[evnt] != null)
        {
            eventTable[evnt] -= action;
        }
        if (eventTable[evnt] == null)
        {
            eventTable.Remove(evnt);
        }
    }

    // Fires the event
    public static void Broadcast(GlobalEvent evnt)
    {
        LogEventBroadcast(evnt);
        if (eventTable.ContainsKey(evnt) && eventTable[evnt] != null)
        {
            eventTable[evnt]();

        }
    }

    /// <summary>
    /// Calls the appropriate logging category for the event
    /// </summary>
    /// <param name="evnt"></param>
    private static void LogEventBroadcast(GlobalEvent evnt)
    {
        string msg = "Broadcasting a " + evnt + " event";
        Category category;

        switch (evnt)
        {
            case GlobalEvent.ChatFocused:
            case GlobalEvent.ChatUnfocused:
            case GlobalEvent.UpdateChatChannels:
            case GlobalEvent.UpdateFov:
                category = Category.UI;
                break;
            case GlobalEvent.DisableInternals:
            case GlobalEvent.EnableInternals:
                category = Category.Equipment;
                break;
            case GlobalEvent.LoggedOut:
                category = Category.Connections;
                break;
            case GlobalEvent.PlayerDied:
                category = Category.Health;
                break;
            case GlobalEvent.PowerNetSelfCheck:
                category = Category.Electrical;
                break;
            case GlobalEvent.RoundStarted:
            case GlobalEvent.RoundEnded:
                category = Category.Round;
                break;
            default:
                category = Category.Unknown;
                break;
        }

        Logger.LogTrace(msg, category);
    }
}
