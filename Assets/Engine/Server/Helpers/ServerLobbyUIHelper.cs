using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class ServerLobbyUIHelper : MonoBehaviour
{
    [SerializeField] private Button embarkButton;

    private void Start()
    {
        embarkButton.onClick.AddListener(delegate { LoginNetworkManager.singleton.SpawnPlayerAfterRoundStart(); });
    }
}
