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

    private ChatManager manager;

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

    private void Submit()
    {
        List<ChatChannel> channels = new List<ChatChannel>();
        foreach (ChatFilterOption option in options)
        {
            ChatChannel channel = option.TickedChannel();
            if (channel != null) channels.Add(channel);
        }

        manager.CreateChatWindow(new ChatTabData(tabNameField.text, channels));
        
        gameObject.SetActive(false);
    }
}