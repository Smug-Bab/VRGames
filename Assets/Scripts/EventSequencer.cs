using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class UniversalEventSequencer : MonoBehaviour
{
    public enum TriggerMode { Timer, Invoke, Next }

    public enum ParamType
    {
        None,
        Int,
        Float,
        String,
        Bool,
        UnityObject,
        Vector2,
        Vector3,
        Color,
        Enum,
        Json // Allows deserializing ANY custom struct or class via JSON
    }

    [Serializable]
    public class MethodTarget
    {
        public UnityEngine.Object targetObject; // GameObject or Component
        public string componentTypeName;
        public string methodName;

        // Comprehensive Parameter Storage
        public ParamType paramType = ParamType.None;
        public int intParam;
        public float floatParam;
        public string stringParam;
        public bool boolParam;
        public UnityEngine.Object objectParam;
        public Vector2 vector2Param;
        public Vector3 vector3Param;
        public Color colorParam = Color.white;
        public int enumIntValue; // Stores the underlying enum integer value
        [TextArea(2, 5)] public string jsonParam = "{}"; // For complex custom structs/classes

        // Cached reflection data for performance
        private object _cachedTarget;
        private MethodInfo _cachedMethod;
        private object[] _cachedArgs;
        private Type _targetParamType;
        private bool _isInitialized;

        public void Execute()
        {
            if (targetObject == null || string.IsNullOrEmpty(methodName)) return;

            if (!_isInitialized)
            {
                CacheReflectionData();
            }

            if (_cachedMethod != null && _cachedTarget != null)
            {
                try
                {
                    _cachedMethod.Invoke(_cachedTarget, _cachedArgs);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UniversalEventSequencer] Failed to invoke method '{methodName}' on {_cachedTarget.GetType().Name}: {e.Message}");
                }
            }
        }

        private void CacheReflectionData()
        {
            _isInitialized = true;

            // Resolve exact target component or GameObject
            if (targetObject is GameObject go)
            {
                if (componentTypeName == "GameObject")
                    _cachedTarget = go;
                else
                    _cachedTarget = go.GetComponent(componentTypeName);
            }
            else
            {
                _cachedTarget = targetObject;
            }

            if (_cachedTarget == null) return;

            Type type = _cachedTarget.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            MethodInfo[] methods = type.GetMethods(flags);

            foreach (var m in methods)
            {
                if (m.Name != methodName) continue;
                ParameterInfo[] pInfos = m.GetParameters();

                if (paramType == ParamType.None && pInfos.Length == 0)
                {
                    _cachedMethod = m;
                    _cachedArgs = null;
                    break;
                }
                else if (pInfos.Length == 1)
                {
                    _targetParamType = pInfos[0].ParameterType;
                    if (IsMatchingType(_targetParamType, paramType))
                    {
                        _cachedMethod = m;
                        _cachedArgs = new object[] { GetParamValue(_targetParamType) };
                        break;
                    }
                }
            }
        }

        private bool IsMatchingType(Type sysType, ParamType pType)
        {
            return pType switch
            {
                ParamType.Int => sysType == typeof(int) || sysType == typeof(long) || sysType == typeof(short),
                ParamType.Float => sysType == typeof(float) || sysType == typeof(double),
                ParamType.String => sysType == typeof(string),
                ParamType.Bool => sysType == typeof(bool),
                ParamType.UnityObject => typeof(UnityEngine.Object).IsAssignableFrom(sysType),
                ParamType.Vector2 => sysType == typeof(Vector2),
                ParamType.Vector3 => sysType == typeof(Vector3),
                ParamType.Color => sysType == typeof(Color),
                ParamType.Enum => sysType.IsEnum,
                ParamType.Json => true, // Can attempt deserializing into any class/struct type
                _ => false
            };
        }

        private object GetParamValue(Type requiredType)
        {
            return paramType switch
            {
                ParamType.Int => Convert.ChangeType(intParam, requiredType),
                ParamType.Float => Convert.ChangeType(floatParam, requiredType),
                ParamType.String => stringParam,
                ParamType.Bool => boolParam,
                ParamType.UnityObject => objectParam,
                ParamType.Vector2 => vector2Param,
                ParamType.Vector3 => vector3Param,
                ParamType.Color => colorParam,
                ParamType.Enum => Enum.ToObject(requiredType, enumIntValue),
                ParamType.Json => string.IsNullOrEmpty(jsonParam) ? null : JsonUtility.FromJson(jsonParam, requiredType),
                _ => null
            };
        }
    }

    [Serializable]
    public class EventStep
    {
        public string stepName = "New Event Step";
        public TriggerMode triggerMode = TriggerMode.Next;
        public float delay = 1.0f;
        public List<MethodTarget> targets = new List<MethodTarget>();
    }

    [Header("Sequence Settings")]
    public bool playOnStart = true;
    public EventStep[] events = new EventStep[0];

    private int _currentIndex = 0;
    private Coroutine _sequenceRoutine;

    private void Start()
    {
        if (playOnStart) StartSequence();
    }

    public void StartSequence()
    {
        _currentIndex = 0;
        if (_sequenceRoutine != null) StopCoroutine(_sequenceRoutine);
        _sequenceRoutine = StartCoroutine(ProcessStep(_currentIndex));
    }

    public void TriggerInvoke()
    {
        if (_currentIndex >= events.Length) return;
        if (events[_currentIndex].triggerMode == TriggerMode.Invoke)
        {
            ExecuteCurrentAndAdvance();
        }
    }

    public void JumpToStep(int index)
    {
        if (index < 0 || index >= events.Length) return;
        _currentIndex = index;
        if (_sequenceRoutine != null) StopCoroutine(_sequenceRoutine);
        _sequenceRoutine = StartCoroutine(ProcessStep(_currentIndex));
    }

    private IEnumerator ProcessStep(int index)
    {
        if (index >= events.Length) yield break;
        EventStep currentStep = events[index];

        switch (currentStep.triggerMode)
        {
            case TriggerMode.Timer:
                yield return new WaitForSeconds(currentStep.delay);
                ExecuteCurrentAndAdvance();
                break;
            case TriggerMode.Next:
                ExecuteCurrentAndAdvance();
                break;
            case TriggerMode.Invoke:
                yield break;
        }
    }

    private void ExecuteCurrentAndAdvance()
    {
        if (_currentIndex >= events.Length) return;

        foreach (var target in events[_currentIndex].targets)
        {
            target.Execute();
        }

        _currentIndex++;
        if (_currentIndex < events.Length)
        {
            _sequenceRoutine = StartCoroutine(ProcessStep(_currentIndex));
        }
    }
}
