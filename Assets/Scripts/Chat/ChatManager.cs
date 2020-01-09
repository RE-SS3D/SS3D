using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class ChatManager : NetworkBehaviour
{
    [SerializeField] public Message debugMessage;

    public GameObject Sender;

    [Header("References")] [SerializeField]
    private ChatChannels chatChannels;

    [SerializeField] private ChatWindow chatWindowPrefab;

    [SerializeField] private List<Message> messages = new List<Message>();

    public UnityEvent messageReceivedEvent;

    private bool chatCreated;
    private ChatWindow chatWindow;

    private void Update()
    {
        if (chatCreated)
        {
            return;
        }

        //TODO: This should probably be converted to an Initialize() method and simply called after the player is spawned.
        NetworkIdentity player = ClientScene.localPlayer;
        if (player == null)
        {
            return;
        }

        Sender = player.gameObject;
        CreateChatWindow(new ChatTabData("All", chatChannels.GetChannels(), false, null), null, Vector2.zero);
        chatCreated = true;
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
            chatWindow = Instantiate(chatWindowPrefab, transform);
            if (position != Vector2.zero)
            {
                chatWindow.transform.position = position;
            }

            chatWindow.Init(tabData, this, Sender.GetComponent<ChatRegister>());
        }
    }

    public ChatWindow GetChatWindow()
    {
        return chatWindow;
    }
}