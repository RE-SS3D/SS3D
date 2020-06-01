using SS3D.Engine.Interactions.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PetalsManager : MonoBehaviour
{
    public GameObject petalPrefab; // the petal prefab
    public GameObject petalParent; // the gameobject under which the petals should be initialized
    public RadialInteractionMenuUI contextMenu;
    public PetalFolder folder;
    int petalIndex = 0;
    
    public void Update()
    {
        if (folder != null && folder.isDirty == true && folder.CheckAnimationDone())
        {
            UpdatePetals();
            folder.isDirty = false;
        }
    }

    public void UpdatePetals()
    {
        int i = 0;

        foreach (Petal petal in folder.petals)
        {
            if (i >= petalIndex && i < petalIndex + 8) // if is active
            {
                if (petal.IsVisible()) // if was active
                {

                    int diff = ((int)(petal.transform.localEulerAngles.z + 0.5f) - ((i - petalIndex) * -45)) % 360;
                    if (diff > 180 && diff != 0)
                    {
                        petal.Rotate(false);
                    }
                    else if (diff < 180 && diff != 0)
                    {
                        petal.Rotate(true);
                    }
                }
                else
                {
                    petal.gameObject.SetActive(true);
                    petal.Appear();
                }
                petal.transform.localEulerAngles = new Vector3(0, 0, (360 + ((i - petalIndex) * -45)) % 360);
            }
            else // if is not active
            {
                if (petal.IsVisible()) // if was active but not anymore
                {
                    petal.Disappear();
                }
                else
                {
                    // don't do anything (keep petal disabled)
                }
            }
            i++;
        }
        if (contextMenu != null)
        {
            contextMenu.menuAnimator.SetBool("ReturnButtonVisible", this.folder.prec != null);
        }
    }

    public PetalFolder GetFolder()
    {
        return(folder);
    }

    public void SetFolder(PetalFolder newFolder, bool isRoot)
    {
        petalIndex = 0;
        if (isRoot && folder != null)
            this.folder.Clear();
        else if (folder != null)
            this.folder.Disable();
        newFolder.prec = (isRoot ? null : this.folder);
        this.folder = newFolder;
        this.folder.isDirty = true;
    }

    public void Return()
    {
        petalIndex = 0;
        if (folder.prec == null)
            return ;
        this.folder.Clear();
        this.folder = this.folder.prec;
        this.folder.Enable();
        this.folder.isDirty = true;
    }

    public Petal AddPetalToFolder()
    {
        Petal newPetal = Instantiate(petalPrefab, petalParent.transform).GetComponent<Petal>();
        newPetal.transform.parent = petalParent.transform;
        folder.AddPetal(newPetal);
        folder.isDirty = true;
        return (newPetal);
    }
    public Petal AddPetalToFolder(Sprite icon, string name)
    {
        Petal newPetal = Instantiate(petalPrefab, petalParent.transform).GetComponent<Petal>();
        newPetal.iconImage.texture = icon.texture;
        newPetal.name = name;
        newPetal.transform.SetParent(petalParent.transform, false);
        folder.AddPetal(newPetal);
        folder.isDirty = true;
        return (newPetal);
    }

    public void Clear()
    {
        foreach(Petal petal in folder.petals)
        {
            Destroy(petal.gameObject);
        }
        folder.petals.Clear();
        folder.isDirty = true;
    }

    public void MoveIndex(int offset)
    {
        if (folder == null)// || folder.CheckAnimationDone() == false)
            return ;
        petalIndex += offset;
        folder.isDirty = true;
        if (petalIndex < 0 || folder.petals.Count <= 8)
            petalIndex = 0;
        else if (petalIndex > folder.petals.Count - 8)
            petalIndex = folder.petals.Count - 8;
    }
}