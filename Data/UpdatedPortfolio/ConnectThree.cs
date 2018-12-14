using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Jai;

public class ConnectThree : MonoBehaviour {

    #region Data Structures
    public class Grid : GameState<MyMove>
    {
        public Node[,] nodes = new Node[3, 3];
        public bool FirstPlayerActive { get; private set; }

        private List<MyMove> childMoves;

        public Grid()
        {
            childMoves = new List<MyMove>(9);
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    childMoves.Add(new MyMove());
            FirstPlayerActive = true;
        }

        #region End Game State Cache
        private Node node;
        private bool fit;
        #endregion

        public override Jai<MyMove>.EndGameState EndGameState(int id)
        {
            bool isFirstPlayer = id == 0;

            Func<Jai<MyMove>.EndGameState> getEndGameState = delegate ()
            {
                if (isFirstPlayer == FirstPlayerActive)
                    return Jai<MyMove>.EndGameState.Lost;
                return Jai<MyMove>.EndGameState.Won;
            };

            // Horizontal
            for (int y = 0; y < 3; y++)
            {
                node = nodes[0, y];
                fit = true;

                if (node == Node.None)
                    continue;

                for (int x = 1; x < 3; x++)
                    if (nodes[x, y] != node)
                    {
                        fit = false;
                        break;
                    }

                if (fit)
                    return getEndGameState();
            }
            
            // Vertical
            for (int x = 0; x < 3; x++)
            {
                node = nodes[x, 0];
                fit = true;

                if (node == Node.None)
                    continue;

                for (int y = 1; y < 3; y++)
                    if (nodes[x, y] != node)
                    {
                        fit = false;
                        break;
                    }

                if (fit)
                    return getEndGameState();
            }

            // Vertical 1
            node = nodes[0, 0];
            if (node != Node.None)
            {
                fit = true;
                for (int d = 1; d < 3; d++)
                    if (nodes[d, d] != node)
                    {
                        fit = false;
                        break;
                    }

                if (fit)
                    return getEndGameState();
            }

            // Vertical 2
            node = nodes[2, 0];
            if (node != Node.None)
            {
                fit = true;
                for (int d = 1; d < 3; d++)
                    if (nodes[2 - d, d] != node)
                    {
                        fit = false;
                        break;
                    }

                if (fit)
                    return getEndGameState();
            }

            if (moves.Count == 9)
                return Jai<MyMove>.EndGameState.Draw;
            return Jai<MyMove>.EndGameState.None;
        }

        #region Get Children Cache
        private List<Move> children = new List<Move>(9);
        private MyMove move;
        #endregion

        public override List<Move> GetChildren()
        {
            int indexMoves = 0;
            children.Clear();

            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    if (nodes[x, y] == Node.None)
                    {
                        move = childMoves[indexMoves];
                        move.x = x;
                        move.y = y;
                        children.Add(move);
                        indexMoves++;
                    }

            return children;
        }

        #region Input Cache
        private double[] input = new double[9];
        #endregion

        public override double[] GetInput()
        {
            Node node;
            int index;
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                {
                    node = nodes[x, y];
                    index = x + y * 3;
                    switch (node)
                    {
                        case Node.None:
                            input[index] = .5f;
                            break;
                        case Node.P1:
                            input[index] = 1;
                            break;
                        case Node.P2:
                            input[index] = 0;
                            break;
                    }
                }
            return input;
        }

        #region Move Cache
        private MyMove myMove;
        #endregion

        public override void DoMove(Move move)
        {
            myMove = move as MyMove;
            nodes[myMove.x, myMove.y] = FirstPlayerActive ? Node.P1 : Node.P2;           
            base.DoMove(move);
            FirstPlayerActive = !FirstPlayerActive;
        }

        public override void UndoMoves(int amount)
        {
            int movesCount = moves.Count - 1;
            for (int move = 0; move < amount; move++)
            {
                myMove = moves[movesCount - move] as MyMove;
                nodes[myMove.x, myMove.y] = Node.None;
                moves.RemoveAt(movesCount - move);
                FirstPlayerActive = !FirstPlayerActive;
            }
        }
    }

    public class MyMove : Move
    {
        public int x, y;

        public override Move GetClone()
        {
            MyMove clone = new MyMove() { x = x, y = y };
            SetCloneInformation(clone);
            return clone;
        }
    }

    public enum Node {None, P1, P2 }
    #endregion

    [SerializeField]
    private JNNetData data;
    private Jai<MyMove> p1, p2, currentAI;
    [SerializeField]
    private int p1CycleAmount, p2CycleAmount;
    private Grid gameState = new Grid();

    [SerializeField]
    private string seed = "Zaad";

    private Thread thread;

    private void Awake()
    {
        p1 = new Jai<MyMove>(gameState, new System.Random(seed.GetHashCode()), data, 0);
        p2 = new Jai<MyMove>(gameState, new System.Random(seed.GetHashCode()), data, 1);

        p1.cycleAmount = p1CycleAmount;
        p2.cycleAmount = p2CycleAmount;

        SwitchAI();
    }

    private void SwitchAI()
    {
        bool firstPlayerActive = currentAI == p1;
        currentAI = firstPlayerActive ? p2 : p1;

        if (firstPlayerActive)
            thread = new Thread(p2.Cycle);
        else
            thread = new Thread(p1.Cycle);
        thread.Start();
    }

    private void Update()
    {
        if (!thread.IsAlive)
            if(gameState.EndGameState(0) == Jai<MyMove>.EndGameState.None)
            {
                MyMove move = currentAI.Call();

                gameState.DoMove(move);

                p1.SetRoot(move);
                p2.SetRoot(move);

                Debug.Log(move.x + " " + move.y + " " + currentAI.GamesPlayed);

                SwitchAI();
            }
    }
}
