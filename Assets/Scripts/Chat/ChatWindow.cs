using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ludiq.PeekCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatWindow : MonoBehaviour
{
    [SerializeField]
    private RectTransform tabRow;

    [SerializeField]
    private TextMeshProUGUI ChatText;

    private List<ChatChannel> channelFilters;

    [SerializeField]
    private ChatTab chatTabPrefab;

    private ChatTabData currentTabData;

    private ChatManager chatManager;

    public void Init(ChatTabData tabData, ChatManager chatManager)
    {
        this.chatManager = chatManager;

        ChatTab chatTab = Instantiate(chatTabPrefab, tabRow);
        chatTab.UpdateText(tabData.Name);
        chatTab.transform.SetAsFirstSibling();

        LoadTabChatLog(tabData);

        currentTabData = tabData;
    }

    public void LoadTabChatLog(ChatTabData tabData)
    {
        StringBuilder sb = new StringBuilder();
        foreach (Message message in chatManager.GetMessages(tabData.Channels))
        {
            sb.AppendFormat(
                "<color=#{0}>[{1}] {2}: {3}\n",
                message.Channel.Color.ToHexString(),
                message.Channel.Abbreviation,
                message.Sender.name,
                message.Text);
            Debug.Log("i work");
        }

        ChatText.text = sb.ToString();
    }

//    public void AddMessage(Message message)
//    {
//        messages.Add(message);
//    }
}