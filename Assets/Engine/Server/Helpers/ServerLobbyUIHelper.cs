using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Engine.Server.Round;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// This class server sole purpose of helping me control UI,
// I despise how it works but it is the solution I have found.
// cum.
public class ServerLobbyUIHelper : NetworkBehaviour
{
    [SerializeField] private Button embarkButton;
    
    [SerializeField] private TMP_Text embarkText;
    [SerializeField] private TMP_Text timer;
    
    [SerializeField] private Animator animator;

    [SerializeField] private Button serverSettingsButton;
    
    private void Start()
    {
        // ClientTimerUpdated sets up the "string text" of SetTimerText,
        // that event is called on the RoundManager when the timer is running (Countdown and Round time)
        RoundManager.ClientTimerUpdated += SetTimerText;
        
        // Updates the Embark button text to "Embark"
        RoundManager.ServerRoundStarted += ChangeEmbarkText;
        
        // Makes the button's function be CmdRequestEmbark
        embarkButton.onClick.AddListener(delegate { CmdRequestEmbark(); });
        
        // TODO:
        // Sync server list and set up user authority to open admin panel
        if (!isLocalPlayer) serverSettingsButton.interactable = true;
        
        // Not sure if this work
        if (RoundManager.singleton.IsRoundStarted)
        {
            embarkButton.interactable = true;
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdRequestEmbark(NetworkConnectionToClient sender = null)
    {
        embarkButton.interactable = false;
        
        // Spawns the player
        LoginNetworkManager.singleton.SpawnPlayerAfterRoundStart(sender);
    }

    public void ChangeEmbarkText()
    {
        // There's a timer UI so we deactivate it and make the embark appear
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