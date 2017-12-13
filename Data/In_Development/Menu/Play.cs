using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Play : MonoBehaviour {

    private DeckBuilder db;
    [SerializeField]
    private GameObject playerScreen, readyButton;
    [SerializeField]
    private Transform deck;
    [SerializeField]
    private Text p1Deck, p2Deck;
    [SerializeField]
    private string chooseDeckText;

    public static Play self;

    private void Awake()
    {
        self = this;
    }

    private void Start()
    {
        db = DeckBuilder.self;
    }

	public void _Play()
    {
        playerScreen.SetActive(true);

        //spawn special deck types
        DeckChoosable dc;
        Transform t;
        foreach (DeckBuilder.Deck d in db.collection.decks)
        {
            t = Instantiate(deck, Vector3.zero, Quaternion.identity);
            t.SetParent(db.gridDecks);
            db.spawnedDecks.Add(t);

            dc = t.GetComponent<DeckChoosable>();
            dc.Deck = d;
        }

        p1Deck.text =
        p2Deck.text = chooseDeckText;
    }

    public void Back()
    {
        playerScreen.SetActive(false);
        db.decks.SetActive(false);

        p1 = null;
        p2 = null;
        isChoosing = -1;
        readyButton.SetActive(false);
        db.RefreshUIList(db.spawnedDecks);
    }

    private void Ready(bool isTrue)
    {
        readyButton.SetActive(isTrue);
    }

    public void StartGame()
    {
        //send information to the next scene
        GameManager.deckData = new GameManager.DeckData(p1, p2);
        StartCoroutine(_StartGame());
    }

    [SerializeField]
    private Image loadBar;
    [SerializeField]
    private GameObject loadScreen;
    private AsyncOperation ao;
    private IEnumerator _StartGame()
    {
        loadScreen.SetActive(true);

        ao = SceneManager.LoadSceneAsync(1);
        ao.allowSceneActivation = false;

        while (!ao.isDone)
        {
            loadBar.fillAmount = ao.progress + 0.1f;

            if (ao.progress == 0.9f)
                ao.allowSceneActivation = true;
            yield return null;
        }
    }

    private DeckBuilder.Deck p1, p2;
    private int isChoosing = -1;

    public void Choose(DeckBuilder.Deck deck)
    {
        switch (isChoosing)
        {
            case -1:
                return;
            case 0:
                p1 = deck;
                p1Deck.text = deck.name;
                break;
            case 1:
                p2 = deck;
                p2Deck.text = deck.name;
                break;
        }

        db.decks.SetActive(false);
        Ready(p1 != null && p2 != null);
    }

    public void StartChoosing(int i)
    {
        isChoosing = i;
        db.decks.SetActive(true);
    }
}
