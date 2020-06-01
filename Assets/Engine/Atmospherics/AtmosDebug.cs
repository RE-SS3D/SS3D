using SS3D.Engine.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    // TODO, move to editors
    public class AtmosDebug : EditorWindow
    {

        [MenuItem("RE:SS3D Editor Tools/Atmospherics Editor")]
        public static void ShowWindow()
        {
            GUIContent gUIContent = new GUIContent();
            gUIContent.text = "Atmospherics";
            GetWindow(typeof(AtmosDebug)).titleContent = gUIContent;
            GetWindow(typeof(AtmosDebug)).Show();

        }

        public void OnEnable()
        {
            // Get Tile manager and Atmos manager
            tileManager = FindObjectOfType<TileManager>();
            atmosManager = tileManager.GetComponent<AtmosManager>();

        }

        public void OnGUI()
        {
            if (tileManager == null)
            {
                tileManager = FindObjectOfType<TileManager>();
                if (tileManager == null)
                {
                    Debug.LogWarning("Editor opened while in runmode");
                    return;
                }
                atmosManager = tileManager.GetComponent<AtmosManager>();
            }

            EditorGUILayout.Space();
            GUILayout.Label("View Settings", EditorStyles.boldLabel);
            drawDebug = EditorGUILayout.BeginToggleGroup("Draw debug:", drawDebug);
            drawAll = EditorGUILayout.Toggle("Draw invisible: ", drawAll);
            drawWall = EditorGUILayout.Toggle("Draw walls: ", drawWall);
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();
            showMessages = EditorGUILayout.Toggle("Show messages: ", showMessages);

            EditorGUILayout.Space();
            EditorGUILayout.PrefixLabel("Draw View:");
            drawView = (AtmosManager.ViewType)EditorGUILayout.EnumPopup(drawView);

            EditorGUILayout.Space();
            EditorGUILayout.PrefixLabel("Update rate:");
            updateRate = EditorGUILayout.FloatField(updateRate);

            EditorGUILayout.Space();
            EditorGUILayout.PrefixLabel("Insert gas:");
            gassSelection = (AtmosGasses)EditorGUILayout.EnumPopup(gassSelection);
            if (GUILayout.Button("Add gas"))
            {
                Debug.Log("Click to add gas. Press escape to stop");
                atmosManager.isAddingGas = true;
            }

            atmosManager.drawDebug = drawDebug;
            atmosManager.drawAll = drawAll;
            atmosManager.drawWall = drawWall;

            atmosManager.showMessages = showMessages;
            atmosManager.SetViewType(drawView);
            atmosManager.SetUpdateRate(updateRate);
            atmosManager.SetAddGas(gassSelection);

            // HandleUtility.Repaint();
        }

        private TileManager tileManager;
        private AtmosManager atmosManager;

        private bool drawDebug = true;
        private bool drawAll = true;
        private bool drawWall = true;

        private bool showMessages = false;
        private float updateRate = 0f;

        private AtmosManager.ViewType drawView;
        private AtmosGasses gassSelection;
    }
}
