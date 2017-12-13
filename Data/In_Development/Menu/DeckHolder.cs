using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckHolder : MonoBehaviour {

	public void Spawn(int index, string name)
    {
        ChangeDeck deck = transform.GetChild(1).GetComponent<ChangeDeck>();
        deck.index = index;
        deck.text.text = name;
    }
}
