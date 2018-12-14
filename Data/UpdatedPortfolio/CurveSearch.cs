using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NEAT;

namespace CurveSearchLib
{
    [Serializable]
    public struct CurveSearchData
    {
        public bool playerOne;
        // This is used to steer the game in a certain direction
        public Flow[] gameflow;
        public string seed;
        // The inputcount is the amount of inputs from your game
        // The depth is the rollout length, the amount of turns to look ahead
        public int gameMaxDuration, inputCount, depth;
    }

    public class CurveSearch<T, U, V, W> where T : State<U, V> where U : Move, new() where V : IStrategy<W>
    {
        // References
        private CurveSearchData data;
        private List<V> strategies;
        private System.Random random;

        // The game state / board / environment
        private T state;
        // Whether or not the ai is player one
        public bool PlayerOne { get; set; }

        // The maximum amount of offset possible
        private int offsetNormalizer;

        // Used in cycle to determine current path
        private Stack<Move> open;
        // The current flow of the game
        private double[] currentGameflow;

        public CurveSearch(T state, CurveSearchData data, List<V> strategies)
        {
            this.data = data;
            this.strategies = strategies;
            this.state = state;

            PlayerOne = data.playerOne;

            random = new System.Random(data.seed.GetHashCode());

            state.Input = new double[data.inputCount];

            open = new Stack<Move>(data.gameMaxDuration);

            // Initialize strategies, for instance when a strategy needs an excel file
            foreach (V strategy in strategies)
                (strategy as IStrategy<W>).Init();

            // Initialize offsetNormalizer
            foreach (Flow flow in data.gameflow)
                offsetNormalizer += flow.importance;
        }
        
        // Pick and do the chosen move
        public U GetAndSetNext()
        {
            Move move = null;
            int visitedAmount = int.MinValue,
                openCount = open.Count;
            List<Move> children = state.LastMove.GetChildren();

            for (int i = 0; i < openCount; i++)
                state.UndoMove();
            open.Clear();

            // If child x's gameflow matches the wanted gameflow better then best child, best child is child x
            foreach (Move child in children)
            {
                if (child.CurveVisitedAmount > visitedAmount)
                {
                    visitedAmount = child.CurveVisitedAmount;
                    move = child;
                }

                // If opponent loses next turn because of this move, automatically pick this move
                if (child.State == Move.GameState.Lost)
                {
                    move = child;
                    break;
                }
            }

            state.LastMove = move;
            state.DoMove(move as U);
            return move as U;
        }
        
        // Undo all moves done while calculating
        public void UndoAllMovesInOpen()
        {
            int openCount = open.Count;
            for (int i = 0; i < openCount; i++)
                state.UndoMove();
            open.Clear();
        }

        // Gather information to dedide what move should be picked with GetAndSetNext
        public void Cycle()
        {
            Move lastMove = state.LastMove;
            int openCount = open.Count;

            UndoAllMovesInOpen();

            // Go through the explored tree until a non explored move has been found
            while (lastMove.Explored)
            {
                if (lastMove.State != Move.GameState.Active)
                {
                    BackPropagate(0, 0);
                    return;
                }

                lastMove = MCTSPick(lastMove);

                state.DoMove(lastMove as U);
                open.Push(lastMove);
            }

            Move.GameState gameState = lastMove.State;

            // If the game has ended, update path and return
            if (gameState != Move.GameState.Active)
            {
                BackPropagate(0, 0);
                return;
            }

            // Set children
            lastMove.SetChildrenCache(state.GetPossibleMoves());

            // Rollout each child
            List<Move> children = lastMove.GetChildren();

            foreach(Move move in children)
            {
                open.Push(move);
                state.DoMove(move as U);

                // Update child data
                state.UpdateInput(strategies, state.IsPlayerOneActive());
                move.Input = state.Input;
                move.State = state.GetGameState();
                move.PlayerActive = state.IsPlayerOneActive();
                move.Offset = GetOffset();

                Rollout();

                state.UndoMove();
                open.Pop();
            }
        }

        #region Pick

        // Get the Move with the highest UCT value
        private Move MCTSPick(Move move)
        {
            List<Move> children = move.GetChildren();

            double highestUCT = double.NegativeInfinity,
                currentUCT;          
            Move picked = null;

            foreach (Move child in children)
            {
                currentUCT = UCT(child.CurvePower, child.CurveVisitedAmount);
                if (currentUCT > highestUCT)
                {
                    highestUCT = currentUCT;
                    picked = child;
                }
            }

            return picked;
        }

        // Used by MCTS to get the next child that will be explored
        private double UCT(double powerValue, int visitedAmount)
        {
            double uct = powerValue / (visitedAmount + Mathf.Epsilon) +
                            Mathf.Sqrt(Mathf.Log(visitedAmount + 1) / (visitedAmount + Mathf.Epsilon))
                            + random.NextDouble() * Mathf.Epsilon;
            return uct;
        }

        #endregion

        // Play a complete game while gathering data, and backpropagate at the end of the rollout
        private void Rollout()
        {
            int movesDone = 0, choice, movesToDo = data.depth - open.Count;
            double offset = 0;

            List<Move> possibleMoves;
            Move move;          

            possibleMoves = state.GetPossibleMoves();
            while(possibleMoves.Count > 0)
            {
                // If the maximum depth has been reached
                movesToDo--;
                if (movesToDo <= 0)
                    break;

                if (state.GetGameState() != Move.GameState.Active)
                    break;

                choice = random.Next(0, possibleMoves.Count - 1);
                move = possibleMoves[choice];

                state.DoMove(move as U);

                movesDone++;
                offset += GetOffset();

                possibleMoves = state.GetPossibleMoves();
            }

            BackPropagate(offset, movesDone);

            for (int i = 0; i < movesDone; i++)
                state.UndoMove();
        }

        // Get the difference between the current gameflow and 
        private double GetOffset()
        {
            int curveCount = data.gameflow.Length;
            float movesDoneIndex = state.MovesDoneIndex,
                flowImportance;
            double offset = 0;
            float curveValue = 0;
            Flow flow;
            AnimationCurve curve;

            state.UpdateInput(strategies, PlayerOne);
            currentGameflow = state.Input;

            for (int i = 0; i < curveCount; i++) {

                flow = data.gameflow[i];
                curve = flow.curve;
                flowImportance = (float)flow.importance / flow.indexes.Length;

                curveValue = curve.Evaluate(movesDoneIndex / data.gameMaxDuration);
                
                foreach(int index in flow.indexes)
                    offset += Mathf.Abs((float)currentGameflow[index] - curveValue) * flowImportance;
            }

            return offset / offsetNormalizer;
        }

        #region Backpropagation

        // Rate the path that has been followed based on the offset
        private void BackPropagate(double rolloutOffset, int rolloutLength)
        {
            double power = rolloutOffset;
            foreach (Move move in open)
                power += move.Offset;

            power /= rolloutLength + open.Count;
            power = 1 - power;

            foreach(Move move in open)
            {
                move.CurvePower += power;
                move.CurveVisitedAmount++;
            }
        }

        #endregion
    }

    // This is the class that the game state / board needs to inherit from
    public abstract class State<T, U> where T : Move
    {
        public abstract bool IsPlayerOneActive();

        // This is where the input will be saved
        public double[] Input { get; set; }
        // Each move done so far (this is a cache of default value T's)
        public List<T> MovesDone;
        // This is used to determine how large MovesDone actually is (the amount of used T's in MovesDone)
        public int MovesDoneIndex { get; private set; }

        public abstract Move.GameState GetGameState();
        // The last REAL move done
        public Move LastMove { get; set; }

        public abstract List<Move> GetPossibleMoves();

        public virtual void DoMove(T move)
        {
            MovesDone[MovesDoneIndex].Transform(move);
            MovesDoneIndex++;
        }

        public virtual void UndoMove()
        {
            MovesDoneIndex--;
        }

        // Updating it manually is cheaper, because input is requested more often than the state changes
        public virtual void UpdateInput(List<U> strategies, bool playerOne)
        {
            int count = strategies.Count;

            for (int i = 0; i < count; i++)
            {
                Input[i] = GetStrategyPower(strategies[i], playerOne);
                Input[i + 1] = GetStrategyPower(strategies[i], !playerOne);
            }
        }

        // Get a value between 0 and 1 based on how much the strategy is being used
        protected abstract double GetStrategyPower(U strategy, bool playerOne);
    }

    // This is the class that custom state moves will inherit from
    public abstract class Move
    {
        // A lot of data is saved in this class
        // Otherwise all the data needs to be requested each time the state is updated,
        // even though the data is used before

        public enum GameState { Active, Lost }

        public bool Explored { get; private set; }
        public GameState State { get; set; } = GameState.Active;
        public bool PlayerActive { get; set; }

        // Curve data
        public int CurveVisitedAmount { get; set; }
        public double CurvePower { get; set; }
        public double Offset { get; set; }

        public double[] Input { get; set; }

        protected List<Move> childrenCache;

        public List<Move> GetChildren()
        {
            return childrenCache;
        }

        public void SetChildrenCache(List<Move> children)
        {
            childrenCache = new List<Move>(children.Count);
            foreach (Move move in children)
                childrenCache.Add(move.GetCopy());
            Explored = true;
        }

        public abstract Move GetCopy();
        public abstract void Transform(Move other);
    }

    // This is used for strategies
    public interface IStrategy<T>
    {
        T Core { get; }
        T[] Required { get; }
        T[][] Mutations { get; }

        void Init();
    }
}