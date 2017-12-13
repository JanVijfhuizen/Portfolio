using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckCard : MonoBehaviour {

    [HideInInspector]
    public int index; //index of card in memory, does not change
    public CardHolder cH;

	public void _DeckCard()
    {
        DeckBuilder.self.curDeck.deck.Remove(index);
        Destroy(gameObject);
    }
}
