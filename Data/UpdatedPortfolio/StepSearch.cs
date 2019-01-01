using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace StepSearch
{
    /// <summary>
    /// This AI is based off of both MiniMax and MCTS, where it uses the min-max idea of MiniMax but uses the UCT formula used in MCTS to search the tree.
    /// The advantages of this treesearch are that it does not have a rollout phase and it is able to detect traps.
    /// </summary>
    /// <typeparam name="T">Game State</typeparam>
    /// <typeparam name="U">Move</typeparam>
    public class StepSearch<T, U> where T : IState<U> where U : ISteppable<U>
    {
        private T state;
        private int id;

        public StepSearch(T state, int maxTurns, int id, Random random)
        {
            this.state = state;
            this.id = id;
            this.random = random;

            open = new List<U>(maxTurns + 1);
            open.Add(state.GetLastMove());
        }

        private List<U> open;
        private Random random;

        /// <summary>
        /// This returns what the AI thinks is the best move
        /// </summary>
        /// <returns></returns>
        public U Next()
        {
            List<U> children = open[0].Children;
            U best = children[0];
            int bestVisitedAmount = best.VisitedAmount;
            int count = children.Count;
            for (int i = 1; i < count; i++)
                if (children[i].VisitedAmount > bestVisitedAmount)
                {
                    best = children[i];
                    bestVisitedAmount = best.VisitedAmount;
                }

            return best;
        }

        /// <summary>
        /// This cleans up the state, undoing all the moves in open
        /// </summary>
        public void CleanState()
        {
            int openCount = open.Count - 1;
            for (int i = openCount; i > 0; i--)
            {
                state.UndoMove();
                open.RemoveAt(i);
            }
        }

        /// <summary>
        /// This expands the tree and explores it
        /// </summary>
        public void Cycle()
        {
            int index = open.Count - 1;
            U best, current, parent;
            double currentScore, bestScore;

            // Check if this is still the best move to explore compared to it's siblings
            if (index > 0)
            {
                current = open[index];
                parent = open[index - 1];

                // This is used to compare ancestors when this is the only child
                int parentIndex = index - 1;
                while (parent.Children.Count == 1 && parentIndex > 0)
                {
                    parentIndex--;
                    parent = open[parentIndex];
                }

                best = GetUCTChild(parent.Children);

                currentScore = UCT(current);
                bestScore = UCT(best);

                if (currentScore < bestScore)
                {
                    // Step UP
                    state.UndoMove();
                    open.RemoveAt(index);

                    Cycle();
                    return;
                }
            }

            // Step DOWN
            current = open[open.Count - 1];
            while (current.Children != null)
            {
                // If this is the endgame
                if (current.Children.Count == 0)
                {
                    BackPropagate(true);
                    return;
                }

                best = GetUCTChild(current.Children);
                state.DoMove(best);
                open.Add(best);
                current = best;
            }

            // Get values for children and update every move in open
            current.Children = state.GetChildren();
            foreach (U child in current.Children)
            {
                state.DoMove(child);
                open.Add(child);

                child.PlayerID = state.GetActivePlayerID();
                BackPropagate(false);

                state.UndoMove();
                open.Remove(child);
            }
        }

        /// <summary>
        /// Adds the score of the current path to each move in open
        /// </summary>
        /// <param name="endgame"></param>
        private void BackPropagate(bool endgame)
        {
            U lastMove = open[open.Count - 1];
            float score = state.GetScore(lastMove.PlayerID);

            // Normally this follows MiniMax's rules where it would select the best for both you and your opponent,
            // But in the endgame (final turn) it needs to be very clear for the AI that we actually want to win
            // So in the endgame you return whether you won or not
            if (endgame)
                score = state.GetWinnerID() == id ? 1 : 0;

            foreach (U move in open)
            {
                move.Score += score;
                move.VisitedAmount++;
            }
        }

        /// <summary>
        /// Returns the child with the highest UCT value
        /// </summary>
        /// <param name="children"></param>
        /// <returns></returns>
        private U GetUCTChild(List<U> children)
        {
            int childrenCount = children.Count;
            double bestScore = double.NegativeInfinity, currentScore;
            U bestChild = children[0];

            foreach (U child in children)
            {
                currentScore = UCT(child);
                if (currentScore < bestScore)
                    continue;

                bestChild = child;
                bestScore = currentScore;
            }

            return bestChild;
        }

        /// <summary>
        /// This is a formula that decides how important it is to search this move
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        private double UCT(U move)
        {
            float powerValue = move.Score;
            int visitedAmount = move.VisitedAmount;
            double uct = powerValue / (visitedAmount + Mathf.Epsilon) +
                            Mathf.Sqrt(Mathf.Log(visitedAmount + 1) / (visitedAmount + Mathf.Epsilon))
                            + random.NextDouble() * Mathf.Epsilon;
            return uct;
        }
    }

    /// <summary>
    /// This is used for game states / board states
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IState<T> where T : ISteppable<T>
    {
        int GetActivePlayerID();
        int GetWinnerID();
        void DoMove(T move);
        void UndoMove();
        T GetLastMove();
        float GetScore(int id);
        List<T> GetChildren();
    }

    /// <summary>
    /// This is used for moves
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISteppable<T>
    {
        int PlayerID { get; set; }
        int VisitedAmount { get; set; }
        float Score { get; set; }
        List<T> Children { get; set; }
    }
}