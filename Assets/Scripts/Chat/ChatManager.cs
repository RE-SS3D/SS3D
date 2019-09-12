using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChatManager : MonoBehaviour
{
    [SerializeField]
    private List<ChatChannel> chatChannels;

    [SerializeField]
    private ChatWindow chatWindowPrefab;

    [SerializeField]
    private List<Message> messages = new List<Message>();

    private void Start()
    {
        CreateChatWindow(new ChatTabData("All", chatChannels));
    }

    public List<Message> GetMessages(List<ChatChannel> channels)
    {
        return messages.Where(x => channels.Any(y => x.Channel.Equals(y))).ToList();
    }

    public void CreateChatWindow(ChatTabData tabData)
    {
        ChatWindow window = Instantiate(chatWindowPrefab, transform);
        window.Init(tabData, this);
    }
}