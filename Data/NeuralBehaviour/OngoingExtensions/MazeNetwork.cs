using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeNetwork : NeuralBehaviour
{
    [SerializeField]
    private Maze maze;

    [SerializeField]
    private bool train, load;

    private NeuralSaveData saveData = new NeuralSaveData();

    private int xPos, yPos;

    private GUIStyle 
        styleWalked, 
        styleWall, 
        stylePath;
    [SerializeField]
    private Color colWalked, colWall, colPath;
    private void Awake()
    {
        if (!train)
        {
            xPos = maze.xPosStart;
            yPos = maze.yPosStart;
            visitedPoints.Add(new Vector2(xPos, yPos));
        }

        if (load)
            saveData = Load();

        outputSize = 4; //each direction
        projectedMaze = maze;
        ConvertGrid(projectedMaze);
        calcX = Mathf.FloorToInt((float)Screen.width / length);
        calcY = Mathf.FloorToInt((float)Screen.height / length);
    }

    private Texture2D MakeTex(Color col)
    {
        Color[] pix = new Color[calcX * calcY];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }

        Texture2D result = new Texture2D(calcX, calcY);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private List<Vector2> visitedPoints = new List<Vector2>();
    private int updateTurns = 0, lastGen = 0;
    private void Update()
    {
        NeuralOutput nO = Call();
        if (train)
        {
            Train();
            if (lastGen < nO.generation)
            {
                Save(saveData);
                lastGen++;
                print("Generation score: " + nO.network.score);
            }
        }
        else if (updateTurns < maxTurns)
        {
            updateTurns++;
            Move(nO.output);
            visitedPoints.Add(new Vector2(xPos, yPos));
        }
    }

    private int calcX, calcY;
    private bool setupGUIDone;
    protected virtual void OnGUI()
    {
        //if (train)
            //return;
        if(!setupGUIDone)
        {
            styleWalked = new GUIStyle(GUI.skin.box);
            styleWall = new GUIStyle(GUI.skin.box);
            stylePath = new GUIStyle(GUI.skin.box);

            styleWalked.normal.background = MakeTex(colWalked);
            stylePath.normal.background = MakeTex(colPath);
            styleWall.normal.background = MakeTex(colWall);

            setupGUIDone = true;
        }
        float calc;

        GUIStyle cur;
        for (int y = 0; y < length; y++)
            for (int x = 0; x < length; x++)
            {
                calc = mazeAsFloatList[x + y * length];
                cur = calc > 0.5f ? styleWall : stylePath;
                foreach (Vector2 vec in visitedPoints)
                    if (Mathf.RoundToInt(vec.x) == x && Mathf.RoundToInt(vec.y) == y)
                    {
                        cur = styleWalked;
                        break;
                    }

                GUI.Box(new Rect(calcX * x, calcY * (length - 1 - y), calcX, calcY),
                    "", cur);
            }
    }

    [System.Serializable]
    public class Maze
    {
        public Texture2D tex;
        public int xPosStart, yPosStart, xPosEnd, yPosEnd;
    }

    private List<float> mazeAsFloatList = new List<float>();
    private Color[] mazeColors;
    private int length;
    private void ConvertGrid(Maze maze)
    {
        mazeAsFloatList.Clear();
        mazeColors = maze.tex.GetPixels();
        length = (int)Mathf.Sqrt(mazeColors.Length);  

        int index;
        for (int height = 0; height < length; height++)
            for (int width = 0; width < length; width++)
            {
                index = height * length + width;
                mazeAsFloatList.Add(mazeColors[index].grayscale);
            }
    }

    protected override List<float> GetInput(bool isTraining)
    {
        List<float> ret = new List<float>() {
            xPos, yPos,
            projectedMaze.xPosEnd,
            projectedMaze.yPosEnd};
        foreach (float i in mazeAsFloatList)
            ret.Add(i);
        return ret;
    }

    [SerializeField]
    private List<Maze> testMazes = new List<Maze>();

    [SerializeField]
    private int maxTurns;
    private Maze projectedMaze;
    protected override IEnumerator Rate(NeuralNetwork net)
    {
        projectedMaze = testMazes[Random.Range(0, testMazes.Count - 1)];
        ConvertGrid(projectedMaze);
        xPos = projectedMaze.xPosStart;
        yPos = projectedMaze.yPosStart;
        //reset and change into new testlevel
        List<Vector2> visited = new List<Vector2>();
        bool progressing;
        net.score = 0;

        for (int turn = 0; turn < maxTurns; turn++)
        {
            visited.Add(new Vector2(xPos, yPos));

            visitedPoints = visited;

            //check dir
            Move(net.GetNext(GetInput(true)));
            
            if(xPos >= length || yPos >= length || xPos < 0 || yPos < 0)
                yield break;
            if(mazeAsFloatList[yPos * length + xPos] < 0.5f)
                yield break;

            progressing = true;
            foreach(Vector2 vec in visited)
                if(Mathf.RoundToInt(vec.x) == xPos && Mathf.RoundToInt(vec.y) == yPos)
                {
                    progressing = false;
                    break;
                }
            if (progressing)
                net.score++;

            if(xPos == projectedMaze.xPosEnd && yPos == projectedMaze.yPosEnd)
            {
                net.score = Mathf.Pow(length, 2); //unachievable score used for completing the maze
                print("Maze solved");
                yield break;
            }
        }
    }

    private void Move(List<float> output)
    {
        int dir = 0;
        float value = output[0];
        if (output[1] > value)
        {
            dir = 1;
            value = output[1];
        }
        if (output[2] > value)
        {
            dir = 2;
            value = output[2];
        }
        if (output[3] > value)
            dir = 3;

        switch (dir)
        {
            case 0:
                yPos++;
                break;
            case 1:
                xPos++;
                break;
            case 2:
                yPos--;
                break;
            case 3:
                xPos--;
                break;
        }
    }
}
