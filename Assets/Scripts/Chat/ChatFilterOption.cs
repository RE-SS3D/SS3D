using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatFilterOption : MonoBehaviour
{
    private string name;

    [SerializeField]
    private TextMeshProUGUI label;

    [SerializeField]
    private Toggle toggle;

    [SerializeField]
    private ChatChannels chatChannels;

    private ChatChannel channel;

    public void Init(ChatChannel channel)
    {
        this.channel = channel;
        label.text = channel.Name;
    }

    public ChatChannel TickedChannel() => toggle.isOn ? channel : new ChatChannel();
}