using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AirlockManager : MonoBehaviour
{

    [Header("Type")]
    [SerializeField] Types type;

    enum Types { regular, dual };

    MaterialChanger changer = null;

    //MaterialChanger changer = 
    // 0 for green
    // 1 for red
    // 2 for off

    [Header("Sounds")] // TODO move these to some global settings somewhere
    [SerializeField] private AudioClip openSound = null;
    [SerializeField] private AudioClip closeSound = null;

    [Header("Doors")]
    [SerializeField] private Transform left = null;
    [SerializeField] private Transform right = null;

    [Header("Access")]
    [SerializeField] string idTag;

    [Header("Power")]
    [SerializeField] bool powered;

    [Header("Lights")]
    [SerializeField] bool lightOn;
    [SerializeField] MeshRenderer[] lights;
    [SerializeField] Material[] lightsMaterials;

    [Header("States")]
    [SerializeField] private bool open = false;
    [SerializeField] private bool moving = false;
    [SerializeField] private bool welded = false;
    [SerializeField] private bool bolted = false;
    [SerializeField] private bool disabled = false;
    [SerializeField] private bool autoClose = true;

    [Header("Animation Settings")]
    [SerializeField] private Vector3 openAxis = Vector3.right;
    [SerializeField] private float openDistance = 1.0f;
    [SerializeField] private float openTime = 1.0f;

    [SerializeField] private float timeLeft = 0.0f;
    [SerializeField] private float target = 0.0f;
    [SerializeField] private float origin = 0.0f;

    [SerializeField] private float targetOpen = 0.45f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField]  private AudioSource audioSource;

    public bool Stuck => welded || bolted || disabled ; //add electrical.off here
    
    void Start()
    {
        changer = gameObject.AddComponent(typeof (MaterialChanger)) as MaterialChanger;
    }

    void Update()
    {
        if (!moving)
        {
            changer.ChangeMaterial(lights[0], lightsMaterials[2]);
            changer.ChangeMaterial(lights[1], lightsMaterials[2]);
        }
    }


    private void OnMouseOver()
    {
        if (!moving)
        {
            if (Input.GetMouseButtonDown(0))
            {
                AuthorizeAccess(idTag);
            }
            if (Input.GetMouseButtonDown(1))
            {
                AuthorizeAccess("nope");
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        ToggleOpen(true);
    }

    private void OnTriggerExit(Collider other)
    {
        ToggleOpen(false);
    }

    public void ToggleOpen(bool enter)
    {
        //Enter is true if enter, false if exiting
        if (enter != open)
        {
            //If the door is already moving, change direction manually
            if (moving)
            {
                origin = open ? targetOpen : 0f;
                target = open ? 0f : targetOpen;

                timeLeft = openTime - timeLeft;

                audioSource.clip = open ? openSound : closeSound;
                audioSource.time = timeLeft;
                audioSource.Play();

                open = open ? false : true;
            }
            else
            {
                //If not moving, start
                StartCoroutine(Move());
            }
        }
    }

    [SerializeField]
    public IEnumerator Move() // TODO make a more performant, declarative manager or these simple animations
    {
        moving = true;
        //print("moving true");
        origin = open ? targetOpen : 0f;
        target = open ? 0f : targetOpen;

        timeLeft = openTime;

        audioSource.clip = open ? openSound : closeSound;
        audioSource.time = timeLeft;
        audioSource.Play();

        open = open ? false : true;

        //We now run until time is up. ToggleOpen can increase timeLeft before it runs out should this loop already be running
        for (; timeLeft > 0; timeLeft -= Time.deltaTime)
        {
            //print("into the for");
            yield return null;

            var t = Mathf.Lerp(origin, target, (openTime - timeLeft) / openTime);

            left.transform.localPosition = openAxis * openDistance * openCurve.Evaluate(t);
            right.transform.localPosition = -openAxis * openDistance * openCurve.Evaluate(t);
        }
        //print("out of the for");
        left.transform.localPosition = openAxis * openDistance * openCurve.Evaluate(target);
        right.transform.localPosition = -openAxis * openDistance * openCurve.Evaluate(target);
        //print("moving false");

        moving = false;
    }

    public bool AuthorizeAccess(string inIdTag)
    {
        if (inIdTag == idTag)
        {
            changer.ChangeMaterial(lights[0], lightsMaterials[0]);
            changer.ChangeMaterial(lights[1], lightsMaterials[0]);
            StartCoroutine(Move());
            return true;
        }
        else
        {
            changer.ChangeMaterial(lights[0], lightsMaterials[1]);
            changer.ChangeMaterial(lights[1], lightsMaterials[1]);
            return false;    
        }
    }
   
}
