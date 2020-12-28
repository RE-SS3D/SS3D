using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Engine.Server.Round;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ServerLobbyUIHelper : NetworkBehaviour
{
    [SerializeField] private Button embarkButton;
    [SerializeField] private TMP_Text embarkText;
    [SerializeField] private TMP_Text timer;
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
        timer.gameObject.SetActive(false);
        Debug.Log("Updating embark button");
        embarkButton.interactable = true;
        embarkText.gameObject.SetActive(true);
    }
    
    private void SetTimerText(string text)
    {
        timer.text = text;
    } 

}