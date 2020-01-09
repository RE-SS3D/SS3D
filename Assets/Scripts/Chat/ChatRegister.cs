using Mirror;

public class ChatRegister : NetworkBehaviour
{
    private ChatManager chatManager;

    private void Awake()
    {
        chatManager = FindObjectOfType<ChatManager>();
    }

    [Command]
    public void CmdSendMessage(Message message)
    {
        message.Sender = gameObject.name;
        chatManager.RpcReceiveMessage(message);
    }
}