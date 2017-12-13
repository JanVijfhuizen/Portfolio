using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeDeck : MonoBehaviour {

    [HideInInspector]
    public int index;
    public Text text;
    private DeckBuilder db;

    private void Awake()
    {
        text = transform.GetChild(0).GetComponent<Text>();
        db = DeckBuilder.self;
    }

    public void _ChangeDeck()
    {
        db.StartBuilding(index);
    }

    public void Delete()
    {
        db.popup.Open("Are you sure you want to delete: " + db.collection.decks[index].name + "?", "", true, _Delete);
    }

    public void _Delete(string s)
    {
        db.RemoveDeck(index);
    }

    public void ChangeName()
    {
        db.popup.Open("Insert Deck Name:", db.collection.decks[index].name, false, _ChangeName);
    }

    public void _ChangeName(string name)
    {
        text.text = name;
        db.collection.decks[index].name = name;
        db.Save();
    }
}
