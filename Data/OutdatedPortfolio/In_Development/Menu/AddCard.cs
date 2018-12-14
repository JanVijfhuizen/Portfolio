using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddCard : MonoBehaviour {
    
    [HideInInspector]
    public int index;
    public CardHolder cH;

    public void _AddCard()
    {
        if (DeckBuilder.self.building)
            CardDetail.self.Open(cH.Card, "Add to Deck", __AddCard);
        else
            CardDetail.self.Open(cH.Card, "Close", null);
    }

    private void __AddCard()
    {
        DeckBuilder.self.AddCard(index);
    }
}
