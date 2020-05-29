using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetalFolder
{
    public GameObject petalPrefab;
    public List<Petal> petals;
    public PetalFolder prec;
    public bool isDirty;

    public PetalFolder(GameObject prefab)
    {
        this.petals = new List<Petal>();
        this.prec = null;
        isDirty = true;
        petalPrefab = prefab;
    }

    public bool AddPetal(Petal petal)
    {
        this.petals.Add(petal);
        isDirty = true;
        return true;
    }

    public bool CheckAnimationDone()
    {
        foreach (Petal petal in petals)
        {
            if (petal.IsAnimationInProgress() == false)
                return (false);
        }
        return (true);
    }

    public void Clear()
    {
        foreach (Petal petal in petals)
        {
            UnityEngine.Object.Destroy(petal.gameObject);
        }
    }

    public void Disable()
    {
        foreach (Petal petal in petals)
        {
            petal.gameObject.SetActive(false);
        }
    }

    public void Enable()
    {
        foreach (Petal petal in petals)
        {
            petal.gameObject.SetActive(true);
        }
    }
}
