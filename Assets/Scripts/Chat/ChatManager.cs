using System;
using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class ChatManager : NetworkBehaviour
{
    [SerializeField]
    public Message debugMessage;

    public GameObject Sender;

    [Header("References")]
    [SerializeField]
    private ChatChannels chatChannels;

    [SerializeField]
    private ChatWindow chatWindowPrefab;

    [SerializeField]
    private List<Message> messages = new List<Message>();

    public UnityEvent messageReceivedEvent;


    private void Start()
    {
        CreateChatWindow(new ChatTabData("All", chatChannels.GetChannels(), false, null), null, Vector2.zero);
    }

    public List<Message> GetMessages(List<ChatChannel> channels)
    {
        return messages.Where(x => channels.Any(y => x.Channel.Equals(y))).ToList();
    }

    [Button]
    public void DebugMessage() => RpcReceiveMessage(debugMessage);

    [ClientRpc]
    public void RpcReceiveMessage(Message message)
    {
        messages.Add(message);
        messageReceivedEvent.Invoke();
    }

    [Command]
    public void CmdSendMessage(Message message)
    {
        message.Sender = Sender.name;
        RpcReceiveMessage(message);
    }

    /// <summary>
    /// Creates a new chat window with the supplied tab data.
    /// Adds a tab to an existing window if an existing window is supplied.
    /// </summary>
    /// <param name="tabData">tab data that decides which channels the chat listens to</param>
    /// <param name="existingWindow">an existing windwo to add a chat tab to rather than create a new window</param>
    /// <param name="position">position for the new window (vector2.zero for default position)</param>
    public void CreateChatWindow(ChatTabData tabData, ChatWindow existingWindow, Vector2 position)
    {
        if (existingWindow)
        {
            existingWindow.AddTab(tabData);
        }
        else
        {
            ChatWindow window = Instantiate(chatWindowPrefab, transform);
            if (position != Vector2.zero) window.transform.position = position;
            window.Init(tabData, this);
        }
    }
}