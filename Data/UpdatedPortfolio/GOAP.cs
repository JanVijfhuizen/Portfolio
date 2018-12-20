using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    public class GOAP<T> where T : IGOAPable
    {
        /// <summary>
        /// A chain of actions that might lead to the solution
        /// </summary>
        private class Path : IComparable<Path>
        {
            public T First
            {
                get
                {
                    return Actions[0];
                }
            }

            public T Last
            {
                get
                {
                    return Actions[length];
                }
            }

            // I use a string to compare paths instead of comparing their actions, since this is way cheaper
            public string PathString { get; private set; }
            // The inefectiveness of the current path
            public float Cost { get; private set; }
            public List<T> Actions { get; private set; }

            // The states this path will unlock
            private List<string> achievedStatesList;
            // A hashset with the same content as the list above, it's just cheaper to compare to
            private HashSet<string> achievedStatesHashset = new HashSet<string>();
            // Cache variable for the length of the path
            private int length = -1;

            public Path(int actionsSize, int statesSize)
            {
                Actions = new List<T>(actionsSize);
                achievedStatesList = new List<string>(statesSize);
            }

            /// <summary>
            /// Transform this path into a copy of another path
            /// </summary>
            /// <param name="other"></param>
            public void Transform(Path other)
            {
                PathString = other.PathString;
                Cost = other.Cost;
                length = other.length;

                foreach (T action in other.Actions)
                    Actions.Add(action);
                foreach (string state in other.achievedStatesList)
                    achievedStatesList.Add(state);
                foreach (string state in other.achievedStatesHashset)
                    achievedStatesHashset.Add(state);
            }

            /// <summary>
            /// Add an action to the action list
            /// </summary>
            /// <param name="action"></param>
            public void Add(T action)
            {
                Actions.Add(action);
                PathString = string.Format("{0}{1}{2}", PathString, "_", action.Tag);
                Cost += action.Cost;
                length++;

                // Add states
                foreach (string state in action.Delivers)
                    if (!achievedStatesHashset.Contains(state))
                    {                  
                        achievedStatesList.Add(state);
                        achievedStatesHashset.Add(state);
                    }

                // Remove states
                foreach(string state in action.Removes)
                    if(achievedStatesHashset.Contains(state))
                    {
                        achievedStatesList.Remove(state);
                        achievedStatesHashset.Remove(state);                      
                    }
            }

            public bool Contains(T action)
            {
                return Actions.Contains(action);
            }

            /// <summary>
            /// Checks whether or not the path has unlocked required states
            /// </summary>
            /// <param name="states"></param>
            /// <returns></returns>
            public bool Contains(List<string> states)
            {
                foreach (string state in states)
                    if (!achievedStatesHashset.Contains(state))
                        return false;
                return true;
            }

            /// <summary>
            /// Reset the path so it may be pooled again later
            /// </summary>
            public void Clear()
            {
                PathString = "";
                Cost = 0;
                length = -1;

                Actions.Clear();
                achievedStatesList.Clear();
                achievedStatesHashset.Clear();
            }

            public int CompareTo(Path other)
            {
                return Mathf.RoundToInt(Cost - other.Cost);
            }
        }

        // All the possible actions the ai can take
        public T[] Actions { get; private set; }

        public GOAP(T[] actions, string[] allStates)
        {
            Actions = actions;

            int maxSize = Mathf.RoundToInt(Mathf.Pow(actions.Length, 2)),
                allStatesLength = allStates.Length;

            open = new Stack<Path>(maxSize);
            closedList = new List<Path>(maxSize);
            remainingStates = new List<string>(allStatesLength);

            pathPool = new Queue<Path>(maxSize);
            for (int i = 0; i < maxSize; i++)
                pathPool.Enqueue(new Path(maxSize, allStatesLength));
        }

        // Remaining paths to explore
        private Stack<Path> open;
        // Completed paths
        private List<Path> closedList;
        // Used to compare, is cheaper
        private HashSet<string> openHashSet = new HashSet<string>(),
            closedHashSet = new HashSet<string>();
        // Used in next to check which states are not in the current world state
        private List<string> remainingStates;
        // A pool to prevent huge amounts of garbage being created
        private Queue<Path> pathPool;

        public void Next(string wantedState, List<string> worldState, ref List<T> chosenPath)
        {
            Path pooledPath, currentPath;
            string pathString;

            Func<List<string>, List<string>, bool> contains = delegate (List<string> wanted, List<string> states)
            {
                foreach (string required in wanted)
                    if (!states.Contains(required))
                        return false;
                return true;
            };

            Action<T> addPath = delegate (T action)
            {
                pooledPath = pathPool.Dequeue();
                pooledPath.Add(action);
                openHashSet.Add(pooledPath.PathString);
                open.Push(pooledPath);
            };

            Action<T, Path> addExpandedPath = delegate (T action, Path path)
            {
                pooledPath = pathPool.Dequeue();
                pooledPath.Transform(path);
                pooledPath.Add(action);
                openHashSet.Add(pooledPath.PathString);
                open.Push(pooledPath);
            };

            // Get initial starters
            foreach (T action in Actions)
                if (action.Executable)
                    if (contains(action.Requires, worldState))
                        addPath(action);

            // As long as there are paths to explore
            while (open.Count > 0)
            {
                currentPath = open.Pop();
                openHashSet.Remove(currentPath.PathString);

                // If path has been found
                if (currentPath.Last.Delivers.Contains(wantedState))
                {
                    closedList.Add(currentPath);
                    closedHashSet.Add(currentPath.PathString);
                    continue;
                }

                // Get all children
                foreach (T action in Actions)
                {
                    if (!action.Executable)
                        continue;

                    pathString = string.Format("{0}{1}{2}", currentPath.PathString, "_", action.Tag);

                    if (closedHashSet.Contains(pathString) || openHashSet.Contains(pathString))
                        continue;
                    if (currentPath.Contains(action))
                        continue;

                    remainingStates.Clear();

                    foreach (string state in action.Requires)
                        if (!worldState.Contains(state))
                            remainingStates.Add(state);

                    if (!currentPath.Contains(remainingStates))
                        continue;

                    addExpandedPath(action, currentPath);
                }
            }

            closedList.Sort();

            List<T> bestPath = closedList[0].Actions;
            foreach (T action in bestPath)
                chosenPath.Add(action);

            openHashSet.Clear();
            closedHashSet.Clear();

            foreach (Path path in closedList)
            {
                path.Clear();
                pathPool.Enqueue(path);
            }

            closedList.Clear();
        }
    }

    public interface IGOAPable
    {
        string Tag { get; }
        float Cost { get; }
        bool Executable { get; }
        List<string> Requires { get; }
        List<string> Delivers { get; }
        List<string> Removes { get; }
    }
}