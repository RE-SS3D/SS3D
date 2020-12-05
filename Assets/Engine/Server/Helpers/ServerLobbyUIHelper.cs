using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Engine.Server.Round;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ServerLobbyUIHelper : MonoBehaviour
{
    [SerializeField] private Button embarkButton;
    [SerializeField] private Animator animator;

    private void Start()
    {
        RoundManager.singleton.ServerRoundStarted += Fade;
        embarkButton.onClick.AddListener(delegate { LoginNetworkManager.singleton.SpawnPlayerAfterRoundStart(); });
    }

    public void Fade()
    {
        animator.SetTrigger("Fade");
    }

}
