using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Visualizer), typeof(TurnManager), typeof(TouchManager))]
public class LevelCreator : MonoBehaviour {

    //current data
    [HideInInspector]
    public bool generated = false;
    public Node[,,] level;
    private TileSet curTileSet;
    public int levelW;
    private int levelH;
    [HideInInspector]
    public float sizeNode;

    //general data
    [SerializeField]
    private TileSet[] tileSets;
    [SerializeField]
    private Transform levelHolder;
    [SerializeField, Tooltip("Amount of nodes instantiated per frame")]
    private int nodesPerFrame = 20;

    private System.Random random;
    [SerializeField]
    private string seed;
    [SerializeField]
    private bool randomSeed;
    [SerializeField]
    private float offsetCharacters;

    private Visualizer visualize;
    public static LevelCreator self;

    private void Awake()
    {
        self = this;
        visualize = GetComponent<Visualizer>();
        BuildLevel("Forest", levelW);
    }

    public void SetCurTileSet(string setTag)
    {
        foreach (TileSet set in tileSets)
            if (setTag == set.tag)
            {
                curTileSet = set;
                break;
            }
        if (!(curTileSet != null))
        {
            Debug.Log("Set with tag " + setTag + " not found.");
            return;
        }
    }

    public Vector3 GetLevelSize()
    {
        float calc = levelW * sizeNode;
        return new Vector3(calc, levelH, calc);
    }

    public float CalcNodeSize()
    {
        return curTileSet.ground.obj.GetComponent<BoxCollider>().size.y;
    }

    public Vector3 CalcStartPoint()
    {
        float calc = levelW * sizeNode;
        levelH = curTileSet._2d ? 1 : 2;

        return new Vector3(-calc, -calc, -levelH);
    }

    public void BuildLevel(string setTag, int width)
    {
        seed = randomSeed ? UnityEngine.Random.Range(0, 1000).GetHashCode().ToString() : seed;
        random = new System.Random(seed.GetHashCode());

        SetCurTileSet(setTag);

        sizeNode = CalcNodeSize();

        leftBottom = CalcStartPoint();

        level = new Node[width, levelH, width];
        InitializeLevel();

        //spawn rooms
        StartCoroutine(SpawnRooms());

        //if rooms contain ladders spawn additional levels

        //locate entrance and exit, always default level

        //create path from entrance to exit

        //make sure all rooms are connected 
    }

    private List<Vector3> connectionPoints = new List<Vector3>(); //not really vector3's but level positions
    private IEnumerator SpawnRooms()
    {
        List<Room> spawnable = new List<Room>();
        int calcX = 0, calcY = 0;
        foreach(Room r in curTileSet.requiredRooms)
        {
            calcX += r.x;
            calcY += r.y;
            spawnable.Add(r);
        }

        calcX = levelW / 2 - calcX + 2;
        calcY = levelW / 2 - calcY + 2;

        //total size rooms has to be 1/3 of levelsize, both width and height
        if (calcX <= 0 || calcY <= 0)
        {
            Debug.Log("Too little space for required rooms. Aborting.");
            yield break;
        }
        
        foreach (Room r in spawnable)
        {
            RandomizeRoomPos(r, 0);
            yield return null;
        }

        //spawn extra rooms
        spawnable.Clear();

        //spawn room and scan x + y with raycast, getcomponent of each groundtile that provides wether or not it is walkable
        //first requiredRooms spawning

        //actually, first connect rooms
        StartCoroutine(InstantiateNodes());
    }

    private void RandomizeRoomPos(Room r, int height)
    {
        int _x, _y, calc;
        bool fit = false;
        calc = levelW - 1;
        //avoid the borders

        while (!fit)
        {
            fit = true;
            _x = random.Next(1, calc - r.x);
            _y = random.Next(r.y, calc);

            for (int x = 0; x < r.x; x++)
                for (int y = 0; y < r.y; y++)
                    if (level[_x + x, height, _y - y].filled)
                        fit = false;
            if (!fit)
                continue;

            //calculate x and y pos of room
            Vector3 pos = CalcPos(_x,height,_y);

            //instantiate room
            Instantiate(r.room, pos, Quaternion.identity);
            //shoot raycasts foreach x and y to decide where its walkable
            ScanRoom(_x, _y, r.x, r.y, height);
            FillRoom(_x, _y, height, r);
            //if hasladder add to 2nd layer spawning, if 1st height ofcourse
        }
    }

    private void ScanRoom(int startX, int startY, int sizeX, int sizeY, int height)
    {
        Vector3 rayPos;
        RaycastHit hit;
        Color c;

        //assigning values
        Ground ground;
        Node node;
        Tile newTile;

        for(int _x = 0; _x < sizeX; _x++)
            for(int _y = 0; _y < sizeY; _y++)
            {
                rayPos = CalcPos(_x + startX, height, -_y + startY); //hier komt soms een indexoutofrange soms
                rayPos.z -= sizeNode / 2;

                if (Physics.Raycast(rayPos, Vector3.forward, out hit, sizeNode * 1.5f))
                {
                    c = Color.red;
                    //SCAN ELKE NODE, EN FILL MET JUISTE DATA
                    ground = hit.transform.GetComponent<Ground>();
                    if (!(ground != null)) //not sure if dit op deze manier werkt
                        continue;

                    //no need to check out of bound
                    node = level[startX + _x, height, startY - _y];
                    node.filled = true;

                    //tile data
                    newTile = new Tile();
                    newTile.obj = hit.transform.gameObject;
                    newTile.type = ground.type;
                    node.tile = new SpawnedTile(newTile);
                    node.tile.spawned = true;

                    hit.transform.GetComponent<TileNode>().node = node;
                    if (ground.type == Ground.GroundType.Connection)
                        connectionPoints.Add(new Vector3(startX + _x, height, startY + _y));
                }
                else c = Color.gray;
                visualize.ShowRay(rayPos, Vector3.forward, c);
            }
    }

    public void FillRoom(int startX, int startY, int height, Room r)
    {
        int sizeX = r.x;
        int sizeY = r.y;

        List<Node> freenodes = new List<Node>();
        Node node;
        SpawnedTile tile;
        for(int x = 0; x < sizeX; x++)
            for(int y = 0; y < sizeY; y++)
            {
                node = level[x + startX, height, -y + startY];

                if (!node.filled || node.occupied)
                    continue;

                tile = node.tile;
                if (tile.type != Ground.GroundType.Walkable)
                    continue;

                freenodes.Add(node);
            }

        if(r.reqSpawns.Length > freenodes.Count)
        {
            Debug.Log("Too many spawns for this chamber. Aborting.");
            return;
        }

        Vector3 pos;
        GameObject inst;
        Character c;
        foreach (SpawnObject g in r.reqSpawns)
        {
            node = level[g.x + startX, height, -g.y + startY];
            pos = CalcPos(node);
            pos.y += offsetCharacters;
            pos.z -= 0.1f;
            inst = Instantiate(g.obj, pos, Quaternion.identity);

            //character data
            c = inst.GetComponent(typeof(Character)) as Character;
            if (c != null)
                c.myNode = node;
            node.occupied = true;
        }

        //spawn later random stuff
    }

    protected bool CheckNode(int x, int y, int height)
    {
        if (x < 0 || y < 0)
            return false;

        if (x >= level.GetLength(0))
            return false;
        if (y >= level.GetLength(1))
            return false;
        Node node = level[x, height, y];
        if (!node.filled || node.occupied)
            return false;
        if (node.tile.type != Ground.GroundType.Walkable)
            return false;

        return true;
    }

    private void InitializeLevel()
    {
        for (int x = 0; x < levelW; x++)
            for (int y = 0; y < levelH; y++)
                for (int z = 0; z < levelW; z++)
                    level[x,y,z] = new Node(x,y,z);
    }

    private IEnumerator InstantiateNodes()
    {
        Node node;
        GameObject tile;
        GameObject spawnable;
        int counter = 0;
        for(int x = 0; x < levelW; x++)
            for(int y = 0; y < levelH; y++)
                for(int z = 0; z < levelW; z++)
                {
                    node = level[x, y, z];
                    if (!node.filled)
                        continue;
                    if (node.tile.spawned)
                        continue;
                    spawnable = node.tile.obj;
                    tile = Instantiate(spawnable, CalcPos(node), Quaternion.identity);
                    node.tile.obj = tile;
                    node.tile.sRColor = tile.GetComponent<SpriteRenderer>();

                    tile.transform.SetParent(levelHolder);
                    counter++;
                    if(counter >= nodesPerFrame)
                    {
                        counter = 0;
                        yield return null;
                    }
                }

        generated = true;
    }

    #region Position Data
    private Vector3 leftBottom;

    public Vector3 CalcPos(int x, int y, int z)
    {
        return CalcPos(level[x,y,z]);
    }

    public Vector3 CalcPos(Node node)
    {
        Vector3 ret = leftBottom;
        ret.x += node.x * sizeNode;
        ret.z -= node.y * sizeNode;
        ret.y += node.z * sizeNode;
        return ret;
    }
    #endregion

    public class Node
    {
        public bool filled, occupied;
        public int x, y, z;
        public SpawnedTile tile;

        public Node(int _x, int _y, int _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }
    }

    #region Rooms
    [Serializable]
    public class Room{
        public int x, y;
        [Tooltip("The pivot has to be in the left top corner.")]
        public GameObject room;
        public SpawnObject[] reqSpawns;
        public bool freeSpawnAllowed;
    }

    [Serializable]
    public class SpawnObject
    {
        public GameObject obj;
        public int x, y;
    }
    #endregion

    #region Tile(set) Class
    [Serializable]
	public class TileSet
    {
        public string tag;
        public bool _2d;
        public Tile ground;
        public Room[] rooms;
        public Room[] requiredRooms;
    }

    [Serializable]
    public class Tile
    {
        public Ground.GroundType type;
        public GameObject obj;
    }

    public class SpawnedTile
    {
        public bool spawned;
        public Ground.GroundType type;
        public GameObject obj;
        public SpriteRenderer sRColor;

        public SpawnedTile(Tile tile)
        {
            type = tile.type;
            obj = tile.obj;
        }
    }
    #endregion
}
