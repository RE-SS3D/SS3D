using UnityEngine;

public class FogColorChanger : MonoBehaviour
{
    [SerializeField]
    private Material fogMaterial;

    public Color color;

    void Update() => fogMaterial.SetVector("_Color", color);
}