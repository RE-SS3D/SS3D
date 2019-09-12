using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatFilterOption : MonoBehaviour
{
    private string name;

    [SerializeField]
    private TextMeshProUGUI label;

    private Toggle toggle;

    private ChatChannel channel;

    public void Init(ChatChannel channel)
    {
        this.channel = channel;
        label.text = channel.name;
    }

    public ChatChannel TickedChannel() => toggle.isOn ? channel : null;
}