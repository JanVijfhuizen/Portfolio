using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

[CustomEditor(typeof(PathFinding)), ExecuteInEditMode]
public class PathFindingInspector : Editor
{
    private PathFinding pathfinding;
    private Pathfinding_Visualizer pathVisualizer;

    private void Awake()
    {
        pathfinding = (PathFinding)target;
        pathVisualizer = pathfinding.GetComponent<Pathfinding_Visualizer>();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        pathVisualizer.Repaint();
    }
}

#endif
