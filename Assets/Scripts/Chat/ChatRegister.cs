using Mirror;

public class ChatRegister : NetworkBehaviour
{
    void Start()
    {
        if (isLocalPlayer)
        {
            ChatManager chatManager = FindObjectOfType<ChatManager>();
            chatManager.Sender = gameObject;

            chatManager.GetComponent<NetworkIdentity>()
                .AssignClientAuthority(GetComponent<NetworkIdentity>().connectionToClient);
        }
    }
}