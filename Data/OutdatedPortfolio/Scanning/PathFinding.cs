using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Pathfinding_Visualizer))]
public class PathFinding : MonoBehaviour {

    //make a 3d grid (visually in the editor) from the object this stands on

    //get all static objects in scene

    //reference
    public static PathFinding self;
    public static Pathfinding_Visualizer visualizer;

    public int widthSize, heightSize;
    public float widthSizeNode, heightSizeNode;

    public Node[,,] grid; //moet dit wel omzetten in custom class met lists van list, sinds geen serializing
    [HideInInspector]
    public List<GameObject> bakeable;

    public bool visualize = true;
    public bool visualizeNodes = false;
    [SerializeField]
    private bool visualizeRaycasts = false;

    public delegate void _Repaint();
    public static _Repaint repaint;

    private void Awake()
    {
        visualizer = GetComponent<Pathfinding_Visualizer>();
        Bake();
    }

    private void Update()
    {
        if (repaint != null)
            repaint();
    }

    public void Bake() //1 grote fout: de bake bugt de fuck out als de objecten aan de bovenkant komen
    {
        StopAllCoroutines();
        #region Prepare Bake
        //initialize reference to self
        self = this;

        //initialize grid
        grid = new Node[widthSize, heightSize, widthSize];
        for (int x = 0; x < widthSize; x++)
            for (int y = 0; y < heightSize; y++)
                for (int z = 0; z < widthSize; z++)
                    grid[x, y, z] = new Node(x, y, z);

        //reset pathfindable
        bakeable = new List<GameObject>();

        //get all gameobjects
        GameObject[] all = FindObjectsOfType(typeof(GameObject)) as GameObject[];
        foreach (GameObject possibleBakeObj in all)
        {
            if (possibleBakeObj.isStatic)
                bakeable.Add(possibleBakeObj);
        }

        SetMidsAndBoundary(); //this is the node where this transform now is, neccessity for getnodefromvector
        #endregion

        BakePreparedScene(); //now bake all objects in the 3d array
    }

    private void BakePreparedScene()
    {
        foreach (GameObject bakeObject in bakeable)
            BakeObject(bakeObject, BakeType.Object);
    }

    public enum BakeType {Object, Enemy, Movable, Walkable }
    private List<Node> BakeObject(GameObject bakeable, BakeType type) //hier zit HET PROBLEEM
    {
        List<Node> ret = new List<Node>();
        //get size collider
        Collider c = bakeable.GetComponent<Collider>();
        Vector3 size = c.bounds.size;

        //get mid size bakeable
        Node midNode = GetNodeFromVector(bakeable.transform.position);

        int _x = (int)(size.x / widthSizeNode);
        int _y = (int)(size.y / heightSizeNode);
        int _z = (int)(size.z / widthSizeNode);
        int halfX = _x / 2;
        int halfY = _y / 2;
        int halfZ = _z / 2;
        
        Collider[] hits;
        Node node;

        if (!(midNode != null))
        {
            Debug.Log(bakeable.name + " not scanned, the center has to be in the bakeable area.");
            return null;
        }

        
        #region Old Code
        for (int x = midNode.x - halfX; x <= halfX + midNode.x; x++) //dit is dus verkeerd, somehow
            for (int y = midNode.y - halfY; y <= halfY + midNode.y + 1; y++) //eentje hoger erbij doen
                for (int z = midNode.z - halfZ; z <= halfZ + midNode.z; z++)
                {
                    //check if in bounds
                    if (x < 0 || x >= grid.GetLength(0))
                        continue;
                    if (y < 0 || y >= grid.GetLength(1))
                        continue;
                    if (z < 0 || z >= grid.GetLength(2))
                        continue;

                    node = grid[x, y, z];
                    if (node.filled)
                        continue;

                    Vector3 midPos = GetVectorFromNode(grid[x, y, z]);
                    hits = Physics.OverlapSphere(midPos, heightSizeNode / 4);
                    if (visualizeRaycasts)
                    {
                        Color v = hits.Length > 0 ? Color.red : Color.grey;
                        Debug.DrawRay(midPos, Vector3.down, v, 1);
                    }
                    foreach (Collider hit in hits)
                    {
                        if (bakeable != hit.transform.gameObject)
                            continue;
                        node.bakeType = type;
                        node.filled = true;
                        ret.Add(node);
                        break;
                    }
                }
        
        #endregion
        
        return ret;
    }

    private List<Coroutine> realtimeBake = new List<Coroutine>();
    public void BakeObjectRealTime(Pathfinding_Bakeable bakeable)
    {
        realtimeBake.Add(StartCoroutine(_BakeObjectRealTime(bakeable)));
    }

    private IEnumerator _BakeObjectRealTime(Pathfinding_Bakeable bakeable)
    {
        GameObject g = bakeable.gameObject;
        bakeable.myNodes = BakeObject(g, bakeable.bakeType);

        while (true)
        {
            //reset old nodes
            bakeable.oldNodes = bakeable.myNodes;
            if(bakeable.oldNodes != null)
                foreach(Node oldNode in bakeable.oldNodes)
                    oldNode.filled = false;
            bakeable.myNodes = BakeObject(g, bakeable.bakeType);
            yield return new WaitForSeconds(bakeable.refreshSpeed);
        }
    }

    //set mids to calculate from
    private int midW;
    private int midH;
    private Vector3 boundary;
    private void SetMidsAndBoundary()
    {
        midW = grid.GetLength(0) / 2;
        midH = grid.GetLength(1) / 2;
        
        GetBoundary();
    }

    public Vector3 GetBoundary()
    {
        boundary = transform.position;

        //boundary is left bottom
        boundary.x -= widthSize * widthSizeNode / 2;
        boundary.y -= heightSize * heightSizeNode / 2;
        boundary.z -= widthSize * widthSizeNode / 2;
        return boundary;
    }
    //<3
    public Vector3 GetVectorFromNode(Node node)
    {
        //calc from boundary
        Vector3 pos = boundary;
        pos.x += widthSizeNode * node.x;
        pos.y += heightSizeNode * node.y;
        pos.z += widthSizeNode * node.z;
        return pos;
    }

    public Vector3 GetSizeSquire()
    {
        return new Vector3(
            GetFullX(),
            GetFullY(),
            GetFullX());
    }

    public float GetFullX()
    {
        return widthSize * widthSizeNode;
    }

    public float GetFullY()
    {
        return heightSize * heightSizeNode;
    }

    //this is what you publicly access
    public Node GetNodeFromVector(Vector3 pos)
    {
        Vector3 difference = pos - transform.position;

        int x = midW + Mathf.FloorToInt(difference.x / widthSizeNode);
        int y = midH + Mathf.FloorToInt(difference.y / heightSizeNode);
        int z = midW + Mathf.FloorToInt(difference.z / widthSizeNode);

        //debug checks, it makes sure to return null when outside the grid
        if (x >= grid.GetLength(0) || x < 0)
            return null;
        if (y >= grid.GetLength(1) || y < 0)
            return null;
        if (z >= grid.GetLength(2) || z < 0)
            return null;
        return grid[x, y, z];
    }

    [Serializable]
    public class Node
    {
        public bool filled; //true when object is in node
        public float height; //different to center height of average object in space, create max height difference, so something like -2.5f or something for instance
        public int x, y, z; //pos
        public BakeType bakeType;

        public Node(int _x, int _y, int _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }
    }
}
