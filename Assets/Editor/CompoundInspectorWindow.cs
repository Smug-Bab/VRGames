using UnityEditor;
using UnityEngine;

public class CompoundInspectorWindow : EditorWindow
{
    string formula = "SiO2";
    string output = "";
    Color tint;

    [MenuItem("Tools/Compound Inspector")]
    static void Open() => GetWindow<CompoundInspectorWindow>("Compound");

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        formula = EditorGUILayout.TextField("Formula", formula);
        if (EditorGUI.EndChangeCheck() || output == "") Refresh();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(output, output.StartsWith("ERROR") ? MessageType.Error : MessageType.None);
        if (!output.StartsWith("ERROR")) EditorGUILayout.ColorField("Tint", tint);
    }

    void Refresh()
    {
        try
        {
            var c = new Compound(formula);   // new instance: doesn't pollute the registry
            output = c.Describe(); tint = c.Tint;
        }
        catch (System.Exception e) { output = "ERROR: " + e.Message; }
    }
}
