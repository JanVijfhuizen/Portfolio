using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardDetail : MonoBehaviour {

    public static CardDetail self;
    [SerializeField]
    private CardHolder ch;
    public delegate void ExitFunction();
    private ExitFunction exitFunction;
    public Text exitText;
    [SerializeField]
    private GameObject cardDetail;

    private void Awake()
    {
        self = this;
    }

    public void Open(Card card, string exitText, ExitFunction function)
    {
        cardDetail.SetActive(true);
        ch.Card = card;
        exitFunction = function;
        this.exitText.text = exitText;
    }

    public void Function(bool close)
    {
        if (exitFunction != null)
            exitFunction();
        else
        {
            Close();
            return;
        }
        if(close)
            Close();    
    }

    public void Close()
    {
        cardDetail.SetActive(false);
        exitFunction = null;
    }
}
