using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class Pathfinding<T> where T : class, IHeapable<T>
    {
        // The problem area
        private T[,] grid;

        // Setting the size initially and using an array saves garbage being created
        private int heapSize;

        // The heap is based on Sebastian Lague's tutorial
        public class Heap
        {
            private T[] items;
            private int currentItemCount = 0;

            public Heap(int size)
            {
                items = new T[size];
            }

            // Add an item and sort it based on it's CompareTo function
            public void Add(T item)
            {
                item.HeapIndex = currentItemCount;
                items[currentItemCount] = item;
                SortUp(item);
                currentItemCount++;
            }

            // Get the top from the heap
            public T Get()
            {
                T item = items[0];
                currentItemCount--;
                items[0] = items[currentItemCount];
                items[0].HeapIndex = 0;
                SortDown(items[0]);
                return item;
            }

            public int Count
            {
                get
                {
                    return currentItemCount;
                }
            }

            public bool Contains(T item)
            {
                return Equals(items[item.HeapIndex], item);
            }

            // Sort an item up based on it's value, where the best will be on top
            private void SortUp(T item)
            {
                int parentIndex = (item.HeapIndex - 1) / 2;
                T parentItem;

                while (true)
                {
                    parentItem = items[parentIndex];

                    if (item.CompareTo(parentItem) > 0)
                        Swap(item, parentItem);
                    else
                        break;

                    parentIndex = (item.HeapIndex - 1) / 2;
                }
            }

            // Sort an item down based on it's value, where the best will be on top
            private void SortDown(T item)
            {
                int left, right, swapIndex;

                while (true)
                {
                    left = item.HeapIndex * 2 + 1;
                    right = left + 1;
                    swapIndex = 0;

                    if (left < currentItemCount)
                    {
                        swapIndex = left;

                        if (right < currentItemCount)
                            if (items[left].CompareTo(items[right]) < 0)
                                swapIndex = right;

                        if (item.CompareTo(items[swapIndex]) < 0)
                            Swap(item, items[swapIndex]);
                        else
                            return;
                    }
                    else
                        return;
                }
            }

            // Swap two items based on their indexes
            private void Swap(T a, T b)
            {
                items[a.HeapIndex] = b;
                items[b.HeapIndex] = a;

                int index = a.HeapIndex;
                a.HeapIndex = b.HeapIndex;
                b.HeapIndex = index;
            }
        }

        public Pathfinding(int heapSize, T[,] grid)
        {
            this.heapSize = heapSize;
            this.grid = grid;

            int xLength = grid.GetLength(0),
                yLength = grid.GetLength(1);

            // Set up every node's position
            for (int x = 0; x < xLength; x++)
                for (int y = 0; y < yLength; y++)
                    grid[x, y].Position = new Vector2Int(x, y);
        }

        public void Calculate2D(List<Vector2Int> path, Vector2Int from, Vector2Int to, bool verticalMovement)
        {
            Heap open = new Heap(heapSize);
            HashSet<T> closed = new HashSet<T>();

            T start = grid[from.x, from.y],
            end = grid[to.x, to.y];

            int xLength = grid.GetLength(0),
                yLength = grid.GetLength(1);

            open.Add(grid[from.x, from.y]);

            T current = null;

            Func<int, int, bool> isOutOfBounds = delegate (int x, int y)
            {
                return x < 0 || x >= xLength || y < 0 || y >= yLength;
            };

            Func<T, T, T> setItem = delegate (T item, T parent)
            {
                item.Parent = parent.Position;
                item.G = Vector2.Distance(item.Position, start.Position);
                item.H = Vector2.Distance(item.Position, end.Position);
                return item;
            };

            Action<int, int> TryAddNeighbour = delegate (int x, int y)
            {
                x += current.Position.x;
                y += current.Position.y;

                if (isOutOfBounds(x, y))
                    return;
                if (!grid[x, y].Walkable)
                    return;
                if (closed.Contains(grid[x, y]))
                    return;
                if (open.Contains(grid[x, y]))
                    return;

            // Set variables
            grid[x, y] = setItem(grid[x, y], current);
                open.Add(grid[x, y]);
            };

            // Keep getting current's parent until the start position has been found
            while (open.Count > 0)
            {
                current = open.Get();
                closed.Add(current);

                if (current.Position == end.Position)
                {
                    // When the start position is found it means that the complete path has been added
                    while (!current.Equals(start))
                    {
                        path.Add(current.Position);
                        current = grid[current.Parent.x, current.Parent.y];
                    }
                    return;
                }

                // Top
                TryAddNeighbour(0, 1);
                // Right
                TryAddNeighbour(1, 0);
                // Bottom
                TryAddNeighbour(0, -1);
                //Left
                TryAddNeighbour(-1, 0);

                if (verticalMovement)
                {
                    // Top Right
                    TryAddNeighbour(1, 1);
                    // Right Bottom
                    TryAddNeighbour(1, -1);
                    // Bottom Left
                    TryAddNeighbour(-1, -1);
                    //Left Top
                    TryAddNeighbour(-1, 1);
                }
            }

            return;
        }

        public void CalculateFill2D(List<Vector2Int> path, Vector2Int from, bool verticalMovement)
        {
            Heap open = new Heap(heapSize);
            HashSet<T> closed = new HashSet<T>();

            T start = grid[from.x, from.y],
            current;

            int xLength = grid.GetLength(0),
                yLength = grid.GetLength(1);

            open.Add(grid[from.x, from.y]);

            Func<int, int, bool> isOutOfBounds = delegate (int x, int y)
            {
                return x < 0 || x >= xLength || y < 0 || y >= yLength;
            };

            Action<int, int> tryAddNeighbour = delegate (int x, int y)
            {
                if (isOutOfBounds(x, y))
                    return;
                if (!grid[x, y].Walkable)
                    return;
                if (closed.Contains(grid[x, y]))
                    return;
                if (open.Contains(grid[x, y]))
                    return;
                open.Add(grid[x, y]);
            };

            Vector2Int currentPosition;

            while(open.Count > 0)
            {
                current = open.Get();
                closed.Add(current);

                currentPosition = current.Position;

                tryAddNeighbour(currentPosition.x, currentPosition.y + 1);
                tryAddNeighbour(currentPosition.x + 1, currentPosition.y);
                tryAddNeighbour(currentPosition.x, currentPosition.y - 1);
                tryAddNeighbour(currentPosition.x - 1, currentPosition.y);

                if (!verticalMovement)
                    continue;

                tryAddNeighbour(currentPosition.x + 1, currentPosition.y + 1);
                tryAddNeighbour(currentPosition.x + 1, currentPosition.y - 1);
                tryAddNeighbour(currentPosition.x - 1, currentPosition.y - 1);
                tryAddNeighbour(currentPosition.x - 1, currentPosition.y + 1);
            }

            foreach (T node in closed)
                path.Add(node.Position);
        }
    }

    public interface IHeapable<T> : IComparable<T> where T : class
    {
        Vector2Int Parent { get; set; }
        Vector2Int Position { get; set; }

        // I'm using a heap instead of a stack for performance reasons
        int HeapIndex { get; set; }

        // Pathfinding
        float G { get; set; }
        float H { get; set; }

        bool Walkable { get; }
    }
}
