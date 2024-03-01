// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2023 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] A custom Inspector for <see cref="Transform"/> components.</summary>
    [CustomEditor(typeof(Transform))]
    public class TransformEditorLite : TransformEditor { }
}

#endif
