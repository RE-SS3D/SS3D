using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class DoorAnimator : NetworkBehaviour
{
    private List<GameObject> insidePlayers = new List<GameObject>();
    [SerializeField] private float openSpeed = 3f;
    [SerializeField] private Transform leftPanel;
    [SerializeField] private Transform rightPanel;

    private Vector3 closedLeft;
    private Vector3 closedRight;

    [SerializeField] private Vector3 openLeft;
    [SerializeField] private Vector3 openRight;
    private float lerpTime;
    [SyncVar]
    private bool isOpening;

    private bool isOpeningLocal;
    [SyncVar]
    private bool isClosing;

    private bool isClosingLocal;
    [SyncVar]
    private bool isOpened;

    private int playerLayer;
    private int playersInTrigger;

    [SerializeField] private AudioSource openSfx;
    [SerializeField] private AudioSource closeSfx;
    

    void Awake()
    {
        closedLeft = leftPanel.localPosition;
        closedRight = rightPanel.localPosition;
        playerLayer = LayerMask.NameToLayer("Player");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        SyncInitialState();
    }

    void SyncInitialState()
    {
        if (isServer) return;
        if (isOpened)
        {
            leftPanel.localPosition = openLeft;
            rightPanel.localPosition = openRight;
        }
    }
    
    public void OnTriggerEnter(Collider other)
    {
        
        if (other.gameObject.layer != playerLayer || !isServer) return;
        
        if (!insidePlayers.Contains(other.gameObject))
        {
            insidePlayers.Add(other.gameObject);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != playerLayer || !isServer) return;
        
        if (insidePlayers.Contains(other.gameObject))
        {
            insidePlayers.Remove(other.gameObject);
        }
    }

    void OpenDoor()
    {
        if (isClosingLocal || isOpeningLocal) return;
        
        lerpTime = 0f;
        StartCoroutine(OpenDoorAnim());
        openSfx.Play();
    }

    void CloseDoor()
    {
        if (isClosingLocal || isOpeningLocal) return;
        
        lerpTime = 0f;
        StartCoroutine(CloseDoorAnim());
        closeSfx.Play();
    }

    IEnumerator OpenDoorAnim()
    {
        if(isServer) isOpening = true;
        isOpeningLocal = true;
        while (lerpTime < 1f)
        {
            lerpTime += Time.deltaTime * (openSpeed * 2f);

            leftPanel.localPosition = Vector3.Lerp(closedLeft, openLeft, lerpTime);
            rightPanel.localPosition = Vector3.Lerp(closedRight, openRight, lerpTime);
            yield return new WaitForEndOfFrame();
        }
        
        yield return new WaitForSeconds(1f);

        if(isServer) isOpening = false;
        isOpeningLocal = false;
        if(isServer) isOpened = true;
    }

    IEnumerator CloseDoorAnim()
    {
        if(isServer)  isClosing = true;
        isClosingLocal = true;
        
        while (lerpTime < 1f)
        {
            lerpTime += Time.deltaTime * openSpeed;

            leftPanel.localPosition = Vector3.Lerp(openLeft, closedLeft, lerpTime);
            rightPanel.localPosition = Vector3.Lerp(openRight, closedRight, lerpTime);
            yield return new WaitForEndOfFrame();
        }
        
        yield return new WaitForSeconds(1f);
        if(isServer) isClosing = false;
        isClosingLocal = false;
        if(isServer) isOpened = false;
    }

    void Update()
    {
        if (isServer)
        {
            ServerMonitorDoor();
        }
        else
        {
            ClientMonitorDoor();
        }
    }

    void ServerMonitorDoor()
    {
        if (insidePlayers.Count > 0 && !isOpening && !isOpened)
        {
            OpenDoor();
        }

        if (insidePlayers.Count == 0 && isOpened)
        {
            CloseDoor();
            isOpened = false;
        }
    }

    void ClientMonitorDoor()
    {
        if (!isOpeningLocal && isOpening)
        {
            OpenDoor();
        }

        if (!isClosingLocal && isClosing)
        {
            CloseDoor();
        }
    }
}