using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileNode : MonoBehaviour {

    public LevelCreator.Node node;

    public void OnTouch(bool hold)
    {
        if (!node.filled)
            return;
        Character c = TurnManager.curChar;
        if (!(c != null))
            return;
        if (!c.friendly)
            return;
        c.PreparePath(node);
    }
}
