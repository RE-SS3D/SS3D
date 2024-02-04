﻿using UnityEngine;

public class EditorZoomArea
{
    private const float KEditorWindowTabHeight = 21.0f;
    private static Matrix4x4 PrevGuiMatrix;

    public static Rect Begin(float zoomScale, Rect screenCoordsArea)
    {
        GUI.EndGroup();        // End the group Unity begins automatically for an EditorWindow to clip out the window tab. This allows us to draw outside of the size of the EditorWindow.

        Rect clippedArea = screenCoordsArea.ScaleSizeBy(1.0f / zoomScale, screenCoordsArea.TopLeft());
        clippedArea.y += KEditorWindowTabHeight;
        GUI.BeginGroup(clippedArea);

        PrevGuiMatrix = GUI.matrix;
        Matrix4x4 translation = Matrix4x4.TRS(clippedArea.TopLeft(), Quaternion.identity, Vector3.one);
        Matrix4x4 scale = Matrix4x4.Scale(new Vector3(zoomScale, zoomScale, 1.0f));
        GUI.matrix = translation * scale * translation.inverse * GUI.matrix;

        return clippedArea;
    }

    public static void End()
    {
        GUI.matrix = PrevGuiMatrix;
        GUI.EndGroup();
        GUI.BeginGroup(new Rect(0.0f, KEditorWindowTabHeight, Screen.width, Screen.height));
    }
}