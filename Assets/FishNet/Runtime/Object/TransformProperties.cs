﻿using System.Runtime.CompilerServices;
using UnityEngine;

namespace FishNet.Object
{
    [System.Serializable]
    public class TransformPropertiesCls
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 LocalScale;

        public TransformPropertiesCls() { }
        public TransformPropertiesCls(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            Position = position;
            Rotation = rotation;
            LocalScale = localScale;
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Update(Vector3.zero, Quaternion.identity, Vector3.zero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(Transform t)
        {
            Update(t.position, t.rotation, t.localScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(TransformPropertiesCls tp)
        {
            Update(tp.Position, tp.Rotation, tp.LocalScale);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(TransformProperties tp)
        {
            Update(tp.Position, tp.Rotation, tp.LocalScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(Vector3 position, Quaternion rotation)
        {
            Update(position, rotation, LocalScale);
        }

        public void Update(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            Position = position;
            Rotation = rotation;
            LocalScale = localScale;
        }
    }

    [System.Serializable]
    public struct TransformProperties
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 LocalScale;

        public TransformProperties(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            Position = position;
            Rotation = rotation;
            LocalScale = localScale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Update(Vector3.zero, Quaternion.identity, Vector3.zero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(Transform t)
        {
            Update(t.position, t.rotation, t.localScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(TransformProperties tp)
        {
            Update(tp.Position, tp.Rotation, tp.LocalScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(Vector3 position, Quaternion rotation)
        {
            Update(position, rotation, LocalScale);
        }

        public void Update(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            Position = position;
            Rotation = rotation;
            LocalScale = localScale;
        }
    }
}

