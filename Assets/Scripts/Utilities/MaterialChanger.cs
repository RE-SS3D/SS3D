using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialChanger : MonoBehaviour
{
    public Material[] cachedMaterial;
    public MeshRenderer cachedRenderer;

    public void ChangeMaterial(MeshRenderer target, Material newMaterial)
    {
        cachedRenderer = target;
        cachedMaterial = target.materials;

        Material[] inMaterials = cachedMaterial;
        for (int i = 1; i < inMaterials.Length; i++)
        {
            inMaterials[i] = newMaterial;
        }
        cachedRenderer.materials = inMaterials;

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