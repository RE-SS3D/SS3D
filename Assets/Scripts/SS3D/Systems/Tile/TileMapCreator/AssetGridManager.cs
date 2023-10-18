using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component, that changes GridLayoutGroup contstraintCount to fit it's width
/// </summary>
[RequireComponent(typeof(GridLayoutGroup))]
public class AssetGridManager : MonoBehaviour
{
    private GridLayoutGroup _grid;
    private float cellWidth;
    private float paddingWidth;
    void Start()
    {
        _grid = GetComponent<GridLayoutGroup>();
        cellWidth = _grid.cellSize.x;
        paddingWidth = _grid.spacing.x;
    }

    // Update is called once per frame
    void Update()
    {
        float width = transform.parent.GetComponent<RectTransform>().rect.width;
        int constraintCount = Convert.ToInt32(Math.Floor(width / (cellWidth + paddingWidth)));
        if (constraintCount != _grid.constraintCount)
            _grid.constraintCount = constraintCount;
    }
}
