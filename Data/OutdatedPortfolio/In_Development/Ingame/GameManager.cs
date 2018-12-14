using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    /* to do list

        fix:
        block unfinished deck
        fix resolution of spawned decks/cards = ingame zijn ze weg in 720p

        add:
        card effects
        game turns (importance stat win condition / destroy other units / unique win condition)
        all bord game things
    */

    public static GameManager self;

    [SerializeField]
    private GameObject card;
    [SerializeField]
    private Transform p1hand, p2hand;

    [SerializeField]
    private GameObject _bord;
    public static GameObject bord;

    public class DeckData
    {
        public Player p1, p2;

        public DeckData(DeckBuilder.Deck deckp1, DeckBuilder.Deck deckp2)
        {
            p1 = new Player(deckp1);
            p2 = new Player(deckp2);
        }
    }

    public class Player
    {
        public int income, resources, commandPoints;
        public List<CardHolder> hand;
        public DeckBuilder.Deck deck;
        public DeckBuilder.Deck shuffledDeck;

        public Player(DeckBuilder.Deck deck)
        {
            this.deck = deck;
        }
    }

    public class Hand
    {
        public List<Card> data;
    }

    public static DeckData deckData;
    [HideInInspector]
    public bool activePlayer;

    //game data
    public int baseIncome = 5, baseResources = 7, baseCommandPoints = 100;
    [SerializeField]
    private Text p1stats, p2stats;

    public void UpdateStats()
    {
        UpdateStats(deckData.p1);
        UpdateStats(deckData.p2);
    }

    public void UpdateStats(Player player)
    {
        Text t;
        if (player == deckData.p1)
            t = p1stats;
        else
            t = p2stats;
        t.text = "CP " + player.commandPoints + " INC " + player.income + " RES " + player.resources; 
    }

    private void Awake()
    {
        bord = _bord;
        self = this;
    }

    private void Start()
    {
        //coinflip who starts first
        activePlayer = Random.Range(0, 2) == 0;

        ShuffleDeck(deckData.p1);
        ShuffleDeck(deckData.p2);

        DrawFirstHand(deckData.p1);
        DrawFirstHand(deckData.p2);

        deckData.p1.income = deckData.p2.income = baseIncome;
        deckData.p1.resources = deckData.p2.resources = baseResources;
        deckData.p1.commandPoints = deckData.p2.commandPoints = baseCommandPoints;

        UpdateStats();
    }

    private void ShuffleDeck(Player player)
    {
        DeckBuilder.Deck shuffledDeck = new DeckBuilder.Deck();
        List<int> shuffleableCards = new List<int>();
        foreach (int i in player.deck.deck)
            shuffleableCards.Add(i);

        int r;
        while(shuffleableCards.Count > 0)
        {
            r = Random.Range(0, shuffleableCards.Count);
            shuffledDeck.deck.Add(shuffleableCards[r]);
            shuffleableCards.RemoveAt(r);
        }

        player.shuffledDeck = shuffledDeck;
    }

    //drawn cards are hidden until revealed
    private void DrawFirstHand(Player player)
    {
        player.hand = new List<CardHolder>();
        int handCound = DeckBuilder.self.handSize;

        for (int i = 0; i < handCound; i++)
            Draw(player, true);

        //show that player x will go first

        StartDeployTurn();
    }

    #region Turn Manager

    //deploy phase (one time only until both end turn while doing nothing)

    //loop:
    //draw - effects - reinforce - command x both players - execute (once)

    [SerializeField]
    private GameObject turnScreen;
    [SerializeField]
    private Text turnButtonText;

    private bool deployed, deploying = true; //whether or not one player has ended their turn without deploying
    private int handCount;
    public Player curPlayer;
    private void StartDeployTurn()
    {
        //black screen
        turnScreen.SetActive(true);
        turnButtonText.text = "Click to start " + (activePlayer ? " Player 1" : " Player 2") + "'s turn";
    }

    public void ReadyDeploy()
    {
        //remove black screen
        turnScreen.SetActive(false);
        foreach (CardHolder cH in deckData.p1.hand)
            cH.Hidden = !activePlayer;
        foreach (CardHolder cH in deckData.p2.hand)
            cH.Hidden = activePlayer;

        curPlayer = activePlayer ? deckData.p1 : deckData.p2;
        handCount = curPlayer.hand.Count;
    }

    private void EndDeployTurn() //when playing something or ending the turn
    {
        if (curPlayer.hand.Count == handCount)
            if (deployed)
            {
                StartGame();
                return;
            }
            else
                deployed = true;
        SwitchPlayer();
        StartDeployTurn();
    }

    private void SwitchPlayer()
    {
        activePlayer = !activePlayer;
        curPlayer = activePlayer ? deckData.p1 : deckData.p2;
    }

    //end deploy

    private void StartGame()
    {
        deploying = false;
        Debug.Log("Start Game");
    }

    #region Main Game Loop

    //draw, play, command


    #endregion

    public void PlayCard(CardHolder cH)
    {
        //play
        curPlayer.hand.Remove(cH);
        Destroy(cH.gameObject);
        UpdateStats(curPlayer);

        //instantiate card object
        PlayedCard();
    }

    public void PlayedCard() //when you finished deploying a card (both reinforcement / deploy phase)
    {
        if (deploying)
            EndDeployTurn();
    }

    public void EndTurn()
    {
        if (deploying)
        {
            EndDeployTurn();
            return;
        }
    }
    #endregion

    private void Draw(Player player, bool hidden)
    {
        DeckBuilder.Deck deck = player.shuffledDeck;
        if (deck.deck.Count == 0)
            return;

        Card _card;
        int r;
        r = Random.Range(0, deck.deck.Count);
        _card = CardManager.cards[deck.deck[r]];
        deck.deck.RemoveAt(r);
        //spawn
        Transform t;
        t = Instantiate(card, Vector3.zero, Quaternion.identity).transform;
        CardHolder cH = t.GetComponent<CardHolder>();
        cH.Card = _card;
        cH.Hidden = hidden;

        if(player == deckData.p1)
            t.SetParent(p1hand, false);
        else
            t.SetParent(p2hand, false);

        t.transform.localPosition = Vector3.zero;
        player.hand.Add(cH);
    }
}
