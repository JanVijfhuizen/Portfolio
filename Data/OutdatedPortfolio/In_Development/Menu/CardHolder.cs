using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardHolder : MonoBehaviour {

    private Card card;
    public Card Card
    {
        get
        {
            return card;
        }
        set
        {
            card = value;
            name.text = card.name;
            cost.text = "CST: " + card.cost.ToString();
            attack.text = "ATK: " + card.attack.ToString();
            health.text = "HLT: " + card.health.ToString();
            ammo.text = "IMP: " + card.importance.ToString();
            speed.text = "SPD: " + card.speed.ToString();
            image.sprite = card.image;
        }
    }

    [SerializeField]
    private GameObject cardBack;
    private bool hidden;
    public bool Hidden
    {
        set
        {
            hidden = value;
            cardBack.SetActive(hidden);
        }    
    }

    public Text name, cost, attack, health, ammo, speed;
    public Image image;

    public void PlayCard()
    {
        if (card.Play())
            GameManager.self.PlayCard(this);
        else
            return;
    }

    public void ShowCard()
    {
        if (hidden)
            return;
        CardDetail.self.Open(card, "Play Card", PlayCard);
    }
}
