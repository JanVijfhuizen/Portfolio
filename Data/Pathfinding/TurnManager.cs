using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour {

    [HideInInspector]
    public bool playerTurn = false;
    public static Character curChar;

	private void Start()
    {
        StartCoroutine(WaitForLevel());
    }

    private IEnumerator WaitForLevel()
    {
        while (LevelCreator.self.generated)
            yield return null;
        while (!Entrance.initialized)
            yield return null;
        while (players.Count == 0)
            yield return null;
        //start turn management
        UpdateTurn();
    }
    [HideInInspector]
    public List<Character> enemies = new List<Character>();
    private Queue<Character> _enemies = new Queue<Character>();
    [HideInInspector]
    public List<Character> players = new List<Character>();
    private Queue<Character> _players = new Queue<Character>();

    public void UpdateTurn() //wacht totdat alle players zijn geladen
    {
        if (playerTurn)
        {
            if (_players.Count > 0)
            {
                Character c = _players.Dequeue();
                c.TakeTurn();
                return;
            }
            else
            {
                playerTurn = false;
                _enemies.Clear();
                foreach (Character c in enemies)
                    _enemies.Enqueue(c);
            }
        }

        if(_enemies.Count > 0)
        {
            Character c = _enemies.Dequeue();
            c.TakeTurn();
            return;
        }
        else
        {
            playerTurn = true;
            _players.Clear();
            foreach (Character c in players)
                _players.Enqueue(c);
            UpdateTurn();
        }
    }
}
