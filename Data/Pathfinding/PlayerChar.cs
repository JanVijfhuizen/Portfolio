using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerChar : Character {

    public override void TakeTurn()
    {
        Debug.Log("MY TURN");
        base.TakeTurn();
    }
}
