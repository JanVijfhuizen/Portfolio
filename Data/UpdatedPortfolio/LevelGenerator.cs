using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using GridExtensions;
using Random = System.Random;

namespace Generating
{
    public static class LevelGenerator
    {
        public static Level GenerateLevel(LevelData data, Random random)
        {
            Level level = new Level(data.size);

            #region Create Rooms
            int roomCount = random.Next(data.minRoomCount, data.maxRoomCount + 1);
            level.rooms = new List<Room>(roomCount);
            Vector2Int roomSize, roomPosition;

            for (int i = 0; i < roomCount; i++)
            {
                roomSize = new Vector2Int(random.Next(data.minRoomSize, data.maxRoomSize), random.Next(data.minRoomSize, data.maxRoomSize));
                roomPosition = new Vector2Int(random.Next(0, data.size - roomSize.x), random.Next(0, data.size - roomSize.y));
                level.rooms.Add(new Room(roomPosition, roomSize));
            }

            Node roomNode;

            foreach (Room room in level.rooms)
                for (int x = 0; x < room.size.x; x++)
                    for (int y = 0; y < room.size.y; y++)
                    {
                        roomNode = level.nodes[x + room.position.x, y + room.position.y];
                        roomNode.filled = true;
                        roomNode.locked = true;                       
                    }
            #endregion
            
            #region Create Paths
            Pathfinding<Node> pathfinding = new Pathfinding<Node>(data.size * data.size, level.nodes);
            List<Vector2Int> path = new List<Vector2Int>(data.size * data.size);
            Room startRoom, endRoom;
            Vector2Int from, to;
            int connectionAmount, endRoomIndex;

            for (int i = 0; i < roomCount; i++)
            {
                startRoom = level.rooms[i];
                connectionAmount = random.Next(1, 2 + data.maxExtraPathsPerRoom);

                for (int j = 0; j < connectionAmount; j++)
                {
                    endRoomIndex = random.Next(0, roomCount);
                    if (level.rooms[endRoomIndex] == startRoom)
                        endRoomIndex++;
                    endRoom = level.rooms[endRoomIndex];

                    from = new Vector2Int(random.Next(0, startRoom.size.x) + startRoom.position.x, random.Next(0, startRoom.size.y) + startRoom.position.y);
                    to = new Vector2Int(random.Next(0, endRoom.size.x) + endRoom.position.x, random.Next(0, endRoom.size.y) + endRoom.position.y);

                    pathfinding.Calculate2D(path, from, to, false);

                    foreach(Vector2Int vec in path)
                    {
                        roomNode = level.nodes[vec.x, vec.y];
                        roomNode.filled = true;
                        roomNode.locked = true;
                    }
                    path.Clear();
                }
            }
            #endregion

            #region Cellular Automata
            Node levelNode;

            Func<int, int, bool, bool> isCorrespondingNeighbour = delegate (int x, int y, bool filled)
            {
                if (level.IsOutOfBounds(x, y))
                    return true;

                return level.nodes[x, y].filled == filled;
            };

            for (int x = 0; x < data.size; x++)
                for (int y = 0; y < data.size; y++)
                {
                    levelNode = level.nodes[x, y];

                    if (levelNode.locked)
                        continue;

                    levelNode.filled = random.NextDouble() < data.fillPercentage;
                }

            int neighbourCount;
            bool neighbourFilled;

            for (int i = 0; i < data.smoothAmount; i++)
                for (int x = 0; x < data.size; x++)
                    for (int y = 0; y < data.size; y++)
                    {
                        neighbourCount = 0;
                        neighbourFilled = level.nodes[x, y].filled;

                        if (isCorrespondingNeighbour(x, y + 1, neighbourFilled))
                            neighbourCount++;
                        if (isCorrespondingNeighbour(x + 1, y, neighbourFilled))
                            neighbourCount++;
                        if (isCorrespondingNeighbour(x, y - 1, neighbourFilled))
                            neighbourCount++;
                        if (isCorrespondingNeighbour(x - 1, y, neighbourFilled))
                            neighbourCount++;

                        if (neighbourCount < 2)
                            level.nodes[x, y].filled = !level.nodes[x, y].filled;
                    }

            #endregion

            #region Ensure Connectivity
            Vector2Int levelRoot = level.rooms[0].position;

            foreach (Node node in level.nodes)
                node.Walkable = node.filled;

            for (int x = 0; x < data.size; x++)
                for (int y = 0; y < data.size; y++)
                {
                    if (level.nodes[x, y].locked || !level.nodes[x, y].filled)
                        continue;

                    path.Clear();
                    from = new Vector2Int(x, y);
                    pathfinding.Calculate2D(path, from, levelRoot, false);

                    if (path.Count == 0)
                    {
                        pathfinding.CalculateFill2D(path, from, false);

                        foreach(Vector2Int vec in path)
                        {
                            levelNode = level.nodes[vec.x, vec.y];
                            if (levelNode.filled)
                                levelNode.filled = false;
                        }
                    }
                }
            #endregion

            return level;
        }
    }

    [Serializable]
    public struct LevelData
    {
        public int size, minRoomCount, maxRoomCount, 
            minRoomSize, maxRoomSize, maxExtraPathsPerRoom, smoothAmount;
        [Range(0, 1)]
        public double fillPercentage;
    }

    public class Room
    {
        public Vector2Int position, size;

        public Room(Vector2Int position, Vector2Int size)
        {
            this.position = position;
            this.size = size;
        }
    }

    public class Level
    {
        public Node[,] nodes;
        public List<Room> rooms;
        public int Size { get; private set; }

        public Level(int size)
        {
            nodes = new Node[size, size];
            Size = size;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    nodes[x, y] = new Node(new Vector2Int(x, y));
        }
    }

    public class Node : IHeapable<Node>
    {
        public Vector2Int Parent { get; set; }
        public Vector2Int Position { get; set; }
        public int HeapIndex { get; set; }
        public float G { get; set; }
        public float H { get; set; }
        public bool Walkable { get; set; }

        public bool filled, locked;
        public GameObject obj;

        public Node(Vector2Int position)
        {
            Position = position;
            Walkable = true;
        }

        public int CompareTo(Node other)
        {
            return Mathf.RoundToInt(other.G + other.H - G - H);
        }
    }
}
