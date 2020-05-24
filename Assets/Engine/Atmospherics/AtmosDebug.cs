using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{

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
            //        foreach (Tile in tilemap)
            //        {
            //            Vector3 draw = new Vector3(i, 0, j) / 1f;

            //            if (Vector3.Distance(draw, hit) < drawRadius || drawAll)
            //            {
            //                Color state;
            //                switch (atmos.GetTile(i, j).GetState())
            //                {
            //                    case TileStates.Active: state = new Color(0, 0, 0, 0); break;
            //                    case TileStates.Semiactive: state = new Color(0, 0, 0, 0.8f); break;
            //                    case TileStates.Inactive: state = new Color(0, 0, 0, 0.8f); break;
            //                    default: state = new Color(0, 0, 0, 1); break;
            //                }

            //                float pressure;

            //                if (atmos.GetTile(i, j).GetState() == TileStates.Blocked)
            //                {
            //                    Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            //                    // Draw black cube where atmos flow is blocked
            //                    Gizmos.DrawCube(new Vector3(i, 0.5f, j), new Vector3(1, 1, 1));
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
            //                                float moles = atmos.GetTile(i, j).GetGasses()[k] / 30f;

            //                                if (moles != 0f)
            //                                {
            //                                    Gizmos.color = colors[k] - state;
            //                                    Gizmos.DrawCube(new Vector3(i, moles / 2f + offset, j), new Vector3(1, moles, 1));
            //                                    offset += moles;
            //                                }
            //                            }
            //                            break;
            //                        case ViewType.Pressure:
            //                            pressure = atmos.GetTile(i, j).GetPressure() / 30f;

            //                            Gizmos.color = Color.white - state;
            //                            Gizmos.DrawCube(new Vector3(i, pressure / 2f, j), new Vector3(1, pressure, 1));
            //                            break;
            //                        case ViewType.Temperature:
            //                            float temperatue = atmos.GetTile(i, j).GetTemperature() / 100f;

            //                            Gizmos.color = Color.red - state;
            //                            Gizmos.DrawCube(new Vector3(i, temperatue / 2f, j), new Vector3(1, temperatue, 1));
            //                            break;
            //                        case ViewType.Combined:
            //                            pressure = atmos.GetTile(i, j).GetPressure() / 30f;

            //                            Gizmos.color = new Color(atmos.GetTile(i, j).GetTemperature() / 500f, 0, 0, 1) - state;
            //                            Gizmos.DrawCube(new Vector3(i, pressure / 2f, j), new Vector3(1, pressure, 1));
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
