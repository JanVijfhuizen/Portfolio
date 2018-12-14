using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Jai
{

    public class Jai<T> where T : Move, new()
    {
        public enum EndGameState { None = -1, Draw = 1, Won = 2, Lost = 0 }
        public int GamesPlayed { get; private set; }

        public GameState<T> State { get; private set; }
        private Move rootMove;

        // NNet
        private JNNetData nnetData;
        private List<JNNet> generation, trained, untrained;
        private JNNet BestNNet
        {
            get
            {
                return generation[0];
            }
        }

        private System.Random random;
        private int id;

        #region Public Settings
        public int cycleAmount;
        public bool train = true;
        #endregion

        public Jai(GameState<T> state, System.Random random, JNNetData nnetData, int id)
        {
            State = state;
            SetRoot(state.GetLastMove());

            this.nnetData = nnetData;
            this.random = random;
            this.id = id;

            generation = new List<JNNet>(nnetData.generationSize);
            trained = new List<JNNet>(nnetData.generationSize);
            untrained = new List<JNNet>(nnetData.generationSize);

            // Initialize NNets
            for (int nnet = 0; nnet < nnetData.generationSize; nnet++)
                generation.Add(new JNNet(nnetData.structure, ref random));

            // Initialize training
            foreach (JNNet nnet in generation)
                untrained.Add(nnet);
        }

        // every score is the same
        #region Next Cache
        private JNNet bestNnet;
        private Move bestMove;
        #endregion

        private Move Next()
        {
            bestNnet = BestNNet;

            double score = double.NegativeInfinity, currentScore;
            foreach (Move move in State.GetChildren())
            {
                State.DoMove(move);
                currentScore = bestNnet.Next(State.GetInput())[0];

                if (currentScore > score)
                {
                    score = currentScore;
                    bestMove = move;
                }
                State.UndoMoves(1);
            }

            return bestMove;
        }

        public void SetRoot(Move move)
        {
            rootMove = move.GetClone();
            rootMove.Explored = false;
        }

        public T Call()
        {
            Move bestMove = null;
            int visits = -1;

            foreach (Move move in rootMove.Children)
                if (move.NumberOfVisits > visits)
                {
                    bestMove = move;
                    visits = move.NumberOfVisits;
                }
            return bestMove as T;
        }

        #region MCTS
        public void Cycle()
        {
            for (int cycleNum = 0; cycleNum < cycleAmount; cycleNum++)
                CycleOnce();
        }

        #region Cycle Cache
        private List<Move> explored = new List<Move>(), currentChildren;
        #endregion

        public void CycleOnce()
        {
            explored.Clear();

            // Dig until leaf
            Move current = rootMove;
            while (current.Explored)
            {
                if (current.Children.Count == 0)
                    break;

                current = Pick(current.Children);
                explored.Add(current);
            }

            current.SetExplored();

            // Update board
            foreach (Move move in explored)
            {
                if (train)
                    TrainNetwork(move);
                State.DoMove(move);
            }

            // If endgame has been reached
            EndGameState endGameState = State.EndGameState(id);
            if (endGameState != EndGameState.None)
            {
                foreach (Move move in explored)
                    Update(move, (int)endGameState);
                current.Children = new List<Move>();
                State.UndoMoves(explored.Count);
                return;
            }

            currentChildren = State.GetChildren();

            current.Children = new List<Move>(currentChildren.Count);
            foreach (Move move in currentChildren)
                current.Children.Add(move.GetClone());

            // Do rollout
            int rollOut;
            foreach (Move child in current.Children)
            {
                rollOut = RollOut(child);
                Update(child, rollOut);

                foreach (Move move in explored)
                    Update(move, rollOut);
            }

            State.UndoMoves(explored.Count);
        }

        private double UCT(Move move)
        {
            double uct = move.TotalValue / (move.NumberOfVisits + Mathf.Epsilon) +
                            Mathf.Sqrt(Mathf.Log(move.NumberOfVisits + 1) / (move.NumberOfVisits + Mathf.Epsilon))
                            + random.NextDouble() * Mathf.Epsilon;
            return uct;
        }
        #endregion

        #region NNet
        #region Train Network Cache
        private double[] input;
        #endregion
        private void TrainNetwork(Move move)
        {
            // If there is just one option the training will always reward the NNet
            if (move.Children == null)
                return;
            if (move.Children.Count < 2)
                return;

            if (untrained.Count == 0)
                UpdateGeneration();

            JNNet nnet = untrained[random.Next(0, untrained.Count - 1)];
            untrained.Remove(nnet);
            trained.Add(nnet);

            Move nnetMove = null, mostVisitedMove = null;
            double score = double.NegativeInfinity, childScore;
            int mostVisitedTimes = int.MinValue;

            input = State.GetInput();
            foreach (Move child in move.Children)
            {
                childScore = nnet.Next(input)[0];
                if (childScore > score)
                {
                    nnetMove = child;
                    score = childScore;
                }

                if (child.NumberOfVisits > mostVisitedTimes)
                {
                    mostVisitedMove = child;
                    mostVisitedTimes = child.NumberOfVisits;
                }
            }

            // Give it a point if it chose the most explored route
            if (mostVisitedMove == nnetMove)
                nnet.Score = 1;
            else
                nnet.Score = 0;
        }

        #region UpdateGeneration Cache
        List<JNNet> parents = new List<JNNet>();
        #endregion

        private void UpdateGeneration()
        {
            trained.Clear();
            generation = generation.OrderBy(x => -x.Score).ToList();

            // Add completely new
            int newIndex = nnetData.victors + nnetData.children;
            for (int nnet = newIndex; nnet < nnetData.generationSize; nnet++)
                generation[nnet].ReInitialize(ref random);

            // Add children and mutate
            int childrenIndex = nnetData.victors;
            for (int nnet = childrenIndex; nnet < newIndex; nnet++)
            {
                parents.Clear();
                for (int parent = 0; parent < nnetData.familySize; parent++)
                    parents.Add(generation[random.Next(0, nnetData.victors - 1)]);

                generation[nnet].Transform(parents, ref random);
                generation[nnet].Mutate(ref random, nnetData.mutationChance);
            }

            // Reset training
            foreach (JNNet nnet in generation)
                untrained.Add(nnet);
        }
        #endregion

        #region MCTS + NNet   
        private Move Pick(List<Move> children)
        {
            Move pick = children[0];
            double bestScore = double.NegativeInfinity, score;

            foreach (Move child in children)
            {
                score = UCT(child);

                if (score > bestScore)
                {
                    bestScore = score;
                    pick = child;
                }
            }

            return pick;
        }

        private int RollOut(Move move)
        {
            GamesPlayed++;

            State.DoMove(move);
            int movesMade = 1;

            while (State.EndGameState(id) == EndGameState.None)
            {
                State.DoMove(Next().GetClone());
                movesMade++;
            }

            EndGameState endGameState = State.EndGameState(id);
            State.UndoMoves(movesMade);

            return (int)endGameState;
        }

        private void Update(Move move, int rollOut)
        {
            move.AddVisit();
            move.AddValue(rollOut);
        }
        #endregion
    }

    public abstract class GameState<T> where T : Move, new()
    {
        protected List<Move> moves = new List<Move>();

        public virtual void DoMove(Move move)
        {
            moves.Add(move);
        }

        public virtual void UndoMoves(int amount)
        {
            int movesCount = moves.Count - 1;
            for (int move = 0; move < amount; move++)
                moves.RemoveAt(movesCount - move);
        }

        public abstract Jai<T>.EndGameState EndGameState(int id);

        public Move GetLastMove()
        {
            try
            {
                return moves[moves.Count - 1];
            }
            catch
            {
                return new T();
            }
        }
        // Where the children have to be in one list
        public abstract List<Move> GetChildren();
        public abstract double[] GetInput();
    }

    public class Move
    {
        public int NumberOfVisits { get; private set; }
        public int TotalValue { get; private set; }
        public bool Explored { get; set; }
        public List<Move> Children { get; set; }

        public void AddVisit()
        {
            NumberOfVisits++;
        }

        public void AddValue(int val)
        {
            TotalValue += val;
        }

        public void SetExplored()
        {
            Explored = true;
        }

        public virtual Move GetClone()
        {
            Move clone = new Move();
            SetCloneInformation(clone);
            return clone;
        }

        protected virtual void SetCloneInformation(Move clone)
        {
            clone.NumberOfVisits = NumberOfVisits;
            clone.TotalValue = TotalValue;
            clone.Explored = Explored;
        }
    }

    [Serializable]
    public class JNNetData
    {
        public int[] structure;
        public int generationSize, victors, children, familySize;
        public double mutationChance;
    }
}