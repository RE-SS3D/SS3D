using System;
using System.Collections.Generic;
using UnityEngine;

//For simple broadcasts:
public enum Event
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
    private static readonly Dictionary<Event, Action> eventTable
        = new Dictionary<Event, Action>();

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
    public static void AddHandler(Event evnt, Action action)
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

    public static void RemoveHandler(Event evnt, Action action)
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
    public static void Broadcast(Event evnt)
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
    private static void LogEventBroadcast(Event evnt)
    {
        string msg = "Broadcasting a " + evnt + " event";
        Category category;

        switch (evnt)
        {
            case Event.ChatFocused:
            case Event.ChatUnfocused:
            case Event.UpdateChatChannels:
            case Event.UpdateFov:
                category = Category.UI;
                break;
            case Event.DisableInternals:
            case Event.EnableInternals:
                category = Category.Equipment;
                break;
            case Event.LoggedOut:
                category = Category.Connections;
                break;
            case Event.PlayerDied:
                category = Category.Health;
                break;
            case Event.PowerNetSelfCheck:
                category = Category.Electrical;
                break;
            case Event.RoundStarted:
            case Event.RoundEnded:
                category = Category.Round;
                break;
            default:
                category = Category.Unknown;
                break;
        }

        Logger.LogTrace(msg, category);
    }
}
