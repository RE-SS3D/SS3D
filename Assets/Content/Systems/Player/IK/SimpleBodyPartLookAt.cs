using System;
using UnityEditor;
using UnityEngine;
public class SimpleBodyPartLookAt : MonoBehaviour
{
    [SerializeField] public Camera camera;
    [SerializeField] public Transform target;
    private Vector3 mousePos = Vector3.zero;

    [SerializeField] public Quaternion currentRot;

    [SerializeField] public float minRotationLimit;
    [SerializeField] public float maxRotationLimit;

    [Range(0.5f, 10)]
    [SerializeField] public float rotationSpeed = 5;

    [SerializeField] public Limits limits;

    [Serializable]
    public class Limits
    {
        public bool x;
        public bool y;
        public bool z;
    }

#if UNITY_EDITOR
    // This class is resposable of displaying the bools properly. Values are hardcoded for lack of better means. (Currently unused)
    //[CustomPropertyDrawer(typeof(Limits))]
    private class LimitsUIE : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            label = new GUIContent("Freeze Axis");
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var xLabelRect = new Rect(position.x, position.y, 30, position.height);
            var yLabelRect = new Rect(position.x + 65, position.y, 30, position.height);
            var zLabelRect = new Rect(position.x + 130, position.y, position.width - 90, position.height);

            var xRect = new Rect(position.x + 12, position.y, 30, position.height);
            var yRect = new Rect(position.x + 77, position.y, 50, position.height);
            var zRect = new Rect(position.x + 142, position.y, position.width - 90, position.height);

            EditorGUI.LabelField(xLabelRect, "X");
            EditorGUI.PropertyField(xRect, property.FindPropertyRelative("x"), GUIContent.none);

            EditorGUI.LabelField(yLabelRect, "Y");
            EditorGUI.PropertyField(yRect, property.FindPropertyRelative("y"), GUIContent.none);

            EditorGUI.LabelField(zLabelRect, "Z");
            EditorGUI.PropertyField(zRect, property.FindPropertyRelative("z"), GUIContent.none);

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
#endif

    private void Start()
    {
        target.position = transform.position;
        currentRot = Quaternion.identity;

        camera = CameraManager.singleton.playerCamera.GetComponent<Camera>();
    }

    public void MoveTarget()
    {
        mousePos = Vector3.Lerp(mousePos, GetMousePosition(false), Time.deltaTime * rotationSpeed);
        target.position = mousePos;
    }

    public Vector3 GetMousePosition(bool changeYAxis)
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        Vector3 mousePos = ray.origin - ray.direction * (ray.origin.y / ray.direction.y);
        mousePos = new Vector3(mousePos.x, changeYAxis ? mousePos.y : transform.position.y, mousePos.z);

        return mousePos;
    }
}