using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TicTacToe : MonoBehaviour {

    public static TicTacToe self;

    public enum Tile {Empty, P1, P2 }
    [SerializeField]
    private GameObject p1Sign, p2Sign;

    public Tile[,] grid;
    public int sizeGrid;

    [HideInInspector]
    public Tile currentPlayer = Tile.P1;
    [HideInInspector]
    public bool playerWon = false;

    private void Start()
    {
        self = this;
        SetupGrid();
    }

    private void SetupGrid()
    {
        grid = new Tile[sizeGrid, sizeGrid];
    }

    private void SwitchTurns()
    {
        if (currentPlayer == Tile.P1)
            currentPlayer = Tile.P2;
        else
            currentPlayer = Tile.P1;
    }

    private void CheckIfWon()
    {
        bool wonFound;
        for (int w = 0; w < sizeGrid; w++)
            for(int h = 0; h < sizeGrid; h++)
                if(grid[w,h] == currentPlayer)
                {
                    #region Checking
                    wonFound = true;
                    //top - bottom
                    for (int t = 0; t < sizeGrid; t++)
                        if (grid[w, t] != currentPlayer)
                            wonFound = false;
                    //right - left
                    if (!wonFound)
                    {
                        wonFound = true;
                        for (int t = 0; t < sizeGrid; t++)
                            if (grid[t, h] != currentPlayer)
                                wonFound = false;
                    }

                    #endregion

                    if (wonFound)
                        PlayerWon();
                }

        //diagonally
        wonFound = true;
        for (int x = 0; x < sizeGrid; x++)
            if (grid[x, x] != currentPlayer)
                wonFound = false;
        if (wonFound)
            PlayerWon();
        if (!wonFound)
        {
            wonFound = true;
            for (int x = 0; x < sizeGrid; x++)
                if (grid[sizeGrid - 1 - x, x] != currentPlayer)
                    wonFound = false;
            if (wonFound)
                PlayerWon();
        }
    }

    private void PlayerWon()
    {
        print("Player " + currentPlayer + " won!");
        playerWon = true;
    }

    private bool CheckSurroundingGrid(int newX, int newY, int thisX, int thisY)
    {
        int x = thisX + newX;
        int y = thisY + newY;
        CheckBorders(x, y);

        if (grid[x, y] == currentPlayer)
            return true;
        return false;
    }

    private bool CheckBorders(int x, int y)
    {
        if (x > sizeGrid || x < 0)
            return false;
        if (y > sizeGrid || y < 0)
            return false;
        return true;
    }

    #region Button Tiles

    public void FillTile(int tileNumber)
    {
        #region FIll
        Tile targetTile;
        int calc = Mathf.FloorToInt(tileNumber / sizeGrid);
        targetTile = grid[calc, tileNumber - calc * sizeGrid];

        #region Check Certain Aspects
        //Check if empty
        if (targetTile != Tile.Empty)
            return;
        grid[calc, tileNumber - calc * sizeGrid] = currentPlayer;
        //Check if won
        CheckIfWon();
        #endregion

        bool filled = true;
        foreach(Tile tile in grid)
            if(tile == Tile.Empty)
            {
                filled = false;
                break;
            }
        if (filled)
            Debug.Log("TIE!");

        SwitchTurns();
        #endregion
    }

    #endregion
}
