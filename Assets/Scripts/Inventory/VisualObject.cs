using UnityEngine;

public class VisualObject : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter MeshFilter;

    public void Initialize(Mesh mesh, Material[] materials)
    {
        meshRenderer.materials = materials;
        MeshFilter.mesh = mesh;
    }
}