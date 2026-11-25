#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UniversalEventSequencer))]
public class UniversalEventSequencerEditor : Editor
{
    private SerializedProperty _playOnStartProp;
    private SerializedProperty _eventsProp;

    private static readonly HashSet<string> IgnoredMethods = new HashSet<string>
    {
        "GetType", "ToString", "Equals", "GetHashCode", "MemberwiseClone",
        "GetInstanceID", "GetComponent", "GetComponentInChildren", "GetComponentInParent",
        "GetComponents", "GetComponentsInChildren", "GetComponentsInParent",
        "CompareTag", "SendMessage", "SendMessageUpwards", "BroadcastMessage"
    };

    private void OnEnable()
    {
        _playOnStartProp = serializedObject.FindProperty("playOnStart");
        _eventsProp = serializedObject.FindProperty("events");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(_playOnStartProp);

        if (Application.isPlaying && GUILayout.Button("Manual TriggerInvoke()"))
        {
            ((UniversalEventSequencer)target).TriggerInvoke();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Event Sequence Array", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Event Step"))
        {
            _eventsProp.InsertArrayElementAtIndex(_eventsProp.arraySize);
        }
        if (_eventsProp.arraySize > 0 && GUILayout.Button("- Remove Last"))
        {
            _eventsProp.DeleteArrayElementAtIndex(_eventsProp.arraySize - 1);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        for (int i = 0; i < _eventsProp.arraySize; i++)
        {
            DrawEventStep(_eventsProp.GetArrayElementAtIndex(i), i);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEventStep(SerializedProperty stepProp, int index)
    {
        SerializedProperty stepNameProp = stepProp.FindPropertyRelative("stepName");
        SerializedProperty triggerModeProp = stepProp.FindPropertyRelative("triggerMode");
        SerializedProperty delayProp = stepProp.FindPropertyRelative("delay");
        SerializedProperty targetsProp = stepProp.FindPropertyRelative("targets");

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        stepNameProp.stringValue = EditorGUILayout.TextField($"[{index}] Name", stepNameProp.stringValue);
        if (GUILayout.Button("X", GUILayout.Width(22)))
        {
            _eventsProp.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(triggerModeProp, new GUIContent("Trigger Mode"));

        if ((UniversalEventSequencer.TriggerMode)triggerModeProp.enumValueIndex == UniversalEventSequencer.TriggerMode.Timer)
        {
            EditorGUILayout.PropertyField(delayProp, new GUIContent("Wait Seconds"));
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Target Objects & Methods", EditorStyles.miniBoldLabel);

        for (int t = 0; t < targetsProp.arraySize; t++)
        {
            DrawMethodTarget(targetsProp, t);
        }

        if (GUILayout.Button("+ Add Target Call", GUILayout.Height(20)))
        {
            targetsProp.InsertArrayElementAtIndex(targetsProp.arraySize);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawMethodTarget(SerializedProperty listProp, int index)
    {
        SerializedProperty targetProp = listProp.GetArrayElementAtIndex(index);
        SerializedProperty objProp = targetProp.FindPropertyRelative("targetObject");
        SerializedProperty compNameProp = targetProp.FindPropertyRelative("componentTypeName");
        SerializedProperty methodNameProp = targetProp.FindPropertyRelative("methodName");
        SerializedProperty paramTypeProp = targetProp.FindPropertyRelative("paramType");

        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(objProp, GUIContent.none);
        if (EditorGUI.EndChangeCheck())
        {
            compNameProp.stringValue = "";
            methodNameProp.stringValue = "";
            paramTypeProp.enumValueIndex = (int)UniversalEventSequencer.ParamType.None;
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            listProp.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        UnityEngine.Object targetObj = objProp.objectReferenceValue;
        if (targetObj != null)
        {
            List<MethodEntry> availableMethods = GetCompatibleMethods(targetObj);
            string[] displayNames = availableMethods.Select(m => m.DisplayName).ToArray();

            int currentSelection = availableMethods.FindIndex(m =>
            m.ComponentName == compNameProp.stringValue &&
            m.MethodName == methodNameProp.stringValue &&
            m.ParamType == (UniversalEventSequencer.ParamType)paramTypeProp.enumValueIndex);

            if (currentSelection < 0) currentSelection = 0;

            EditorGUI.BeginChangeCheck();
            int newSelection = EditorGUILayout.Popup("Method", currentSelection, displayNames);
            if (EditorGUI.EndChangeCheck() || string.IsNullOrEmpty(methodNameProp.stringValue))
            {
                if (availableMethods.Count > 0)
                {
                    MethodEntry selected = availableMethods[newSelection];
                    compNameProp.stringValue = selected.ComponentName;
                    methodNameProp.stringValue = selected.MethodName;
                    paramTypeProp.enumValueIndex = (int)selected.ParamType;
                }
            }

            // Draw dynamic Parameter field corresponding to ANY data type selected
            UniversalEventSequencer.ParamType activeParam = (UniversalEventSequencer.ParamType)paramTypeProp.enumValueIndex;
            switch (activeParam)
            {
                case UniversalEventSequencer.ParamType.Int:
                    EditorGUILayout.PropertyField(targetProp.FindPropertyRelative("intParam"), new GUIContent("Int Value"));
                    break;
                case UniversalEventSequencer.ParamType.Float:
                    EditorGUILayout.PropertyField(targetProp.FindPropertyRelative("floatParam"), new GUIContent("Float Value"));
                    break;
                case UniversalEventSequencer.ParamType.String:
                    EditorGUILayout.PropertyField(targetProp.FindPropertyRelative("stringParam"), new GUIContent("String Value"));
                    break;
                case UniversalEventSequencer.ParamType.Bool:
                    EditorGUILayout.PropertyField(targetProp.FindPropertyRelative("boolParam"), new GUIContent("Bool Value"));
                    break;
                case UniversalEventSequencer.ParamType.UnityObject:
                    EditorGUILayout.PropertyField(targetProp.FindPropertyRelative("objectParam"), new GUIContent("Object Value"));
                    break;
                case UniversalEventSequencer.ParamType.Vector2:
                    EditorGUILayout.PropertyField(targetProp.FindPropertyRelative("vector2Param"), new GUIContent("Vector2 Value"));
                    break;
                case UniversalEventSequencer.ParamType.Vector3:
                    EditorGUILayout.PropertyField(targetProp.FindPropertyRelative("vector3Param"), new GUIContent("Vector3 Value"));
                    break;
                case UniversalEventSequencer.ParamType.Color:
                    EditorGUILayout.PropertyField(targetProp.FindPropertyRelative("colorParam"), new GUIContent("Color Value"));
                    break;
                case UniversalEventSequencer.ParamType.Enum:
                    EditorGUILayout.PropertyField(targetProp.FindPropertyRelative("enumIntValue"), new GUIContent("Enum (Int Index)"));
                    break;
                case UniversalEventSequencer.ParamType.Json:
                    EditorGUILayout.PropertyField(targetProp.FindPropertyRelative("jsonParam"), new GUIContent("JSON Struct Payload"));
                    break;
            }
        }

        EditorGUILayout.EndVertical();
    }

    private struct MethodEntry
    {
        public string DisplayName;
        public string ComponentName;
        public string MethodName;
        public UniversalEventSequencer.ParamType ParamType;
    }

    private List<MethodEntry> GetCompatibleMethods(UnityEngine.Object targetObj)
    {
        List<MethodEntry> entries = new List<MethodEntry>
        {
            new MethodEntry { DisplayName = "No Function", ComponentName = "", MethodName = "", ParamType = UniversalEventSequencer.ParamType.None }
        };

        List<object> targetsToInspect = new List<object> { targetObj };
        if (targetObj is GameObject go)
        {
            targetsToInspect.AddRange(go.GetComponents<Component>().Where(c => c != null));
        }

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        foreach (var item in targetsToInspect)
        {
            Type type = item.GetType();
            string compName = type.Name;

            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.IsSpecialName || IgnoredMethods.Contains(method.Name)) continue;
                if (method.DeclaringType == typeof(object)) continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 1) continue; // Single parameter or 0 parameters supported

                UniversalEventSequencer.ParamType pType = UniversalEventSequencer.ParamType.None;
                string paramLabel = "()";

                if (parameters.Length == 1)
                {
                    Type pt = parameters[0].ParameterType;

                    if (pt == typeof(int) || pt == typeof(long) || pt == typeof(short)) { pType = UniversalEventSequencer.ParamType.Int; paramLabel = "(int)"; }
                    else if (pt == typeof(float) || pt == typeof(double)) { pType = UniversalEventSequencer.ParamType.Float; paramLabel = "(float)"; }
                    else if (pt == typeof(string)) { pType = UniversalEventSequencer.ParamType.String; paramLabel = "(string)"; }
                    else if (pt == typeof(bool)) { pType = UniversalEventSequencer.ParamType.Bool; paramLabel = "(bool)"; }
                    else if (typeof(UnityEngine.Object).IsAssignableFrom(pt)) { pType = UniversalEventSequencer.ParamType.UnityObject; paramLabel = $"({pt.Name})"; }
                    else if (pt == typeof(Vector2)) { pType = UniversalEventSequencer.ParamType.Vector2; paramLabel = "(Vector2)"; }
                    else if (pt == typeof(Vector3)) { pType = UniversalEventSequencer.ParamType.Vector3; paramLabel = "(Vector3)"; }
                    else if (pt == typeof(Color)) { pType = UniversalEventSequencer.ParamType.Color; paramLabel = "(Color)"; }
                    else if (pt.IsEnum) { pType = UniversalEventSequencer.ParamType.Enum; paramLabel = $"({pt.Name} Enum)"; }
                    else
                    {
                        // Fallback: Catch-all for ANY custom class or struct via JSON deserialization!
                        pType = UniversalEventSequencer.ParamType.Json;
                        paramLabel = $"(JSON: {pt.Name})";
                    }
                }

                string accessPrefix = method.IsPublic ? "" : "[Private] ";

                entries.Add(new MethodEntry
                {
                    DisplayName = $"{compName}/{accessPrefix}{method.Name} {paramLabel}",
                    ComponentName = compName,
                    MethodName = method.Name,
                    ParamType = pType
                });
            }
        }

        return entries;
    }
}
#endif
