using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Engine.Server.Round;
using SS3D.Engine.Tiles;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking.Types;
using UnityEngine.UI;

/// <summary>
/// This class server sole purpose of helping control the server lobby UI,
/// I despise how it works but it is the solution I have found.
/// cum.
/// </summary>
public class ServerLobbyUIHelper : NetworkBehaviour
{
    public static ServerLobbyUIHelper singleton { get; private set; }
    
    // the button we use to join a round
    [SerializeField] private Button embarkButton;
    
    // Here we have the embarkText which just displays "embark" and the timer countdown.
    // Look, the only reason why there are two text objects on the embark part
    // is cause for some reason "RoundManager.ClientTimerUpdated -= SetTimerText();" doesn't work,
    // otherwise I would have done that.
    [SerializeField] private TMP_Text embarkText;
    [SerializeField] private TMP_Text timer;
    
    [SerializeField] private Animator animator;

    // The admin panel, should be only accessible to admin users, for now used only for the host
    // TODO: User permissions
    [SerializeField] private Button serverSettingsButton;

    private void Awake()
    {
        // Here begins the disaster 

        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
        }
        else
        {
            singleton = this;
        }

        // ClientTimerUpdated sets up the "string text" of SetTimerText,
        // that event is called on the RoundManager when the timer is running (Countdown and Round time)
        RoundManager.ClientTimerUpdated += SetTimerText;
        RoundManager.ServerWarmupStarted += EnableTimer;
        
        // Updates the Embark button text to "Embark"
        RoundManager.ServerRoundStarted += ChangeEmbarkText;
        // Updates the menu if the round ends, maybe we can change later to a final round end later
        RoundManager.ServerRoundEnded += ForceToggleOn;
        
        // Makes the button's function be CmdRequestEmbark and the UI fade out
        embarkButton.onClick.AddListener(delegate
        {
            CmdRequestEmbark();
            Toggle(false);
        });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && RoundManager.singleton.IsRoundStarted)
        {
            Toggle();
        }
    }

    // Here we allow the player to access the server settings
    public void UnlockServerSettings() 
    {
        // TODO:
        // Sync server list and set up user authority to open admin panel
        serverSettingsButton.interactable = true;
    }
    
    // Handles asking the server to spawn the player
    // the "sender" param is handled by Mirror, no need to worry about it
    [Command(requiresAuthority = false)]
    public void CmdRequestEmbark(NetworkConnectionToClient sender = null)
    {
        // Spawns the player
        LoginNetworkManager.singleton.SpawnPlayerAfterRoundStart(sender);
    }  

    // Updates the embark text status according to the round status (starting, started, stopped)
    public void ChangeEmbarkText()
    {
        // There's a timer UI so we deactivate it and make the embark appear
        timer.gameObject.SetActive(false);
        //Debug.Log("Updating embark button");
        embarkText.gameObject.SetActive(true);

	// and we wait until the map is loaded (locally) for the embark button to be unlocked
        StartCoroutine(WaitUntilMapLoaded());
    }

    public IEnumerator WaitUntilMapLoaded()
    {
        yield return new WaitUntil(
            delegate
            {
                return TileManager.Instance.IsInitialized;
            }
        );
        
        embarkButton.interactable = true;
    }

    // in the case we need to force the Lobby on the player, when the round ends for example
    private void ForceToggleOn()
    {
        // pain
        Toggle(true);
        timer.gameObject.SetActive(false);
        embarkText.gameObject.SetActive(true);
        embarkButton.interactable = false;
    }
    
    private void Toggle(bool toggle)
    {
        if (!animator.enabled) animator.enabled = true;
        animator.SetBool("Toggle", toggle);
    }

    private void Toggle()
    {
        bool state = animator.GetBool("Toggle");
        Toggle(!state);
    }

    // Triggered when warmup is started
    private void EnableTimer()
    {
            timer.gameObject.SetActive(true);
            embarkText.gameObject.SetActive(false);
    }
    private void SetTimerText(int time)
    {
        timer.text = time.ToString();
    }

    private void OnDestroy()
    {
        RoundManager.ClientTimerUpdated -= SetTimerText;
        RoundManager.ServerWarmupStarted -= EnableTimer;
        RoundManager.ServerRoundStarted -= ChangeEmbarkText;
        RoundManager.ServerRoundEnded -= ForceToggleOn;
    }
}
