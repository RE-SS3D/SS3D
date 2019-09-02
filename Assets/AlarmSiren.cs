using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlarmSiren : MonoBehaviour
{
    [SerializeField] bool on = false;
    [SerializeField] GameObject sirens;
    [SerializeField] float speed = 11f;
    bool moving = false;
    void Start()
    {
        
    }

    void Update()
    {
        if (!moving || on)
        {
            StartCoroutine(Emergency());
        }
        if (!on)
        {
            sirens.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        print("click");
        on = !on;
    }

    [System.Obsolete]
    private IEnumerator Emergency()
    {
        sirens.SetActive(true);
        moving = true;
        for (var time = 0f; time < 0.59f; time += Time.deltaTime)
        { 
            print("into the for");
            yield return null;


            sirens.transform.Rotate(Vector3.up, speed * Time.deltaTime);
        }
        moving = false;
    }
}
