using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDC
{
    namespace Generating
    {
        [RequireComponent(typeof(MapVisualizer)), RequireComponent(typeof(MapData))]
        [RequireComponent(typeof(MapLoader))]
        public class MapGenerator : MonoBehaviour
        {
            #region Mapdata Settings
            
            private int widthMin, widthMax;
            private List<TagManager.TagType> tags;
            public enum MapType {Two_Dimensional, Three_Dimensional }
            public MapType mapType;

            #endregion

            public Node[,,] level;

            //random generating
            public System.Random random;
            [SerializeField]
            private string seed;
            [HideInInspector]
            public int _seed;
            [SerializeField]
            private bool randomSeed;
            public Node entrance;
            private MapVisualizer visualizer;
            private MapLoader loader;
            private MapData mapData;

            #region Shortcuts for data
            [HideInInspector]
            public int size, ySize;
            #endregion

            #region Room Management
            [SerializeField, Tooltip(
                "All the different types of rooms (for instance: Top (Entrance), Left(Entrance), Right(Entrance), Back(Closed), Front(Closed), Down(Closed)) are required to make a full dungeon.")]
            private List<Room> rooms;
            private List<Room> _rooms; //all rooms + rotated rooms

            private void ConvertRooms()
            {
                _rooms = new List<Room>();
                TagManager tM = null;
                foreach (Room rc in rooms)
                {
                    tM = rc.room.GetComponent<TagManager>();
                    if(!(tM != null))
                    {
                        Debug.Log("There is no tag manager attached to a room. Retard.");
                        return;
                    }
                    if (!tags.Contains(tM._tag))
                        continue;
                    Room temp = rc;
                    _rooms.Add(temp);
                    if (temp.converter == Converting.None)
                        continue;

                    if(temp.converter == Converting.All)
                        for (int i = 0; i < 3; i++)
                        {
                            temp = TurnRoom(temp);
                            _rooms.Add(temp);
                        }
                    else
                    {
                        Room _temp = TurnRoom(temp);
                        temp = TurnRoom(_temp);
                        _rooms.Add(temp);
                    }
                }
            }

            private Room TurnRoom(Room r)
            {
                Room ret = new Room();
                ret.front = r.right;
                ret.right = r.back;
                ret.back = r.left;
                ret.left = r.front; //stabilize rotation
                int rot = (int)r.rotation - 90;

                if (rot == -360)
                    rot = 0;
                ret.rotation = (Rotation)rot;
                ret.room = r.room;
                return ret;
            }
            #endregion

            private void Awake()
            {
                visualizer = GetComponent<MapVisualizer>();
                loader = GetComponent<MapLoader>();
                mapData = GetComponent<MapData>();
                StartCoroutine(Initializing());
            }

            private IEnumerator Initializing()
            {
                //set map data
                widthMin = MapData.widthMin;
                widthMax = MapData.widthMax;
                tags = MapData.tags;

                if (widthMin < 2)
                {
                    Debug.Log("Try setting the minimal width to at least 2.");
                    yield break;
                }
                if (widthMax < widthMin)
                {
                    Debug.Log("The max width is lower than the min width!");
                    yield break;
                }

                yield return null;
                
                //convert room to all directions
                ConvertRooms();
                loader.SetProgress(MapLoader.Progress.Convert_Rooms);

                yield return null;
                //set seed
                SetSeed();
                loader.SetProgress(MapLoader.Progress.Set_Seed);

                yield return null;
                //initialize nodes and nodelevels
                StartCoroutine(InitializeLevel());
            }

            #region STEPS

            //STEP #1
            private void SetSeed()
            {
                if (randomSeed)
                {
                    int i = UnityEngine.Random.Range(0, 1000).GetHashCode();
                    _seed = i;
                }
                else
                    _seed = seed.GetHashCode();
                random = new System.Random(_seed);
            }

            //STEP #2     
            private IEnumerator InitializeLevel()
            {
                //data
                size = random.Next(widthMin, widthMax);
                ySize = mapType == MapType.Three_Dimensional ? size : 1;
                level = new Node[size, ySize, size];
                for (int x = 0; x < size; x++)
                {
                    yield return null;
                    for (int y = 0; y < ySize; y++)
                    {
                        for (int z = 0; z < size; z++)
                        {
                            Node n = level[x, y, z] = new Node();
                            n.posX = x;
                            n.posY = y;
                            n.posZ = z;
                        }
                    }
                }

                //set entrance
                SetEntrance();

                //make path using seed spiraling down
                StartCoroutine(CreateRootPath());

                //spawn all _rooms
            }

            //STEP #3
            private void SetEntrance()
            {
                int x = random.Next(0, size);
                int z = random.Next(0, size);
                entrance = level[x, ySize - 1, z];
            }

            //STEP #4
            private enum RootDirection { Down, Left, Right, Front, Back, Count }
            private IEnumerator CreateRootPath()
            {
                //list of the path, get last when in while, first in branches
                List<Node> path = new List<Node>();
                path.Add(entrance);
                while (path[path.Count - 1].posY != 0) //maak hier miss een stack van
                {
                    yield return null;
                    //check which direction to go (left right forward back bottom), and check if you CAN
                    int dir = random.Next(0, (int)RootDirection.Count - 1);
                    Node newN = null;
                    Node oldN = path[path.Count - 1];
                    oldN.initialized = true;
                    oldN.room = new Room();
                    switch (dir) //the direction of the next part of the path
                    {
                        //check if direction can be taken
                        //else continue
                        case (int)RootDirection.Down:
                            newN = level[oldN.posX, oldN.posY - 1, oldN.posZ]; //no additional checks required
                            oldN.room.down = Connection.Required;
                            break;
                        case (int)RootDirection.Left:
                            if (oldN.posX <= 0) //check if adjecent excist
                                continue;
                            //set new node, if a check fails it doesnt matter because it will set again when it works
                            newN = level[oldN.posX - 1, oldN.posY, oldN.posZ];
                            if (newN.initialized) //check if initialized
                                continue;
                            //init old node room and set direction
                            oldN.room.left = Connection.Required;
                            break;
                        case (int)RootDirection.Right:
                            if (oldN.posX >= size - 1)
                                continue;
                            newN = level[oldN.posX + 1, oldN.posY, oldN.posZ];
                            if (newN.initialized)
                                continue;
                            oldN.room.right = Connection.Required;
                            break;
                        case (int)RootDirection.Front:
                            if (oldN.posZ >= size - 1)
                                continue;
                            newN = level[oldN.posX, oldN.posY, oldN.posZ + 1];
                            if (newN.initialized)
                                continue;
                            oldN.room.front = Connection.Required;
                            break;
                        case (int)RootDirection.Back:
                            if (oldN.posZ <= 0)
                                continue;
                            newN = level[oldN.posX, oldN.posY, oldN.posZ - 1];
                            if (newN.initialized)
                                continue;
                            oldN.room.back = Connection.Required;
                            break;
                    }

                    InitializeNode(oldN);

                    //save that new node in list
                    path.Add(newN);

                    //repeat until the ground floor has been reached
                }

                Node exit = null;
                //used for 1 floor dungeons
                if (mapType == MapType.Two_Dimensional)
                {
                    exit = entrance;
                    InitializeNode(exit);
                }
                else
                    exit = path[path.Count - 1];
                
                loader.SetProgress(MapLoader.Progress.Creating_Main_Path);

                //function: create branches
                while (path.Count > 0) //use same list in a different way, always get bottom of the list (0)
                {
                    //fill nodes until none are left / accessible
                    //check foreach adjecents, if accessible but empty, fill and add to list

                    Node n = path[0];
                    path.RemoveAt(0);

                    //check adjecent

                    //top
                    if (n.room.top != Connection.Closed) //check out of bounds
                        if (n.posY < size - 1)
                        {
                            //check !initialized
                            Node _n = level[n.posX, n.posY + 1, n.posZ];
                            if (!_n.initialized)
                            {
                                //pick room
                                InitializeNode(_n);
                                //add to list
                                path.Add(_n);
                            }
                        }

                    //bottom
                    if (n.room.down != Connection.Closed)
                        if (n.posY > 0)
                        {
                            Node _n = level[n.posX, n.posY - 1, n.posZ];
                            if (!_n.initialized)
                            {
                                InitializeNode(_n);
                                path.Add(_n);
                            }
                        }

                    //right
                    if (n.room.right != Connection.Closed)
                        if (n.posX < size - 1)
                        {
                            Node _n = level[n.posX + 1, n.posY, n.posZ];
                            if (!_n.initialized)
                            {
                                InitializeNode(_n);
                                path.Add(_n);
                            }
                        }

                    //left
                    if (n.room.left != Connection.Closed)
                        if (n.posX > 0)
                        {
                            Node _n = level[n.posX - 1, n.posY, n.posZ];
                            if (!_n.initialized)
                            {
                                InitializeNode(_n);
                                path.Add(_n);
                            }
                        }

                    //front
                    if (n.room.front != Connection.Closed)
                        if (n.posZ < size - 1)
                        {
                            Node _n = level[n.posX, n.posY, n.posZ + 1];
                            if (!_n.initialized)
                            {
                                InitializeNode(_n);
                                path.Add(_n);
                            }
                        }

                    //back
                    if (n.room.back != Connection.Closed)
                        if (n.posZ > 0)
                        {
                            Node _n = level[n.posX, n.posY, n.posZ - 1];
                            if (!_n.initialized)
                            {
                                InitializeNode(_n);
                                path.Add(_n);
                            }
                        }

                    //repeat until no _rooms are left to fill
                    
                    yield return null;
                }

                loader.SetProgress(MapLoader.Progress.Branching_Main_Path);
                
                //find exit
                if (mapType == MapType.Two_Dimensional) {
                    List<Node> allRooms = new List<Node>();
                    foreach (Node n in level)
                        if (n.initialized)
                            allRooms.Add(n);

                    bool exitFound = false;
                    
                    while (!exitFound)
                    {
                        if (allRooms.Count == 0)
                            break;
                        int u = random.Next(0, allRooms.Count - 1);
                        exit = allRooms[u];
                        allRooms.RemoveAt(u);

                        if (exit.initialized)
                            if (exit != entrance)
                                exitFound = true;
                    }
                }
                
                visualizer.SpawnRooms(level, entrance, exit, tags);
            }

            #region Tools

            private void InitializeNodeRoom(Node n)
            {
                n.initialized = true;
                n.room = new Room();
            }

            private void InitializeNode(Node n)
            {
                //get all _rooms with set directions (builds from getfit_rooms)
                List<Room> fitRooms = GetFitRooms(n);
                if (fitRooms.Count == 0)
                {
                    Debug.Log("There arent any fit _rooms for this Node: " + n.posX + " " + n.posY + " " + n.posZ);
                    return;
                }

                //pick random GetFitRooms to put in old node node
                int chosen = random.Next(0, fitRooms.Count - 1);
                n.room = fitRooms[chosen];
                n.initialized = true;

                //tijdelijk
            }

            private List<Room> GetFitRooms(Node n)
            {
                List<Room> ret = new List<Room>();
                foreach (Room r in _rooms)
                    if (CheckBorders(n, r))
                        ret.Add(r);
                return ret;
            }

            #endregion

            //check borders
            public bool CheckBorders(Node node, Room room) //checks both borders and (if initialized) if they meet set required passages
            {
                Node adj = null;

                if (!(node.room != null))
                    node.room = new Room();

                //check top
                //check required
                if (node.initialized)
                    if (node.room.top != room.top && node.room.top != Connection.Required)
                        return false;

                if (0 > node.posY) //size - 1
                    adj = level[node.posX, node.posY + 1, node.posZ];
                else
                    adj = null;
                if (adj != null)
                {
                    if (adj.initialized)
                        if (adj.room.down != room.top)
                            return false;
                }
                //check out of bounds
                else if (room.top != Connection.Closed)
                    return false;

                //check bottom
                //check required
                if (node.initialized)
                    if (node.room.down != room.down && node.room.down != Connection.Required)
                        return false;
                if (0 < node.posY)
                    adj = level[node.posX, node.posY - 1, node.posZ];
                else
                    adj = null;
                if (adj != null)
                {
                    if (adj.initialized)
                        if (adj.room.top != room.down)
                            return false;
                }
                //check out of bounds
                else if (room.down != Connection.Closed)
                    return false;

                //check left
                //check required
                if (node.initialized)
                    if (node.room.left != room.left && node.room.left != Connection.Required)
                        return false;
                if (0 < node.posX)
                    adj = level[node.posX - 1, node.posY, node.posZ];
                else
                    adj = null;
                if (adj != null)
                {
                    if (adj.initialized)
                        if (adj.room.right != room.left)
                            return false;
                }
                //check out of bounds
                else if (room.left != Connection.Closed)
                    return false;

                //check right
                //check required
                if (node.initialized)
                    if (node.room.right != room.right && node.room.right != Connection.Required)
                        return false;
                if (size - 1 > node.posX)
                    adj = level[node.posX + 1, node.posY, node.posZ];
                else
                    adj = null;
                if (adj != null)
                {
                    if (adj.initialized)
                        if (adj.room.left != room.right)
                            return false;
                }
                //check out of bounds
                else if (room.right != Connection.Closed)
                    return false;

                //check front
                //check required
                if (node.initialized)
                    if (node.room.front != room.front && node.room.front != Connection.Required)
                        return false;
                if (size - 1 > node.posZ)
                    adj = level[node.posX, node.posY, node.posZ + 1];
                else
                    adj = null;
                if (adj != null)
                {
                    if (adj.initialized)
                        if (adj.room.back != room.front)
                            return false;
                }
                //check out of bounds
                else if (room.front != Connection.Closed)
                    return false;

                //check back
                //check required
                if (node.initialized)
                    if (node.room.back != room.back && node.room.back != Connection.Required)
                        return false;
                if (0 < node.posZ)
                    adj = level[node.posX, node.posY, node.posZ - 1];
                else
                    adj = null;
                if (adj != null)
                {
                    if (adj.initialized)
                        if (adj.room.front != room.back)
                            return false;
                }
                //check out of bounds
                else if (room.back != Connection.Closed)
                    return false;

                return true;
            }

            #endregion

            //return corresponding (random) room

            #region Classes

            public class Node
            {
                public bool initialized;
                public Room room;
                public int posX, posY, posZ;
            }

            public enum Connection { Closed, Entrance, Full, Required }
            public enum Rotation { Default, Right = 90, Turned = 180, Left = 270 }
            public enum Converting {None, Double, All }

            [Serializable]
            public class Room //create array in room for props to set active at will
            {
                [Header("Ga met de entrances uit van een 0,0,0 rotatie.")]
                public GameObject room;
                //entrances
                [Tooltip("Whether or not you need rotated versions of this room.")]
                public Converting converter;
                public Connection top, down,
                    left, right,
                    front, back;
                [HideInInspector]
                public Rotation rotation = 0;
            }

            #endregion
        }
    }
}
