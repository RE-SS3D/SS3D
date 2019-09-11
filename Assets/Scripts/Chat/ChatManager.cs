using System;
using System.Collections.Generic;
using UnityEngine;

public class ChatManager : MonoBehaviour
{
    [SerializeField]
    private List<ChatChannel> channels;

    [SerializeField]
    private ChatWindow chatWindowPrefab;

    private List<Message> messages = new List<Message>();

    private void Start()
    {
        // Get all the 
        CreateChatWindow();
    }

    public void CreateChatWindow()
    {
        ChatWindow window = Instantiate(chatWindowPrefab);
        window.Init(messages, channels);
    }
}