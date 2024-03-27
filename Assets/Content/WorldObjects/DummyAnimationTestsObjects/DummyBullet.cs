using Coimbra;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyBullet : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DestroyItself());
    }

    private IEnumerator DestroyItself()
    {
        yield return new WaitForSeconds(5f);
        gameObject.Dispose(true);
    }
}
