#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SS3D.Editor.Perf
{
    /// <summary>
    /// Reflection binding for Unity 6 <c>UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility</c>
    /// (no public Frame Debugger export API). Verified against Editor 6000.3.16f1.
    /// </summary>
    internal sealed class FrameDebuggerReflection
    {
        private static readonly BindingFlags StaticFlags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly Dictionary<(Type, string), FieldInfo> _fieldCache =
            new Dictionary<(Type, string), FieldInfo>();

        private readonly object[] _singleIntArg = new object[1];
        private object[] _eventDataArgs;
        private object _eventDataScratch;
        private int _getFrameEventDataParamCount = -1;

        private Type _utilType;
        private Type _eventDataType;
        private Type _eventType;
        private MethodInfo _getFrameEvents;
        private MethodInfo _getFrameEventData;
        private MethodInfo _getFrameEventInfoName;
        private MethodInfo _getFrameEventObject;
        private MethodInfo _getBatchBreakCauseStrings;
        private PropertyInfo _propCount;
        private PropertyInfo _propLimit;
        private FieldInfo _eventTypeField;
        private FieldInfo _eventObjField;
        private string[] _batchBreakCauseStrings;
        private bool _discovered;
        private string _discoverError;

        public bool IsReady => _discovered && _utilType != null && string.IsNullOrEmpty(_discoverError);

        public string DiscoverError => _discoverError;

        public Type UtilType => _utilType;

        public void EnsureDiscovered()
        {
            if (_discovered)
            {
                return;
            }

            _discovered = true;
            DiscoverTypes();
        }

        public int GetCount()
        {
            EnsureDiscovered();
            if (_propCount == null)
            {
                return 0;
            }

            try
            {
                return (int)_propCount.GetValue(null);
            }
            catch (Exception e)
            {
                _discoverError = $"Failed to read count: {e.GetBaseException().Message}";
                return 0;
            }
        }

        public int GetLimit()
        {
            EnsureDiscovered();
            if (_propLimit == null)
            {
                return 0;
            }

            try
            {
                return (int)_propLimit.GetValue(null);
            }
            catch
            {
                return 0;
            }
        }

        public bool TrySetLimit(int limit)
        {
            EnsureDiscovered();
            if (_propLimit == null || !_propLimit.CanWrite)
            {
                return false;
            }

            try
            {
                _propLimit.SetValue(null, limit);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Array GetFrameEvents()
        {
            EnsureDiscovered();
            if (_getFrameEvents == null)
            {
                return null;
            }

            try
            {
                return _getFrameEvents.Invoke(null, null) as Array;
            }
            catch (Exception e)
            {
                _discoverError = $"GetFrameEvents failed: {e.GetBaseException().Message}";
                return null;
            }
        }

        public string GetFrameEventInfoName(int index)
        {
            EnsureDiscovered();
            if (_getFrameEventInfoName == null)
            {
                return null;
            }

            try
            {
                _singleIntArg[0] = index;
                return _getFrameEventInfoName.Invoke(null, _singleIntArg) as string;
            }
            catch
            {
                return null;
            }
        }

        public UnityEngine.Object GetFrameEventObject(int index)
        {
            EnsureDiscovered();
            if (_getFrameEventObject == null)
            {
                return null;
            }

            try
            {
                _singleIntArg[0] = index;
                return _getFrameEventObject.Invoke(null, _singleIntArg) as UnityEngine.Object;
            }
            catch
            {
                return null;
            }
        }

        public bool TryGetEventTypeName(object frameEvent, out string typeName)
        {
            typeName = null;
            if (frameEvent == null || _eventTypeField == null)
            {
                return false;
            }

            try
            {
                object value = _eventTypeField.GetValue(frameEvent);
                typeName = value?.ToString();
                return !string.IsNullOrEmpty(typeName);
            }
            catch
            {
                return false;
            }
        }

        public UnityEngine.Object GetEventObjectField(object frameEvent)
        {
            if (frameEvent == null || _eventObjField == null)
            {
                return null;
            }

            try
            {
                return _eventObjField.GetValue(frameEvent) as UnityEngine.Object;
            }
            catch
            {
                return null;
            }
        }

        public string[] GetBatchBreakCauseStrings()
        {
            EnsureDiscovered();
            return _batchBreakCauseStrings;
        }

        public object GetFrameEventData(int index)
        {
            EnsureDiscovered();
            if (_getFrameEventData == null || _eventDataType == null)
            {
                return null;
            }

            try
            {
                if (_getFrameEventDataParamCount == 1)
                {
                    _singleIntArg[0] = index;
                    return _getFrameEventData.Invoke(null, _singleIntArg);
                }

                if (_getFrameEventDataParamCount >= 2 && _eventDataArgs != null)
                {
                    if (_eventDataScratch == null)
                    {
                        _eventDataScratch = Activator.CreateInstance(_eventDataType);
                    }

                    _eventDataArgs[0] = index;
                    _eventDataArgs[1] = _eventDataScratch;
                    object result = _getFrameEventData.Invoke(null, _eventDataArgs);
                    if (result is bool ok && !ok)
                    {
                        return null;
                    }

                    return _eventDataScratch;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public T GetField<T>(object obj, string fieldName, T fallback = default)
        {
            if (obj == null)
            {
                return fallback;
            }

            FieldInfo fi = ResolveField(obj.GetType(), fieldName);
            if (fi == null)
            {
                return fallback;
            }

            try
            {
                object value = fi.GetValue(obj);
                if (value == null)
                {
                    return fallback;
                }

                if (value is T typed)
                {
                    return typed;
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return fallback;
            }
        }

        public object GetFieldObject(object obj, string fieldName)
        {
            if (obj == null)
            {
                return null;
            }

            FieldInfo fi = ResolveField(obj.GetType(), fieldName);
            if (fi == null)
            {
                return null;
            }

            try
            {
                return fi.GetValue(obj);
            }
            catch
            {
                return null;
            }
        }

        public static bool IsDrawEventType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return false;
            }

            switch (typeName)
            {
                case "Mesh":
                case "InstancedMesh":
                case "SRPBatch":
                case "DynamicBatch":
                case "StaticBatch":
                case "DynamicGeometry":
                case "GLDraw":
                case "SkinOnGPU":
                case "DrawProcedural":
                case "DrawProceduralIndirect":
                case "DrawProceduralIndexed":
                case "DrawProceduralIndexedIndirect":
                case "HybridBatch":
                    return true;
                default:
                    return false;
            }
        }

        private void DiscoverTypes()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }
                catch
                {
                    continue;
                }

                if (types == null)
                {
                    continue;
                }

                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];
                    if (type?.FullName == null)
                    {
                        continue;
                    }

                    if (_utilType == null
                        && type.Name == "FrameDebuggerUtility"
                        && type.FullName.IndexOf("FrameDebugger", StringComparison.Ordinal) >= 0)
                    {
                        _utilType = type;
                    }

                    if (_eventDataType == null && type.Name == "FrameDebuggerEventData")
                    {
                        _eventDataType = type;
                    }

                    if (_eventType == null
                        && type.Name == "FrameDebuggerEvent"
                        && type.FullName.IndexOf("EventData", StringComparison.Ordinal) < 0)
                    {
                        _eventType = type;
                    }
                }
            }

            // Prefer Unity 6 namespace explicitly when present.
            Type unity6Util = Type.GetType(
                "UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility, UnityEditor");
            if (unity6Util != null)
            {
                _utilType = unity6Util;
            }

            Type unity6Data = Type.GetType(
                "UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerEventData, UnityEditor");
            if (unity6Data != null)
            {
                _eventDataType = unity6Data;
            }

            Type unity6Event = Type.GetType(
                "UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerEvent, UnityEditor");
            if (unity6Event != null)
            {
                _eventType = unity6Event;
            }

            if (_utilType == null)
            {
                _discoverError =
                    "Could not find FrameDebuggerUtility (expected UnityEditorInternal.FrameDebuggerInternal on Unity 6).";
                return;
            }

            _getFrameEvents = _utilType.GetMethod("GetFrameEvents", StaticFlags);
            _getFrameEventData = _utilType.GetMethod("GetFrameEventData", StaticFlags);
            _getFrameEventInfoName = _utilType.GetMethod("GetFrameEventInfoName", StaticFlags);
            _getFrameEventObject = _utilType.GetMethod("GetFrameEventObject", StaticFlags);
            _getBatchBreakCauseStrings = _utilType.GetMethod("GetBatchBreakCauseStrings", StaticFlags);
            _propCount = _utilType.GetProperty("count", StaticFlags);
            _propLimit = _utilType.GetProperty("limit", StaticFlags);

            if (_eventType != null)
            {
                _eventTypeField = _eventType.GetField("m_Type", InstanceFlags);
                _eventObjField = _eventType.GetField("m_Obj", InstanceFlags);
            }

            if (_getBatchBreakCauseStrings != null)
            {
                try
                {
                    _batchBreakCauseStrings = _getBatchBreakCauseStrings.Invoke(null, null) as string[];
                }
                catch
                {
                    _batchBreakCauseStrings = null;
                }
            }

            if (_getFrameEventData != null && _eventDataType != null)
            {
                _getFrameEventDataParamCount = _getFrameEventData.GetParameters().Length;
                try
                {
                    _eventDataScratch = Activator.CreateInstance(_eventDataType);
                }
                catch (Exception e)
                {
                    _discoverError = $"Could not construct FrameDebuggerEventData: {e.Message}";
                    return;
                }

                if (_getFrameEventDataParamCount >= 2)
                {
                    _eventDataArgs = new object[] { 0, _eventDataScratch };
                }
            }

            if (_getFrameEventData == null)
            {
                _discoverError = "FrameDebuggerUtility.GetFrameEventData missing.";
            }
        }

        private FieldInfo ResolveField(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            var key = (type, name);
            if (_fieldCache.TryGetValue(key, out FieldInfo cached))
            {
                return cached;
            }

            FieldInfo fi = type.GetField(name, InstanceFlags);
            if (fi == null && !name.StartsWith("m_", StringComparison.Ordinal))
            {
                fi = type.GetField("m_" + name, InstanceFlags);
            }

            if (fi == null && name.StartsWith("m_", StringComparison.Ordinal))
            {
                fi = type.GetField(name.Substring(2), InstanceFlags);
            }

            // Unity 6 ShaderInfo uses m_Keywords; older dumps used keywords.
            if (fi == null)
            {
                string alt = char.ToLowerInvariant(name[0]) + name.Substring(1);
                if (name.StartsWith("m_", StringComparison.Ordinal) && name.Length > 2)
                {
                    alt = char.ToLowerInvariant(name[2]) + name.Substring(3);
                }

                fi = type.GetField(alt, InstanceFlags);
            }

            _fieldCache[key] = fi;
            return fi;
        }
    }
}
#endif
