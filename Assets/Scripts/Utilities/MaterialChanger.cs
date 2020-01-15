using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialChanger : MonoBehaviour
{
    public Material[] cachedMaterial;
    public MeshRenderer cachedRenderer;

    //todo: make sure the main materials are here, Unity's material manipulation is not really nice.
    public  Material Palette01            = Resources.Load<Material>("Materials/Pallete01.mat");
    public  Material Palette05            = Resources.Load<Material>("Materials/Pallete05.mat");
    public  Material Palette05Emission    = Resources.Load<Material>("Materials/Pallete05 Emission.mat");

    public void ChangeMaterial(MeshRenderer target, Material newMaterial)
    {
        /* cachedRenderer = target;
        cachedMaterial = target.materials;

        Material[] inMaterials = cachedMaterial;
        for (int i = 1; i < inMaterials.Length; i++)
        {
            inMaterials[i] = newMaterial;
        }
        cachedRenderer.materials = inMaterials;*/
    }

    public void ChangeMaterialSlot(int slot, MeshRenderer target, Material newMaterial)
    {
        cachedRenderer = target;
        cachedMaterial = target.materials;

        Material[] inMaterials = cachedMaterial;

        inMaterials[slot] = newMaterial;

        cachedRenderer.materials = inMaterials;
    }
}