using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding_Bakeable : MonoBehaviour {

    public float refreshSpeed;
    public PathFinding.BakeType bakeType;
    [HideInInspector]
    public List<PathFinding.Node> myNodes = new List<PathFinding.Node>();
    [SerializeField]
    private bool bake = true;

    private void Start()
    {
        if (!bake)
            return;

        if(bakeType == PathFinding.BakeType.Object)
        {
            Debug.Log("Object baking is not supported. Change baketype to something else.");
            return;
        }

        PathFinding.self.BakeObjectRealTime(this);
    }
}
