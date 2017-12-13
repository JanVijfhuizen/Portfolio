using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour {

    public static List<Card> cards;

    private void Awake()
    {
        if (cards != null)
            return;
        cards = new List<Card>();
        Component[] components = GetComponents(typeof(Card));
        foreach (Component c in components)
            if (c as Card != null)
                cards.Add(c as Card);
    }
}
