using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChessLib;
using CurveSearchLib;
using System;
using System.Threading;

public class TestChess : MonoBehaviour
{
    private class VisualPiece
    {
        private GameObject obj;
        private Transform trans;
        private SpriteRenderer renderer;

        public VisualPiece()
        {
            obj = new GameObject();
            trans = obj.transform;
            renderer = obj.AddComponent(typeof(SpriteRenderer)) as SpriteRenderer;
            renderer.sortingOrder = 1;

            Disable();
        }

        public void Enable()
        {
            obj.SetActive(true);
        }

        public void Disable()
        {
            obj.SetActive(false);
        }

        public Color Color
        {
            set
            {
                renderer.color = value;
            }
        }

        public Sprite Sprite
        {
            set
            {
                renderer.sprite = value;

                // Because for some reason spriterenderers and size are just not ment for eachother
                Bounds bounds = renderer.sprite.bounds;
                float boundsSize = bounds.size.x;
                trans.localScale = new Vector2(.5f, .5f) / boundsSize;
            }
        }

        public Vector2 Position
        {
            set
            {
                trans.position = value;
            }
        }
    }

    // Data
    #region Data

    [SerializeField]
    private CurveSearchData data;
    [SerializeField]
    private List<ChessStrategy> strategies = new List<ChessStrategy>();

    private Chess.ChessBoard board;
    private CurveSearch<Chess.ChessBoard, Chess.ChessMove, ChessStrategy, ChessTactic> searchP1, searchP2;

    #endregion

    // Visuals
    #region Visuals

    [SerializeField]
    private Transform boardVisual;
    [SerializeField]
    private float size;
    [SerializeField]
    private Sprite pawnSprite, rookSprite, knightSprite,
        bishopSprite, queenSprite, kingSprite;
    [SerializeField]
    private Color p1Color, p2Color;
    [SerializeField]
    private int cycleCount;

    private float boardVisualStart, nodeSize;
    private VisualPiece[] pieces;

    private Thread thread;

    #endregion

    private void Awake()
    {
        data.seed = DateTime.Now.ToString();

        board = new Chess.ChessBoard();
        searchP1 = new CurveSearch<Chess.ChessBoard, Chess.ChessMove, ChessStrategy, ChessTactic>(board, data, strategies);
        searchP2 = new CurveSearch<Chess.ChessBoard, Chess.ChessMove, ChessStrategy, ChessTactic>(board, data, strategies);

        searchP1.PlayerOne = true;
        searchP2.PlayerOne = false;

        pieces = new VisualPiece[32];
        for (int i = 0; i < 32; i++)
            pieces[i] = new VisualPiece();

        boardVisualStart = -size / 2 + size / 16;
        nodeSize = size / 8;

        SpriteRenderer renderer = boardVisual.GetComponent<SpriteRenderer>();
        Bounds bounds = renderer.sprite.bounds;
        float boundsSize = bounds.size.x;

        boardVisual.localScale = new Vector2(size, size) / boundsSize;

        UpdateBoard();

        thread = new Thread(Cycle) { IsBackground = true };
        //thread.Start();
    }

    private void Cycle()
    {
        CurveSearch<Chess.ChessBoard, Chess.ChessMove, ChessStrategy, ChessTactic> search = board.IsPlayerOneActive() ? searchP1 : searchP2;

        for (int i = 0; i < cycleCount; i++)
            search.Cycle();

        Chess.ChessMove move = search.GetAndSetNext();

        Vector2Int from = move.From, to = move.To;

        Debug.Log((!board.IsPlayerOneActive() ? "White" : "Black") + " from " + from + ", to " + to);
    }

    private void Update()
    {
        if (!thread.IsAlive)
        {
            UpdateBoard();

            if (board.GetGameState() != Move.GameState.Active)
                return;

            thread = new Thread(Cycle) { IsBackground = true };
            thread.Start();
        }
    }

    private void UpdateBoard()
    {
        Chess.Node[,] grid = board.grid;
        Chess.Piece chessPiece;
        VisualPiece visualPiece;
        Sprite sprite;
        Type type;
        int index = 0;

        for (int i = 0; i < 32; i++)
            pieces[i].Disable();

        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                if (!grid[x, y].Empty)
                {
                    chessPiece = grid[x, y].Piece;

                    visualPiece = pieces[index];
                    visualPiece.Color = chessPiece.PlayerOne ? p1Color : p2Color;

                    type = chessPiece.GetType();

                    if (type == typeof(Chess.Pawn))
                        sprite = pawnSprite;
                    else
                        if (type == typeof(Chess.Rook))
                        sprite = rookSprite;
                    else
                        if (type == typeof(Chess.Knight))
                        sprite = knightSprite;
                    else
                        if (type == typeof(Chess.Bishop))
                        sprite = bishopSprite;
                    else
                        if (type == typeof(Chess.Queen))
                        sprite = queenSprite;
                    else
                        sprite = kingSprite;

                    visualPiece.Sprite = sprite;
                    visualPiece.Position = ConvertPosition(chessPiece.Position);
                    visualPiece.Enable();

                    index++;
                }
            }
    }

    private Vector2 ConvertPosition(Vector2Int gridPosition)
    {
        return new Vector2(nodeSize * gridPosition.x + boardVisualStart, 
            nodeSize * gridPosition.y + boardVisualStart);
    }
}