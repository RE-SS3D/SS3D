using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UDiscord; // Use This To Get Discord Manager
public class ExampleButton : MonoBehaviour
{
    public void OnClickJoin()
    {
        DiscordBehaviour.OpenUrl("https://discord.com");
    }
    public void ToggleWindow()
    {
        gameObject.SetActive(false);
    }
}
