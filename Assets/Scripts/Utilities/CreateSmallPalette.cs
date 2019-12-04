using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CreateSmallPalette : MonoBehaviour
{
    public Texture2D txtrPalette;
    public int k;
    public Renderer mtm;
    public Texture2D newPalette;
    public enum WTexture { oldone, newone };
    public WTexture wTexture;
    public List<Color> meshColors;
    public List<Color> textureColors;




    [ContextMenu("start some function")]
    void SomeFunction()
    {
        newPalette = new Texture2D(k, k, TextureFormat.RGBA32, false);
        newPalette.filterMode = FilterMode.Point;

        meshColors.Clear();

        for (int i = k - 1; i >= 0; i--)
        {
            for (int j = k - 1; j >= 0; j--)
            {
                var tmpColor = txtrPalette.GetPixel(i * 16 - 8, j * 16 - 8);
                newPalette.SetPixel(i - 1, j - 1, tmpColor);
            }
        }



        newPalette.Apply();

        //byte[] bytes = newPalette.EncodeToPNG();
        //File.WriteAllBytes(Application.dataPath + "/../SavedScreen.png", bytes);

        mtm.sharedMaterial.SetTexture("_BaseMap", wTexture == WTexture.oldone ? txtrPalette : newPalette);
        var tmpMesh = mtm.gameObject.GetComponent<MeshFilter>().sharedMesh;
        var r = mtm.sharedMaterial.GetTexture("_BaseMap") as Texture2D;

        textureColors.Clear();
        for (int i = 0; i <= r.width; i++)
        {
            for (int j = 0; j <= r.height; j++)
            {
                var tmpColor = r.GetPixel(i, j);
                textureColors.Add(tmpColor);
            }
        }
        textureColors = new HashSet<Color>(textureColors).ToList();

        foreach (Vector2 t in tmpMesh.uv)
        {

            meshColors.Add(r.GetPixel((int)(t.x * r.width), (int)(t.y * r.height)));
            //meshColors.Add(r.GetPixel(Mathf.RoundToInt(t.x * r.width), Mathf.RoundToInt(t.y * r.height)));
        }
        var e = new HashSet<Color>(meshColors);
        meshColors.Clear();
        meshColors = e.ToList();
    }
}