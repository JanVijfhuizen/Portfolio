using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Pathfinding_Bakeable))]
public class MoveManager : MonoBehaviour {
    public delegate void Callable(List<Vector3> list);

    private Pathfinding_Bakeable bake;
    private PathFinding p;
    [SerializeField]
    private int maxNodesBetweenGroundAndTarget = 5;
    [SerializeField]
    private bool _2d;
    [SerializeField]
    private int checksPerFrame = 10;
    [SerializeField, Tooltip("Do not set this too low, It wont follow you if it can't get close.")]
    private int stoppingNodeDistance = 5;

    public Transform goal;

    private void Start()
    {
        p = PathFinding.self;
        bake = GetComponent<Pathfinding_Bakeable>();
        MoveTowards(goal.position, null);
    }

	public bool MoveTowards(Vector3 position, Callable callable)
    {
        if (!(PathFinding.self != null))
            return false;
        StopAllCoroutines();
        StartCoroutine(CalculatePath(position, callable));
        return true;
    }

    private class Node : IComparable<Node>
    {
        public PathFinding.Node node;
        public Node parent;
        public int cost;

        public Node(PathFinding.Node _node)
        {
            node = _node;
        }

        public Node(PathFinding.Node _node, Node _parent, int _cost)
        {
            node = _node;
            parent = _parent;
            cost = _cost;
        }

        public int CompareTo(Node other)
        {
            if (other == null)
                return 1;
            return cost - other.cost;
        }
    }

    private Vector3 dest;
    private Vector3 origin;
    private Vector3 _position;
    private int cost;
    private float distanceOrigin;
    private float distanceGoal;
    private List<Node> adjecentNodes;
    private Node nodeToCheck;
    private PathFinding.Node n;
    private List<Node> open;
    private List<PathFinding.Node> closed;
    private int lengthX, lengthY, lengthZ;

    //bugs
    //when out of bounds, will travel to last known destination
    //when out of range, will repeat same checks and slow down game
    //bake is raar met specifieke objecten
    //z werkt niet goed met pathfinding

    private IEnumerator CalculatePath(Vector3 position, Callable callable) //omhoog / omlaag moet ook werken. hiervoor moet schuin gaan kunnen
    {
        PathFinding.Node destination = p.GetNodeFromVector(position);

        //array information
        lengthX = p.grid.GetLength(0) - 1;
        lengthY = p.grid.GetLength(1) - 1;
        lengthZ = p.grid.GetLength(2) - 1;

        //pathfinding tools
        open = new List<Node>();
        closed = new List<PathFinding.Node>();

        Pathfinding_Visualizer pV = PathFinding.visualizer;

        PathFinding.Node start = p.GetNodeFromVector(transform.position);
        int nodesBetween = 0;
        if (start != null)
        {
            while(nodesBetween < maxNodesBetweenGroundAndTarget)
            {
                if (start.filled && start.bakeType == PathFinding.BakeType.Object)
                    break;
                nodesBetween++;
                if (start.y > 0)
                    start = p.grid[start.x, start.y - 1, start.z];
                else break;
            }
            if (start.filled)
                open.Add(new Node(start));
            else
                Debug.Log("There is nothing walkable around the start point.");
        }
        else
            Debug.Log("Currently not in a walkable area, unable to create a path.");

        nodesBetween = 0;
        if(destination != null)
        {
            while (nodesBetween < maxNodesBetweenGroundAndTarget)
            {
                if (destination.filled && destination.bakeType == PathFinding.BakeType.Object)
                    break;
                nodesBetween++;
                if (destination.y > 0)
                    destination = p.grid[destination.x, destination.y - 1, destination.z];
                else break;
            }
            if (!destination.filled)
            {
                Debug.Log("There is nothing walkable around the destination.");
                open.Clear();
            }
        } 

        //cost calculation 
        if(start != null)
            origin = new Vector3(start.x, start.y, start.z);
        if(destination != null)
            dest = new Vector3(destination.x, destination.y, destination.z);

        int checks = 0;
        while (open.Count > 0) //hij gaat nu via het gevulde een pad zoeken ipv bovenop het pad
        {
            open.Sort();
            PathFinding.Node node;
            if (p.visualize)
                if (open.Count > 0)
                {
                    node = open[0].node;
                    //in dit geval weet je al dat +1 bestaat omdat dat een check gaat worden
                    pV.ChangeColorGridPart(p.grid[node.x, node.y + 1, node.z], Color.green);
                }

            checks++;
            if(checks >= checksPerFrame)
            {
                checks = 0;
                yield return null;
            }

            adjecentNodes = new List<Node>();
            
            nodeToCheck = open[0];
            n = nodeToCheck.node;

            //check if destination
            if (Vector3.Distance(dest, new Vector3(n.x, n.y, n.z)) <= stoppingNodeDistance)
                break;

            open.RemoveAt(0);
            closed.Add(nodeToCheck.node);

            //front
            PrepareNode(n.x, n.y, n.z + 1);
            //back
            PrepareNode(n.x, n.y, n.z - 1);
            //right
            PrepareNode(n.x + 1, n.y, n.z);
            //left
            PrepareNode(n.x - 1, n.y, n.z);

            if (!_2d)
            {
                //top checks
                PrepareNode(n.x, n.y + 1, n.z + 1);
                PrepareNode(n.x + 1, n.y + 1, n.z + 1);
                PrepareNode(n.x + 1, n.y + 1, n.z);
                PrepareNode(n.x + 1, n.y + 1, n.z - 1);
                PrepareNode(n.x, n.y + 1, n.z - 1);
                PrepareNode(n.x - 1, n.y + 1, n.z - 1);
                PrepareNode(n.x - 1, n.y + 1, n.z);
                PrepareNode(n.x - 1, n.y + 1, n.z + 1);

                //bottom checks
                PrepareNode(n.x, n.y - 1, n.z + 1);
                PrepareNode(n.x + 1, n.y - 1, n.z + 1);
                PrepareNode(n.x + 1, n.y - 1, n.z);
                PrepareNode(n.x + 1, n.y - 1, n.z - 1);
                PrepareNode(n.x, n.y - 1, n.z - 1);
                PrepareNode(n.x - 1, n.y - 1, n.z - 1);
                PrepareNode(n.x - 1, n.y - 1, n.z);
                PrepareNode(n.x - 1, n.y - 1, n.z + 1);
            }

            foreach (Node _node in adjecentNodes)
                open.Add(_node);
        }

        //check if path has been found
        //if curnode != null
        //if curnode == destination
        
        //convert everything to a vector3 list
        List<Vector3> _path = new List<Vector3>();
        Node curNode;
        Vector3 pos;
        if (open.Count > 0) {
            curNode = open[0];
            open.RemoveAt(0);
            while (curNode.parent != null)
            {
                //convert
                pos = p.GetVectorFromNode(curNode.node);
                pos.y += p.heightSizeNode;
                pV.ChangeColorGridPart(curNode.node, Color.red);
                _path.Add(pos);
                curNode = curNode.parent;
            }
            _path.Add(p.GetVectorFromNode(curNode.node));
        }

        if(_path.Count == 0)
            Debug.Log("No path could be found.");
        if(callable != null)
            callable(_path);
    }

    private PathFinding.Node checkNode;
    private void PrepareNode(int x, int y, int z)
    {
        if (x < 0 || x >= lengthX)
            return;
        if (y < 0 || y + 1 >= lengthY)
            return;
        if (z < 0 || z >= lengthZ)
            return;

        //hier alle checks doen
        checkNode = p.grid[x, y, z];

        if (closed.Contains(checkNode) || !checkNode.filled)
            return;

        //check if already waiting to be checked
        foreach (Node node in open)
            if (node.node == checkNode)
                return;

        //switch to node above
        checkNode = p.grid[x, y + 1, z];
        if (checkNode.filled && !bake.myNodes.Contains(checkNode))
            return;

        _position = new Vector3(x, y, z);
        distanceOrigin = Vector3.Distance(_position, origin);
        distanceGoal = Vector3.Distance(_position, dest);
        adjecentNodes.Add(new Node(p.grid[x, y, z], nodeToCheck, (int)(distanceOrigin + distanceGoal)));
    }
}
