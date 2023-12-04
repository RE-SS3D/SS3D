using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapNameSlot : MonoBehaviour
{
    [SerializeField]
    private UnityEngine.UI.Button _mapNameButton;


    [SerializeField]
    private UnityEngine.UI.Button _deleteButton;

    public UnityEngine.UI.Button MapNameButton => _mapNameButton;

    public UnityEngine.UI.Button DeleteButton => _deleteButton;
}
