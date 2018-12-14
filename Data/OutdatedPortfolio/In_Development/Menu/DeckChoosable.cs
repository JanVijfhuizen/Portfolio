using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckChoosable : MonoBehaviour {

    [SerializeField]
    private Text text;

    private DeckBuilder.Deck deck;
    public DeckBuilder.Deck Deck
    {
        set
        {
            deck = value;
            text.text = deck.name;
        }
    }

	public void Choose()
    {
        Play.self.Choose(deck);
    }
}
