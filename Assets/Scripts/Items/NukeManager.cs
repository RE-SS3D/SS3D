using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NukeManager : MonoBehaviour
{
    [SerializeField]
    private GameObject disk;
    [SerializeField]
    private bool hasDisk;

    void Update()
    {
        disk.transform.localScale = hasDisk ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0); // verifies hasDisk and then choose which transform will be
    }
}
