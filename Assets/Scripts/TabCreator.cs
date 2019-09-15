using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TabCreator : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField tabNameField;

    [SerializeField]
    private ChatFilterOption optionPrefab;

    [SerializeField]
    private RectTransform optionContainer;

    [SerializeField]
    private List<ChatChannel> availableChannels;

    [SerializeField]
    private ChatWindow chatWindow;

    private List<ChatFilterOption> options = new List<ChatFilterOption>();

    private void OnEnable()
    {
        foreach (Transform child in optionContainer.transform)
        {
            Destroy(child.gameObject);
        }
        options.Clear();
        
        foreach (ChatChannel channel in availableChannels)
        {
            ChatFilterOption option = Instantiate(optionPrefab, optionContainer);
            option.Init(channel);
            options.Add(option);
        }
    }

    public void Submit()
    {
        List<ChatChannel> channels = new List<ChatChannel>();
        foreach (ChatFilterOption option in options)
        {
            ChatChannel channel = option.TickedChannel();
            if (!string.IsNullOrEmpty(channel.Name)) channels.Add(channel);
        }

        chatWindow.GetChatManager().CreateChatWindow(new ChatTabData(tabNameField.text, channels, true, null), chatWindow, Vector2.zero);
        
        gameObject.SetActive(false);
    }
}