using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Tiles.Connections
{
    /// <summary>
    /// Struct for storing which mesh and rotation to use. Used by the adjency connectors.
    /// </summary>
    public struct MeshDirectionInfo
    {
        public Mesh mesh;
        public float rotation;
    }
}