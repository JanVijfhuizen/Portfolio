using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding_Bakeable : MonoBehaviour {

    public float refreshSpeed;
    public PathFinding.BakeType bakeType;
    [HideInInspector]
    public List<PathFinding.Node> myNodes = new List<PathFinding.Node>();
    [HideInInspector]
    public List<PathFinding.Node> oldNodes = new List<PathFinding.Node>();

    private void Start()
    {
        if(bakeType == PathFinding.BakeType.Object)
        {
            Debug.Log("Object baking is not supported. Change baketype to something else.");
            return;
        }

        PathFinding.self.BakeObjectRealTime(this);
    }
}
