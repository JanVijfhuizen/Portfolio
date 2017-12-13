using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class But : MonoBehaviour {

    private bool pressed = false;
	public void Press(int num)
    {
        if (pressed || TicTacToe.self.playerWon)
            return;
        pressed = true;
        transform.GetComponent<Image>().color = TicTacToe.self.currentPlayer == TicTacToe.Tile.P1 ? Color.green : Color.red;
        TicTacToe.self.FillTile(num);
    }
}
