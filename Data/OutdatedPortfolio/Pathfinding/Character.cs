using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour {

    public int walkPoints;
    public LevelCreator.Node myNode;
    public bool takesTurn = true, friendly;
    private TurnManager tM;
    [SerializeField]
    private Color pathColor;
    private LevelCreator lC;

    protected virtual void Start()
    {
        SetupReference();
        if (friendly)
            tM.players.Add(this);
        else
            tM.enemies.Add(this);
    }

    protected virtual void SetupReference()
    {
        tM = GameManager.self.tm;
        lC = LevelCreator.self;
    }

    public virtual void TakeTurn()
    {
        if (!takesTurn)
            EndTurn();
        nodeSelected = myNode;
        TurnManager.curChar = this;
    }

    public virtual void EndTurn()
    {
        ColorPath(nodePath, Color.white);
        nodePath.Clear();
        nodeSelected = null;

        tM.UpdateTurn();
        TurnManager.curChar = null;
    }

    private LevelCreator.Node nodeSelected;
    private List<LevelCreator.Node> nodePath = new List<LevelCreator.Node>();
    public void PreparePath(LevelCreator.Node node)
    {
        //get path
        if (nodeSelected == node)
            return;

        List<LevelCreator.Node> path = GetPath(node);
        //if length 0 return
        if (path.Count == 0)
            return;

        //disable color for all path parts
        ColorPath(nodePath, Color.white);
        nodePath = path;

        nodeSelected = node;

        //enable color for all path parts
        ColorPath(path, pathColor);
    }

    private class Node : IComparable<Node>
    {
        public LevelCreator.Node node;
        public Node parent;
        public int cost;

        public Node(LevelCreator.Node _node, Node _parent)
        {
            node = _node;
            parent = _parent;
        }

        public int CompareTo(Node other)
        {
            if (other == null)
                return 1;
            return cost - other.cost;
        }
    }

    private List<Node> open;
    private List<LevelCreator.Node> closed;
    Node curNode = null;
    LevelCreator.Node destination = null;
    private List<LevelCreator.Node> GetPath(LevelCreator.Node dest)
    {
        List<LevelCreator.Node> path = new List<LevelCreator.Node>();
        destination = dest;
        open = new List<Node>();
        closed = new List<LevelCreator.Node>();
        open.Add(new Node(myNode, null));

        curNode = null;
        LevelCreator.Node n;
        while(open.Count > 0)
        {
            curNode = open[0];
            open.Remove(curNode);
            if (curNode.node == dest)
                break;
            n = curNode.node;
            //check around the character
            closed.Add(curNode.node);

            CheckNode(n.x, n.y, n.z + 1);
            CheckNode(n.x + 1, n.y, n.z);
            CheckNode(n.x, n.y, n.z - 1);
            CheckNode(n.x - 1, n.y, n.z);

            //sort
            open.Sort();
        }

        if (!(curNode != null))
            return path;
        if (curNode.node != dest)
            return path;

        path.Add(curNode.node);
        while(curNode.parent != null)
        {
            path.Add(curNode.parent.node);
            curNode = curNode.parent;
        }

        return path;
    }

    private void CheckNode(int x, int y, int height)
    {
        //check if out of bounds
        if (x < 0 || y < 0)
            return;
        if (x >= lC.levelW - 1 || y >= lC.levelW - 1)
            return;

        LevelCreator.Node node = lC.level[x,y,height];

        //check if node is in open or closed
        foreach (Node n in open)
            if (n.node == node)
                return;
        if (closed.Contains(node))
            return;

        //other checks
        if (!node.filled)
            return;
        if (node.occupied)
            return;
        switch (node.tile.type)
        {
            case Ground.GroundType.Wall:
                return;
            case Ground.GroundType.Unwalkable:
                return;
            default:
                break;
        }

        //add parent, cost and add to open
        Node newNode = new Node(node, curNode);
        newNode.cost = (int)Vector2.Distance(new Vector2(newNode.node.x, newNode.node.y), 
            new Vector2(myNode.x, myNode.y));
        newNode.cost += (int)Vector2.Distance(new Vector2(newNode.node.x, newNode.node.y), 
            new Vector2(destination.x, destination.y));
        open.Add(newNode);
    }

    private void ColorPath(List<LevelCreator.Node> path, Color c)
    {
        foreach (LevelCreator.Node n in path)
            n.tile.obj.GetComponent<SpriteRenderer>().color = c;
    }
}
