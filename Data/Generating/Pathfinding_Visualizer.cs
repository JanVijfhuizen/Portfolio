using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Pathfinding_Visualizer : MonoBehaviour {
    [SerializeField, HideInInspector]
    private Vector3 leftBotBack;
    [SerializeField, HideInInspector]
    private Vector3 rightBotBack;
    [SerializeField, HideInInspector]
    private Vector3 leftBotFront;
    [SerializeField, HideInInspector]
    private Vector3 rightBotFront;
    [SerializeField, HideInInspector]
    private Vector3 leftTopBack;
    [SerializeField, HideInInspector]
    private Vector3 rightTopBack;
    [SerializeField, HideInInspector]
    private Vector3 leftTopFront;
    [SerializeField, HideInInspector]
    private Vector3 rightTopFront;
    [SerializeField, HideInInspector]
    private float fullX, fullY;

    private PathFinding pathfinding;
    [SerializeField]
    private float durationPathVisualization;
    [SerializeField]
    private bool showBake;
    [SerializeField]
    private bool visualizeRealltimeObjects;

    private void Awake()
    {
        pathfinding = GetComponent<PathFinding>();
        PathFinding.repaint += UpdateGrid;
    }

    public void Repaint()
    {
        if (!(pathfinding != null))
            return;
        if (!pathfinding.visualize)
            return;
        if (!Application.isPlaying)
            CalcCorners();

        //bottom
        Debug.DrawLine(leftBotBack, rightBotBack, Color.blue);
        Debug.DrawLine(leftBotBack, leftBotFront, Color.blue);
        Debug.DrawLine(rightBotFront, rightBotBack, Color.blue);
        Debug.DrawLine(rightBotFront, leftBotFront, Color.blue);

        //mid
        Debug.DrawLine(leftBotBack, leftTopBack, Color.blue);
        Debug.DrawLine(rightBotBack, rightTopBack, Color.blue);
        Debug.DrawLine(leftBotFront, leftTopFront, Color.blue);
        Debug.DrawLine(rightBotFront, rightTopFront, Color.blue);

        //top
        Debug.DrawLine(leftTopBack, rightTopBack, Color.blue);
        Debug.DrawLine(leftTopBack, leftTopFront, Color.blue);
        Debug.DrawLine(rightTopFront, rightTopBack, Color.blue);
        Debug.DrawLine(rightTopFront, leftTopFront, Color.blue);

        //nu voor elke grootte van de nodes
        if (!pathfinding.visualizeNodes)
            return;

        float xNode = pathfinding.widthSizeNode;
        float yNode = pathfinding.heightSizeNode;

        Vector3 startX, endX, startY, endY;
        float disX;
        float disY;

        //horizontally
        for (int x = 0; x < pathfinding.widthSize + 1; x++)
            for (int y = 0; y < pathfinding.heightSize + 1; y++)
            {
                //calculate cur pos
                disX = x * xNode;
                disY = y * yNode;

                //start
                startX = leftBotBack;
                startX.y += disY;

                startY = startX;
                startY.z += disX;
                startX.x += disX;

                //end
                endX = leftBotFront;
                endX.y += disY;
                endX.x += disX;

                endY = rightBotBack;
                endY.y += disY;
                endY.z += disX;

                Debug.DrawLine(startX, endX, Color.green);
                Debug.DrawLine(startY, endY, Color.red);
            }
    }

    private void CalcCorners()
    {
        //calculate corners
        fullX = pathfinding.GetFullX();
        fullY = pathfinding.GetFullY();

        //bottom
        leftBotBack = pathfinding.GetBoundary();

        rightBotBack = leftBotBack;
        rightBotBack.x += fullX;

        leftBotFront = leftBotBack;
        leftBotFront.z += fullX;

        rightBotFront = rightBotBack;
        rightBotFront.z += fullX;

        //top
        leftTopBack = leftBotBack;
        leftTopBack.y += fullY;

        rightTopBack = leftTopBack;
        rightTopBack.x += fullX;

        leftTopFront = leftTopBack;
        leftTopFront.z += fullX;

        rightTopFront = rightTopBack;
        rightTopFront.z += fullX;
    }

    //de reden dat ik dit dubbel doe is performance, nu hoef ik niet iedere keer deze variabelen te maken als ik update
    public void ChangeColorGridPart(PathFinding.Node node, Color c)
    {
        float _x, _y, _z;
        Vector3 _leftBotBack, _rightBotBack, _leftBotFront, _rightBotFront,
            _leftTopBack, _rightTopBack, _leftTopFront, _rightTopFront;

        _x = node.x * pathfinding.widthSizeNode;
        _y = node.y * pathfinding.heightSizeNode;
        _z = node.z * pathfinding.widthSizeNode;

        //assign positions

        //top
        _leftBotBack = leftBotBack + new Vector3(_x, _y, _z);

        _rightBotBack = _leftBotBack;
        _rightBotBack.x += pathfinding.widthSizeNode;

        _leftBotFront = _leftBotBack;
        _leftBotFront.z += pathfinding.widthSizeNode;

        _rightBotFront = _leftBotFront;
        _rightBotFront.x += pathfinding.widthSizeNode;

        //bot
        _leftTopBack = leftBotBack + new Vector3(_x, _y - pathfinding.heightSizeNode, _z);

        _rightTopBack = _leftTopBack;
        _rightTopBack.x += pathfinding.widthSizeNode;

        _leftTopFront = _leftTopBack;
        _leftTopFront.z += pathfinding.widthSizeNode;

        _rightTopFront = _leftTopFront;
        _rightTopFront.x += pathfinding.widthSizeNode;

        //bottom
        Debug.DrawLine(_leftBotBack, _rightBotBack, c, durationPathVisualization);
        Debug.DrawLine(_leftBotBack, _leftBotFront, c, durationPathVisualization);
        Debug.DrawLine(_rightBotFront, _rightBotBack, c, durationPathVisualization);
        Debug.DrawLine(_rightBotFront, _leftBotFront, c, durationPathVisualization);

        //mid
        Debug.DrawLine(_leftBotBack, _leftTopBack, c, durationPathVisualization);
        Debug.DrawLine(_rightBotBack, _rightTopBack, c, durationPathVisualization);
        Debug.DrawLine(_leftBotFront, _leftTopFront, c, durationPathVisualization);
        Debug.DrawLine(_rightBotFront, _rightTopFront, c, durationPathVisualization);

        //top
        Debug.DrawLine(_leftTopBack, _rightTopBack, c, durationPathVisualization);
        Debug.DrawLine(_leftTopBack, _leftTopFront, c, durationPathVisualization);
        Debug.DrawLine(_rightTopFront, _rightTopBack, c, durationPathVisualization);
        Debug.DrawLine(_rightTopFront, _leftTopFront, c, durationPathVisualization);
    }

    public void UpdateGrid()
    {
        if(!PathFinding.pathfindable)
            return;
        if (!(pathfinding != null))
            return;
        if (!pathfinding.visualize && !visualizeRealltimeObjects)
            return;
        if (!showBake)
            return;

        //calculate corners
        CalcCorners();
        #region Play Time
        Color c = Color.white;
        //local squire
        Vector3 _leftBotBack, _rightBotBack, _leftBotFront, _rightBotFront,
            _leftTopBack, _rightTopBack, _leftTopFront, _rightTopFront;
        float _x, _y, _z;
        for (int x = 0; x < pathfinding.widthSize; x++)
            for (int y = 0; y < pathfinding.heightSize; y++)
                for (int z = 0; z < pathfinding.widthSize; z++)
                {
                    PathFinding.Node node = pathfinding.grid[x, y, z];
                    if (node.filled)
                    {
                        //get right color
                        switch (node.bakeType)
                        {
                            case PathFinding.BakeType.Enemy:
                                c = Color.red;
                                break;
                            case PathFinding.BakeType.Object:
                                c = Color.blue;
                                break;
                            case PathFinding.BakeType.Movable:
                                c = Color.green;
                                break;
                            default:
                                break;
                        }

                        if (visualizeRealltimeObjects && !pathfinding.visualize)
                            if (node.bakeType == PathFinding.BakeType.Object)
                                continue;

                        _x = x * pathfinding.widthSizeNode;
                        _y = y * pathfinding.heightSizeNode;
                        _z = z * pathfinding.widthSizeNode;

                        //assign positions

                        //top
                        _leftBotBack = leftBotBack + new Vector3(_x, _y, _z);

                        _rightBotBack = _leftBotBack;
                        _rightBotBack.x += pathfinding.widthSizeNode;

                        _leftBotFront = _leftBotBack;
                        _leftBotFront.z += pathfinding.widthSizeNode;

                        _rightBotFront = _leftBotFront;
                        _rightBotFront.x += pathfinding.widthSizeNode;

                        //bot
                        _leftTopBack = leftBotBack + new Vector3(_x, _y - pathfinding.heightSizeNode, _z);

                        _rightTopBack = _leftTopBack;
                        _rightTopBack.x += pathfinding.widthSizeNode;

                        _leftTopFront = _leftTopBack;
                        _leftTopFront.z += pathfinding.widthSizeNode;

                        _rightTopFront = _leftTopFront;
                        _rightTopFront.x += pathfinding.widthSizeNode;

                        //bottom
                        Debug.DrawLine(_leftBotBack, _rightBotBack, c);
                        Debug.DrawLine(_leftBotBack, _leftBotFront, c);
                        Debug.DrawLine(_rightBotFront, _rightBotBack, c);
                        Debug.DrawLine(_rightBotFront, _leftBotFront, c);

                        //mid
                        Debug.DrawLine(_leftBotBack, _leftTopBack, c);
                        Debug.DrawLine(_rightBotBack, _rightTopBack, c);
                        Debug.DrawLine(_leftBotFront, _leftTopFront, c);
                        Debug.DrawLine(_rightBotFront, _rightTopFront, c);

                        //top
                        Debug.DrawLine(_leftTopBack, _rightTopBack, c);
                        Debug.DrawLine(_leftTopBack, _leftTopFront, c);
                        Debug.DrawLine(_rightTopFront, _rightTopBack, c);
                        Debug.DrawLine(_rightTopFront, _leftTopFront, c);
                    }
                }
        #endregion
    }
}
