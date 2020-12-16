using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Engine.Server.Round;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ServerLobbyUIHelper : MonoBehaviour
{
    [SerializeField] private Button embarkButton;
    [SerializeField] private TMP_Text embarkText;
    [SerializeField] private Animator animator;

    [SerializeField] private Button serverSettingsButton;
    
    private void Start()
    {
        RoundManager.ClientTimerUpdated += SetTimerText;
        
        RoundManager.ServerRoundStarted += ChangeEmbarkText;
        embarkButton.onClick.AddListener(delegate { LoginNetworkManager.singleton.SpawnPlayerAfterRoundStart(); });
        
        if (NetworkServer.localConnection == null) serverSettingsButton.interactable = true;
    }

    public void ChangeEmbarkText()
    {
        RoundManager.ClientTimerUpdated -= SetTimerText;
        Debug.Log("Updating embark text");
        embarkButton.interactable = true;
        embarkText.text = "Embark";
    }
    
    private void SetTimerText(string text)
    {
        embarkText.text = text;
    } 

}