using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class consoleWindowController : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private GameObject _commandTray;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _consoleWindowRectTransform;

    [Header("Runtime")]
    [SerializeField] private bool _consoleWindowOpen = false;

    //Components
    private RectTransform _canvasRectTransform;
    private RectTransform _commandTrayRectTransform;

    //Working Variables
    private bool _isResizing = false;
    private float _trayPosition = 0;

    void Start()
    {
        if (_commandTray == null) { Debug.LogWarning("Command console controller needs a command tray game object. Manager Disabled"); gameObject.SetActive(false); }
        _commandTrayRectTransform = _commandTray.GetComponent<RectTransform>();
        if( _commandTrayRectTransform == null ) { Debug.LogWarning("Command console controller needs a command tray rect transform. Manager Disabled"); gameObject.SetActive(false); }
        if (_canvas == null) { Debug.LogWarning("Command Console needs a Canvas component. Manager Disabled"); gameObject.SetActive(false); }
        _canvasRectTransform = _canvas.gameObject.GetComponent<RectTransform>(); //Canvas already has a RectTransform RequireComponent, so this should be fine.
        _trayPosition = _commandTray.transform.position.y;
    }

    void Update()
    {
        if( Input.GetKeyDown( KeyCode.F12 ) ) _consoleWindowOpen = !_consoleWindowOpen;

        _consoleWindowRectTransform.gameObject.SetActive(_consoleWindowOpen);

        //Get the command tray to follow the mouse when the resize button is held.
        if (_isResizing)
        {
            //Set the bottom of the console to mouse position.
            _trayPosition = Mathf.Clamp(Input.mousePosition.y, 0, _canvasRectTransform.rect.height - _commandTrayRectTransform.rect.height );
            _consoleWindowRectTransform.offsetMin = new Vector2(_consoleWindowRectTransform.offsetMin.x, _trayPosition + _commandTrayRectTransform.rect.height);
        }
    }

    public void resizePointerUp()
    {
        _isResizing = false;
    }

    public void resizePointerDown()
    {
        _isResizing = true;
    }

    public void closeWindow()
    {
        _consoleWindowOpen = false;
    }
}

