using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddDeck : MonoBehaviour {

	public void _AddDeck()
    {
        DeckBuilder.self.StartBuildingNewDeck();
    }
}
