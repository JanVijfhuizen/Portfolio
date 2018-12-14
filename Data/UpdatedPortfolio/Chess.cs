using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CurveSearchLib;
using System.IO;

namespace ChessLib
{
    public class Chess
    {
        public abstract class Piece
        {
            public enum LineType { Horizontal, Vertical, Diagonal1, Diagonal2 }

            public bool PlayerOne { get; set; }
            public Vector2Int Position { get; set; }
            public int MovesDone { get; set; }

            protected List<ChessMove> movesCache = new List<ChessMove>(28);
            // Used to determine how much moves will be returned from movesCache
            protected int movesCacheSize;

            protected void Init()
            {
                for (int i = 0; i < 28; i++)
                    movesCache.Add(new ChessMove());
            }

            public void GetMoves(List<Move> moves, Node[,] board)
            {
                SetMoves(board);
                for (int i = 0; i < movesCacheSize; i++)
                    moves.Add(movesCache[i]);
            }

            protected virtual void SetMoves(Node[,] board)
            {
                movesCacheSize = 0;
            }

            protected bool IsOutOfBounds(Vector2Int position)
            {
                return position.x < 0 || position.x >= 8 || position.y < 0 || position.y >= 8;
            }

            // Get line in single direction
            protected void GetLineMoves(int dis, bool positiveDir, bool canDestroy, LineType lineType, Node[,] board)
            {
                Vector2Int toPosition;
                Node node;
                int dirMul = positiveDir ? 1 : -1;
                
                for (int i = 1; i <= dis; i++)
                {
                    switch (lineType)
                    {
                        case LineType.Horizontal:
                            toPosition = new Vector2Int(Position.x + i * dirMul, Position.y);
                            break;
                        case LineType.Vertical:
                            toPosition = new Vector2Int(Position.x, Position.y + i * dirMul);
                            break;
                        case LineType.Diagonal1:
                            toPosition = new Vector2Int(Position.x + i * dirMul, Position.y + i * dirMul);
                            break;
                        case LineType.Diagonal2:
                            // doesnt work correctly
                            toPosition = new Vector2Int(Position.x + i * dirMul, Position.y + i * -dirMul);
                            break;
                        default:
                            toPosition = default(Vector2Int);
                            break;
                    }

                    if (IsOutOfBounds(toPosition))
                        break;

                    node = board[toPosition.x, toPosition.y];

                    if (!node.Empty)
                    {
                        if (canDestroy && node.Piece.PlayerOne != PlayerOne)
                            AddMove(toPosition);
                        break;
                    }

                    AddMove(toPosition);
                }
            }

            // Get line in both directions
            protected void GetLineMoves(int dis, bool canDestroy, LineType lineType, Node[,] board)
            {
                GetLineMoves(dis, true, canDestroy, lineType, board);
                GetLineMoves(dis, false, canDestroy, lineType, board);
            }

            // Get all lines in all direction
            protected void GetLineMoves(int dis, bool canDestroy, Node[,] board)
            {
                for (int i = 0; i < 4; i++)
                    GetLineMoves(dis, canDestroy, (LineType)i, board);
            }

            protected ChessMove move;
            protected void AddMove(Vector2Int to)
            {
                move = movesCache[movesCacheSize];

                move.From = Position;
                move.To = to;

                move.OnDo = CheckForOnDoAndOnUndo(move);

                movesCacheSize++;
            }

            protected virtual bool CheckForOnDoAndOnUndo(ChessMove move)
            {
                return false;
            }

            public virtual void OnDo()
            {

            }

            public virtual void OnUndo()
            {

            }
        }

        public class Pawn : Queen
        {
            bool transformed;

            public Pawn(Vector2Int position, bool playerOne) : base(position, playerOne)
            {
                Position = position;
                PlayerOne = playerOne;
                Init();
            }

            private void CheckDiagonalMovement(Node[,] board, int dir, int dirMil)
            {
                Vector2Int toPosition = new Vector2Int(Position.x + dir, Position.y + dirMil);
                Node node;

                if (IsOutOfBounds(toPosition))
                    return;

                node = board[toPosition.x, toPosition.y];

                if (node.Empty)
                    return;
                if (node.Piece.PlayerOne == PlayerOne)
                    return;

                AddMove(toPosition);
            }

            protected override void SetMoves(Node[,] board)
            {
                if (transformed)
                {
                    base.SetMoves(board);
                    return;
                }
                else
                    movesCacheSize = 0;

                int dirMil = PlayerOne ? 1 : -1;

                // Since a pawn has some weird behaviour I have to do this part manually
                CheckDiagonalMovement(board, 1, dirMil);
                CheckDiagonalMovement(board, -1, dirMil);

                GetLineMoves(MovesDone > 0 ? 1 : 2, PlayerOne, false, LineType.Vertical, board);
            }

            protected override bool CheckForOnDoAndOnUndo(ChessMove move)
            {
                return (move.To.y == 7 && PlayerOne) || (move.To.y == 0 && !PlayerOne);
            }

            public override void OnDo()
            {
                transformed = true;
            }

            public override void OnUndo()
            {
                transformed = false;
            }
        }

        public class Rook : Piece
        {
            public Rook(Vector2Int position, bool playerOne)
            {
                Position = position;
                PlayerOne = playerOne;
                Init();
            }

            protected override void SetMoves(Node[,] board)
            {
                base.SetMoves(board);

                GetLineMoves(7, true, LineType.Horizontal, board);
                GetLineMoves(7, true, LineType.Vertical, board);
            }
        }

        public class Knight : Piece
        {
            public Knight(Vector2Int position, bool playerOne)
            {
                Position = position;
                PlayerOne = playerOne;
                Init();
            }

            private void TryAddMove(Node[,] board, int offsetX, int offsetY)
            {
                Vector2Int toPosition = new Vector2Int(Position.x + offsetX, Position.y + offsetY);

                if (IsOutOfBounds(toPosition))
                    return;

                Node node = board[toPosition.x, toPosition.y];

                if (!node.Empty)
                    if (node.Piece.PlayerOne == PlayerOne)
                        return;

                AddMove(toPosition);
            }

            protected override void SetMoves(Node[,] board)
            {
                base.SetMoves(board);

                // Up
                TryAddMove(board, -1, 2);
                TryAddMove(board, 1, 2);

                // Right
                TryAddMove(board, 2, 1);
                TryAddMove(board, 2, -1);

                // Down
                TryAddMove(board, 1, -2);
                TryAddMove(board, -1, -2);

                // Left
                TryAddMove(board, -2, -1);
                TryAddMove(board, -2, 1);
            }
        }

        public class Bishop : Piece
        {
            public Bishop(Vector2Int position, bool playerOne)
            {
                Position = position;
                PlayerOne = playerOne;
                Init();
            }

            protected override void SetMoves(Node[,] board)
            {
                base.SetMoves(board);

                GetLineMoves(7, true, LineType.Diagonal1, board);
                GetLineMoves(7, true, LineType.Diagonal2, board);
            }
        }

        public class Queen : Piece
        {
            public Queen(Vector2Int position, bool playerOne)
            {
                Position = position;
                PlayerOne = playerOne;
                Init();
            }

            protected override void SetMoves(Node[,] board)
            {
                base.SetMoves(board);

                GetLineMoves(7, true, board);
            }
        }

        public class King : Piece
        {
            public King(Vector2Int position, bool playerOne)
            {
                Position = position;
                PlayerOne = playerOne;
                Init();
            }

            protected override void SetMoves(Node[,] board)
            {
                base.SetMoves(board);
                GetLineMoves(1, true, board);
            }
        }

        public class Node
        {
            public Piece Piece { get; set; }
            public bool Empty
            {
                get
                {
                    return Piece == null;
                }
            }
        }

        public class ChessBoard : State<ChessMove, ChessStrategy>
        {
            public Node[,] grid = new Node[8, 8];

            private List<Piece> playerOnePieces = new List<Piece>(16),
                playerTwoPieces = new List<Piece>(16);

            private List<Move> movesCache = new List<Move>(218),
                singlePieceMovesCache = new List<Move>(28);

            private double[,] pmPawn, pmRook, pmKnight, pmBishop, pmQueen, pmKing;

            public ChessBoard()
            {
                Action<List<Piece>> addPiecesToGrid = delegate (List<Piece> pieces)
                {
                    foreach (Piece piece in pieces)
                        grid[piece.Position.x, piece.Position.y].Piece = piece;
                };

                // Initialize base move
                LastMove = new ChessMove();

                // Initialize movesDone
                MovesDone = new List<ChessMove>(50);
                for (int i = 0; i < 50; i++)
                    MovesDone.Add(new ChessMove());

                // Initialize grid
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 8; y++)
                        grid[x, y] = new Node();

                LoadPositionalMaps();

                #region Add Chess Pieces

                // Add Pawns
                for (int i = 0; i < 8; i++)
                {
                    playerOnePieces.Add(new Pawn(new Vector2Int(i, 1), true));
                    playerTwoPieces.Add(new Pawn(new Vector2Int(i, 6), false));
                }

                // Add Rooks
                playerOnePieces.Add(new Rook(new Vector2Int(0, 0), true));
                playerOnePieces.Add(new Rook(new Vector2Int(7, 0), true));

                playerTwoPieces.Add(new Rook(new Vector2Int(0, 7), false));
                playerTwoPieces.Add(new Rook(new Vector2Int(7, 7), false));

                // Add Knights
                playerOnePieces.Add(new Knight(new Vector2Int(1, 0), true));
                playerOnePieces.Add(new Knight(new Vector2Int(6, 0), true));

                playerTwoPieces.Add(new Knight(new Vector2Int(1, 7), false));
                playerTwoPieces.Add(new Knight(new Vector2Int(6, 7), false));

                // Add Bishops
                playerOnePieces.Add(new Bishop(new Vector2Int(2, 0), true));
                playerOnePieces.Add(new Bishop(new Vector2Int(5, 0), true));

                playerTwoPieces.Add(new Bishop(new Vector2Int(2, 7), false));
                playerTwoPieces.Add(new Bishop(new Vector2Int(5, 7), false));

                // Add Queen
                playerOnePieces.Add(new Queen(new Vector2Int(3, 0), true));
                playerTwoPieces.Add(new Queen(new Vector2Int(3, 7), false));

                // Add King
                playerOnePieces.Add(new King(new Vector2Int(4, 0), true));
                playerTwoPieces.Add(new King(new Vector2Int(4, 7), false));

                #endregion

                addPiecesToGrid(playerOnePieces);
                addPiecesToGrid(playerTwoPieces);
            }

            #region Various Checks

            private bool HasKing(List<Piece> pieces)
            {
                foreach (Piece piece in pieces)
                    if (piece as King != null)
                        return true;
                return false;
            }

            public override Move.GameState GetGameState()
            {
                if (MovesDoneIndex >= 50)
                    return Move.GameState.Lost;

                List<Piece> pieces = IsPlayerOneActive() ? playerOnePieces : playerTwoPieces;

                if (!HasKing(pieces))
                    return Move.GameState.Lost;

                return Move.GameState.Active;
            }

            private void AddMoves(Piece piece)
            {
                singlePieceMovesCache.Clear();
                piece.GetMoves(singlePieceMovesCache, grid);
                foreach (Move move in singlePieceMovesCache)
                    movesCache.Add(move);
            }

            private void SetAllMoves(List<Piece> pieces)
            {
                movesCache.Clear();
                foreach (Piece piece in pieces)
                    AddMoves(piece);
            }

            public override List<Move> GetPossibleMoves()
            {
                SetAllMoves(IsPlayerOneActive() ? playerOnePieces : playerTwoPieces);
                return movesCache;
            }

            public override bool IsPlayerOneActive()
            {
                return MovesDoneIndex % 2 == 0;
            }

            #endregion

            #region Do and Undo Move

            public override void DoMove(ChessMove move)
            {
                Vector2Int from = move.From, to = move.To;

                if (!grid[to.x, to.y].Empty)
                {
                    move.Removes = grid[to.x, to.y].Piece;
                    if (move.Removes.PlayerOne)
                        playerOnePieces.Remove(move.Removes);
                    else
                        playerTwoPieces.Remove(move.Removes);
                }
                else
                    move.Removes = null;

                grid[to.x, to.y].Piece = grid[from.x, from.y].Piece;
                grid[from.x, from.y].Piece = null;

                Piece piece = grid[to.x, to.y].Piece;
                piece.Position = to;
                piece.MovesDone++;

                if (move.OnDo)
                    grid[to.x, to.y].Piece.OnDo();

                base.DoMove(move);
            }

            public override void UndoMove()
            {
                ChessMove move = MovesDone[MovesDoneIndex - 1];
                Vector2Int from = move.From, to = move.To;
                Piece removedPiece = move.Removes;

                grid[from.x, from.y].Piece = grid[to.x, to.y].Piece;
                grid[to.x, to.y].Piece = null;

                Piece piece = grid[from.x, from.y].Piece;
                piece.Position = from;
                piece.MovesDone--;

                // Add removed piece back on to the board
                if (removedPiece != null)
                {
                    Vector2Int position = removedPiece.Position;

                    if (removedPiece.PlayerOne)
                        playerOnePieces.Add(move.Removes);
                    else
                        playerTwoPieces.Add(move.Removes);

                    grid[position.x, position.y].Piece = removedPiece;
                }

                if (move.OnDo)
                    grid[from.x, from.y].Piece.OnUndo();

                base.UndoMove();
            }

            #endregion

            #region Valid Checks

            protected bool IsOutOfBounds(Vector2Int vec)
            {
                return vec.x < 0 || vec.x >= 8 || vec.y < 0 || vec.y >= 8;
            }

            private bool IsBeingUsed(ChessTactic[] tactics, Vector2Int from, bool playerOne)
            {
                Vector2Int vec;
                Node node;
                Type _type;

                int dir = playerOne ? 1 : -1;

                foreach (ChessTactic tactic in tactics)
                {
                    vec = new Vector2Int(from.x + tactic.relativePosition.x, from.y + tactic.relativePosition.y * dir);
                    if (IsOutOfBounds(vec))
                        return false;

                    node = grid[vec.x, vec.y];

                    if (node.Empty)
                        return false;

                    if (node.Piece.PlayerOne != playerOne)
                        return false;

                    _type = node.Piece.GetType();

                    if (!tactic.ContainsType(_type))
                        return false;
                }

                return true;
            }

            #endregion

            #region Get Power

            public override void UpdateInput(List<ChessStrategy> strategies, bool playerOne)
            {
                base.UpdateInput(strategies, playerOne);

                int count = strategies.Count * 2;

                SetMaterialPower(count, playerOne);
                SetMaterialPower(count + 6, !playerOne);

                SetPositionalPower(count + 12, playerOne);
                SetPositionalPower(count + 18, !playerOne);
            }

            
            private void SetMaterialPower(int startingIndex, bool playerOne)
            {
                // I'm using the general values of that are used in most chess AI
                List<Piece> pieces = playerOne ? playerOnePieces : playerTwoPieces;
                SetMaterialPowerPlayer(startingIndex, pieces);
            }

            private void SetMaterialPowerPlayer(int startingIndex, List<Piece> pieces)
            {
                Type type;

                for (int i = 0; i < 6; i++)
                    Input[i + startingIndex] = 0;

                foreach(Piece piece in pieces)
                {
                    type = piece.GetType();

                    if (type == typeof(Pawn))
                        Input[startingIndex] += .125f;
                    else
                        if (type == typeof(Rook))
                        Input[startingIndex + 1] += .5f;
                    else
                        if (type == typeof(Knight))
                        Input[startingIndex + 2] += .5f;
                    else
                        if (type == typeof(Bishop))
                        Input[startingIndex + 3] += .5f;
                    else
                        if (type == typeof(Queen))
                        Input[startingIndex + 4] += 1;
                    else
                        if (type == typeof(King))
                        Input[startingIndex + 5] += 1;
                }
            }

            private void SetPositionalPower(int startingIndex, bool playerOne)
            {
                List<Piece> pieces = playerOne ? playerOnePieces : playerTwoPieces;
                Vector2Int position;
                Type type;
                double[,] map;
                int index, y;

                for (int i = 0; i < 6; i++)
                    Input[i + startingIndex] = 0;

                foreach(Piece piece in pieces)
                {
                    position = piece.Position;
                    type = piece.GetType();

                    if (type == typeof(Pawn))
                    {
                        map = pmPawn;
                        index = 0;
                    }
                    else
                        if (type == typeof(Rook))
                    {
                        map = pmRook;
                        index = 1;
                    }
                    else
                        if (type == typeof(Knight))
                    {
                        map = pmRook;
                        index = 2;
                    }
                    else
                        if (type == typeof(Bishop))
                    {
                        map = pmRook;
                        index = 3;
                    }
                    else
                        if (type == typeof(Queen))
                    {
                        map = pmRook;
                        index = 4;
                    }
                    else
                    {
                        map = pmRook;
                        index = 5;
                    }

                    y = playerOne ? position.y : 7 - position.y;
                    Input[index + startingIndex] += map[position.x, y];
                }

                Input[startingIndex] /= 8;
                Input[startingIndex + 1] /= 2;
                Input[startingIndex + 2] /= 2;
                Input[startingIndex + 3] /= 2;
            }

            protected override double GetStrategyPower(ChessStrategy strategy, bool playerOne)
            {
                List<Piece> pieces = playerOne ? playerOnePieces : playerTwoPieces;
                Type type;
                bool fit;
                Vector2Int position;
                double ret = 0;

                foreach (Piece piece in pieces)
                {
                    type = piece.GetType();

                    if (!strategy.Core.ContainsType(type))
                        continue;

                    position = piece.Position;

                    if (!IsBeingUsed(strategy.Required, position, playerOne))
                        continue;

                    if (strategy.Mutations.Length > 0)
                    {
                        fit = false;

                        foreach (ChessTactic[] tactic in strategy.Mutations)
                            if (IsBeingUsed(tactic, position, playerOne))
                            {
                                fit = true;
                                break;
                            }

                        if (!fit)
                            continue;
                    }

                    ret += 1f / strategy.maxUses;
                }

                return ret;
            }

            #endregion

            #region Loading Data

            private void LoadPositionalMaps()
            {
                pmPawn = GetPositionalTable("Pawn");
                pmRook = GetPositionalTable("Rook");
                pmKnight = GetPositionalTable("Knight");
                pmBishop = GetPositionalTable("Bishop");
                pmQueen = GetPositionalTable("Queen");
                pmKing = GetPositionalTable("King");
            }

            private double[,] GetPositionalTable(string type)
            {
                string[][] data = CSVReader.Load(string.Format("PositionalTable_{0}", type));
                int size = data.Length;
                double[,] positionalTable = new double[size, size];

                for (int x = 0; x < size; x++)
                    for (int y = 0; y < size; y++)
                        positionalTable[x, y] = double.Parse(data[x][y]);

                return positionalTable;
            }

            #endregion
        }

        public class ChessMove : Move
        {
            public Vector2Int From { get; set; }
            public Vector2Int To { get; set; }
            public Piece Removes { get; set; }

            public bool OnDo { get; set; }

            public ChessMove()
            {

            }

            public ChessMove(Vector2Int from, Vector2Int to)
            {
                From = from;
                To = to;
            }

            public override Move GetCopy()
            {
                ChessMove move = new ChessMove(From, To);
                move.Removes = Removes;
                return move;
            }

            public override void Transform(Move other)
            {
                ChessMove otherMove = (ChessMove)other;
                From = otherMove.From;
                To = otherMove.To;
                OnDo = otherMove.OnDo;
                Removes = otherMove.Removes;
            }
        }
    }
}