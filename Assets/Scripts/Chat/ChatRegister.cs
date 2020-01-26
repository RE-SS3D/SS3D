using Mirror;
using System.Collections.Generic;
using System;

public class ChatRegister : NetworkBehaviour
{
    private ChatManager chatManager;
    public List<String> restrictedChannels = new List<String>(){"System"};

    private void Awake()
    {
        chatManager = FindObjectOfType<ChatManager>();
    }

    [Command]
    public void CmdSendMessage(Message message)
    {
        if(restrictedChannels.Contains(message.Channel.Name)) return;
        message.Sender = gameObject.name;
        chatManager.RpcReceiveMessage(message);
    }
}