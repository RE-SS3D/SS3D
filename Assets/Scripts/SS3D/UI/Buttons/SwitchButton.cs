using SS3D.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Button that can have two different images and that may do some noise when clicked on.
/// </summary>
public class SwitchButton : MonoBehaviour
{

    [SerializeField]
    private Button _button;

    [SerializeField]
    private Image _image;

    [SerializeField]
    private Sprite _stateOnSprite;

    [SerializeField]
    private Sprite _stateOffSprite;

    public Action<bool> Switch;

    public bool State { get; private set; }

    
    // Start is called before the first frame update
    public void Start()
    {
        _button.onClick.AddListener(SwitchOnClick);
    }

    private void SwitchOnClick()
    {
        SetState(!State, true);
    }

    public void SetState(bool state, bool withSound = false)
    {
        if(state == State) return;

        State = state;
        _image.sprite = state ? _stateOnSprite : _stateOffSprite;
        Switch?.Invoke(state);
    }
}