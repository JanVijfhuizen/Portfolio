using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR

using UnityEditor;

[CustomEditor(typeof(NeuralFlappy))]
public class FlappyCustomInspector : Editor
{
    private bool showDefaultInspector;

    public override void OnInspectorGUI()
    {
        NeuralFlappy neuralF = (NeuralFlappy)target;
        EditorGUILayout.LabelField("Flappy Nerd Script");

        showDefaultInspector = EditorGUILayout.Toggle("Show Default Inspector", showDefaultInspector);
        neuralF.fileNameSaveData = EditorGUILayout.TextField("Savedata path: ", neuralF.fileNameSaveData);

        if (showDefaultInspector)
            DrawDefaultInspector();       
    }
}

#endif
