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
                atmosManager = tileManager.GetComponent<AtmosManager>();
            }

            EditorGUILayout.Space();
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            drawDebug = EditorGUILayout.Toggle("Draw debug: ", drawDebug);
            drawAll = EditorGUILayout.Toggle("Draw all: ", drawAll);
            drawRadius = EditorGUILayout.FloatField(drawRadius);
            // EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Draw View:");
            drawView = (ViewType)EditorGUILayout.EnumPopup(drawView);

            
        }

        private Vector3 GetMouse()
        {
            //Plane plane = new Plane(Vector3.up, Vector3.zero);
            //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //float distance;

            //if (plane.Raycast(ray, out distance))
            //{
            //    return ray.GetPoint(distance);
            //}

            return Vector3.down;
        }

        private void OnDrawGizmos()
        {
            //if (drawDebug)
            //{
            //    Vector3 hit = GetMouse();

            //    if (hit != Vector3.down)
            //    {
            //        // For each tile in the tilemap
            //        foreach (TileObject tile in tileManager.GetAllTiles())
            //        {
            //            // ugly hack to get coordinates
            //            string[] coords = tile.name.Split(',');
            //            int x = Int32.Parse(coords[0].Replace("[", ""));
            //            int y = Int32.Parse(coords[1].Replace("]", ""));

            //            Vector3 draw = new Vector3(x, 0, y) / 1f;

            //            if (Vector3.Distance(draw, hit) < drawRadius || drawAll)
            //            {
            //                Color state;
            //                switch (tile.atmos.GetState())
            //                {
            //                    case AtmosStates.Active: state = new Color(0, 0, 0, 0); break;
            //                    case AtmosStates.Semiactive: state = new Color(0, 0, 0, 0.8f); break;
            //                    case AtmosStates.Inactive: state = new Color(0, 0, 0, 0.8f); break;
            //                    default: state = new Color(0, 0, 0, 1); break;
            //                }

            //                float pressure;

            //                if (tile.atmos.GetState() == AtmosStates.Blocked)
            //                {
            //                    Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            //                    // Draw black cube where atmos flow is blocked
            //                    Gizmos.DrawCube(new Vector3(x, 0.5f, y), new Vector3(1, 1, 1));
            //                }
            //                else
            //                {
            //                    switch (drawView)
            //                    {
            //                        case ViewType.Content:
            //                            float[] gases = new float[5];
            //                            Color[] colors = new Color[] { Color.blue, Color.red, Color.gray, Color.magenta };

            //                            float offset = 0f;

            //                            for (int k = 0; k < 4; ++k)
            //                            {
            //                                float moles = tile.atmos.GetGasses()[k] / 30f;

            //                                if (moles != 0f)
            //                                {
            //                                    Gizmos.color = colors[k] - state;
            //                                    Gizmos.DrawCube(new Vector3(x, moles / 2f + offset, y), new Vector3(1, moles, 1));
            //                                    offset += moles;
            //                                }
            //                            }
            //                            break;
            //                        case ViewType.Pressure:
            //                            pressure = tile.atmos.GetPressure() / 30f;

            //                            Gizmos.color = Color.white - state;
            //                            Gizmos.DrawCube(new Vector3(x, pressure / 2f, y), new Vector3(1, pressure, 1));
            //                            break;
            //                        case ViewType.Temperature:
            //                            float temperatue = tile.atmos.GetTemperature() / 100f;

            //                            Gizmos.color = Color.red - state;
            //                            Gizmos.DrawCube(new Vector3(x, temperatue / 2f, y), new Vector3(1, temperatue, 1));
            //                            break;
            //                        case ViewType.Combined:
            //                            pressure = tile.atmos.GetPressure() / 30f;

            //                            Gizmos.color = new Color(tile.atmos.GetTemperature() / 500f, 0, 0, 1) - state;
            //                            Gizmos.DrawCube(new Vector3(x, pressure / 2f, y), new Vector3(1, pressure, 1));
            //                            break;
            //                    }
            //                }
            //            }
            //        }
            //    }

            //}
        }

        private TileManager tileManager;
        private AtmosManager atmosManager;

        private bool drawDebug = false;
        private bool drawAll = false;
        public float drawRadius = 3.5f;
        private enum ViewType { Content, Pressure, Temperature, Combined };
        private ViewType drawView = ViewType.Content;
    }
}
