using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class JSolver<T, U> where T : class, ISolvable<T> where U : class, IMoveable<U> {

    // Task level

    /*
        follow the flow of the game until turncount has been reached, then try to keep the last values of the flow

        Select based on offset between turns and target flow

        rate "us" based on win amount + epsilon / games played

        backpropagate "them" based on offset from player database

        ask root whether it is "us" or "them" when using rollout
    */

    [Serializable]
    public struct JSolverData
    {
        public string seed;
        public AnimationCurve[] flow;
        public int turnCount;
        public GeneticAlgorithm<NNet, NNet.NNetData>.GeneticAlgorithmData geneticAlgorithmData;
        public NNet.NNetData neuralNetworkData;

        public int visitedListCacheLength;
    }

    private JSolverData data;
    private System.Random random;

    private ISolvable<U> rootState;

    private GeneticAlgorithm<NNet, NNet.NNetData> us;
    private NNet them = new NNet();

    #region Cache
    private List<U> visited;
    private double[] nnetInput, nnetOutput;
    #endregion

    public JSolver(JSolverData data)
    {
        this.data = data;
        random = new System.Random(data.seed.GetHashCode());

        us = new GeneticAlgorithm<NNet, NNet.NNetData>(data.geneticAlgorithmData, data.neuralNetworkData);
        them.Initialize(data.neuralNetworkData);

        visited = new List<U>(data.visitedListCacheLength);
    }

    public void CycleOnce()
    {
        U current = rootState.GetLastMove();

        visited.Clear();

        // Move to leaf
        while(current.Children != null)
        {
            if (current.Children.Count == 0)
                break;

            visited.Add(current);
            current = Select(current.Children);
        }

        foreach (U move in visited)
            rootState.DoMove(move);

        current.Children = rootState.GetPossibleMoves();

        
    }

    private U Select(List<U> moves)
    {
        int movesCount = moves.Count;

        int bestIndex = 0;
        double bestScore = double.NegativeInfinity;

        U current;

        double totalOffset;
        int visitedAmount;
        double averageOffset;

        double score;

        for (int i = 0; i < movesCount; i++)
        {
            current = moves[i];

            totalOffset = current.TotalOffset;
            visitedAmount = current.VisitedAmount;
            averageOffset = totalOffset / visitedAmount;

            score = UCT(current);

            if (score > bestScore)
            {
                bestIndex = i;
                bestScore = score;               
            }
        }

        return moves[bestIndex];
    }

    // Rate offset average of path 0-1
    private double UCT(U move)
    {
        double uct = move.TotalOffset / (move.VisitedAmount + Mathf.Epsilon) +
                        Mathf.Sqrt(Mathf.Log(move.VisitedAmount + 1) / (move.VisitedAmount + Mathf.Epsilon))
                        + random.NextDouble() * Mathf.Epsilon;
        return uct;
    }
}

#region Interfaces

public interface ISolvable<T>
{
    T GetLastMove();
    List<T> GetPossibleMoves();
    void DoMove(T move);
    void UndoMove();

    double[] GetInput(); 
}

public interface IMoveable<T>
{
    int VisitedAmount { get; set; }
    int TotalWinAmount { get; set; }
    double TotalOffset { get; set; }

    List<T> Children { get; set; }
}

#endregion