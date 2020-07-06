using System;
using System.Collections;
using System.Collections.Generic;
using SS3D.Content.Systems.Construction;
using TMPro;
using UnityEngine;

public class ConstructionEntry : MonoBehaviour
{
    public string Title
    {
        get => title.text;
        set => title.SetText(value);
    }
    
    public string Materials
    {
        get => materials.text;
        set => materials.SetText(value);
    }
    
    public event EventHandler Click;

    [SerializeField]
    private TMP_Text title;
    [SerializeField]
    private TMP_Text materials;

    public void OnClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }

    public void SetConstruction(ConstructionMaterial.ConstructionUiData data)
    {
        Title = data.name;
        Materials = data.description;
    }
}
