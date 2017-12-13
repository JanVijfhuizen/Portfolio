using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class DeckBuilder : MonoBehaviour {

    //to do:

    /*
    duplicates visueel weghalen
    delete knop maken voor decks
    */

    public static DeckBuilder self;
    public Popup popup;
    public GameObject _collection, decks;

    [HideInInspector]
    public bool building;

    public RectTransform cardHoldLine, gridCards, gridDecks, deck, addDeck, cardDeck;

    //data management
    [HideInInspector]
    public List<Transform> spawnedLines, spawnedCards, spawnedDecks;

    [SerializeField]
    private Text warning;
    [SerializeField]
    private float warningDuration;
    public void ShowWarning(string text)
    {
        StartCoroutine(_ShowWarning(text));
    }

    private IEnumerator _ShowWarning(string text)
    {
        warning.gameObject.SetActive(true);
        warning.text = text;

        float remainingTime = warningDuration;
        while(remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        warning.gameObject.SetActive(false);
    }

    public void RefreshUIList(List<Transform> deletable)
    {
        foreach (Transform t in deletable)
            if(t != null)
                Destroy(t.gameObject);
        deletable.Clear();
    }

    private void Awake()
    {
        self = this;
    }

    private void Start()
    {
        folderPath = MakeFolderPath(fileName);
        Load();
    }

    private bool opened;
    public void OpenCollection()
    {
        if (opened)
            return;
        opened = true;

        _collection.SetActive(true);
        decks.SetActive(true);

        //show all cards
        int lineCount = 3;
        int index = 0;
        RectTransform currentLine = null;
        Transform holder;
        AddCard ac;

        foreach (Card card in CardManager.cards)
        {
            lineCount++;
            if (lineCount == 4) //width is 4
            {
                lineCount = 0;
                currentLine = Instantiate(cardHoldLine, Vector3.zero, Quaternion.identity);
                currentLine.SetParent(gridCards, false);
                spawnedLines.Add(currentLine);
            }

            //get child as cardholder
            holder = currentLine.GetChild(lineCount);
            holder.gameObject.SetActive(true);

            ac = holder.GetComponent<AddCard>();
            ac.index = index;
            ac.cH.Card = card;

            spawnedCards.Add(holder);
            index++;
        }

        Refresh();
    }

    public void CloseCollection()
    {
        opened = false;
        _collection.SetActive(false);
        decks.SetActive(false);

        RefreshUIList(spawnedLines);
        RefreshUIList(spawnedDecks);
        popup.Close();
        CardDetail.self.Close();
    }

    private void Refresh()
    {
        SpawnDecks();
        SpawnAddDeckButton();
    }

    public void SpawnDecks()
    {
        RefreshUIList(spawnedDecks);

        Transform holder;
        for (int i = 0; i < collection.decks.Count; i++) //Deck _deck in collection.decks
        {
            holder = Instantiate(deck, Vector3.zero, Quaternion.identity);
            holder.SetParent(gridDecks);
            /*
            holder.GetChild(0).GetChild(0).GetComponent<Text>().text = _deck.name;
            */
            holder.GetComponent<DeckHolder>().Spawn(i, collection.decks[i].name);
            spawnedDecks.Add(holder);
        }
    }

    private void SpawnAddDeckButton()
    {
        //spawn "add new deck"
        Transform holder;
        holder = Instantiate(addDeck, Vector3.zero, Quaternion.identity);
        holder.SetParent(gridDecks);
        spawnedDecks.Add(holder);
    }

    public Collection collection;
    [SerializeField]
    public class Collection
    {
        public List<Deck> decks = new List<Deck>();
    }

    public int handSize, deckSize, maxDeckCount, maxDuplicates;
    [Serializable]
    public class Deck
    {
        public string name = "Custom Deck";
        public List<int> deck = new List<int>();
    }

    [SerializeField]
    private string folderName, fileName;
    private string folderPath;

    #region DeckBuilding

    [HideInInspector]
    public Deck curDeck;
    public void StartBuildingNewDeck()
    {
        int i = collection.decks.Count;
        if (i >= maxDeckCount)
        {
            //give an error
            ShowWarning("Maximal amount of decks has been reached!");
            return;
        }

        collection.decks.Add(new Deck());
        StartBuilding(i);
    }

    public void StartBuilding(int index)
    {
        if(building)
        {
            DoneBuilding();
            return;
        }
        building = true;
        curDeck = collection.decks[index];
        RefreshUIList(spawnedDecks);

        //spawn all cards within the deck in deck grid
        Transform holder;

        //spawn deck
        holder = Instantiate(deck, Vector3.zero, Quaternion.identity);
        holder.SetParent(gridDecks);

        holder.GetComponent<DeckHolder>().Spawn(index, collection.decks[index].name);
        spawnedDecks.Add(holder);
        curDeck.deck.Sort();

        foreach (int i in curDeck.deck)
            SpawnDeckCard(i);
    }

    public void AddCard(int index)
    {
        if (!building)
            return;
        //check for duplicates
        #region Check For Duplicate
        int i = 1;

        foreach (int d in curDeck.deck)
            if (d == index)
                i++;

        if (i > maxDuplicates)
        {
            //give an error
            ShowWarning("Duplicate amount reached!");
            return;
        }
        #endregion

        if (curDeck.deck.Count < deckSize)
        {
            curDeck.deck.Add(index);
            SpawnDeckCard(index);
        }
    }

    private void SpawnDeckCard(int index)
    {
        //spawn card within the deck in deck grid
        Transform holder = Instantiate(cardDeck, Vector3.zero, Quaternion.identity);
        holder.SetParent(gridDecks);
        DeckCard card = holder.GetComponent<DeckCard>();
        card.cH.Card = CardManager.cards[index];
        card.index = index;
        spawnedDecks.Add(holder);
    }

    public void RemoveCard(int index)
    {
        foreach(int d in curDeck.deck)
            if(d == index)
            {
                curDeck.deck.Remove(d);
                return;
            }
    }

    public void RemoveDeck(int index)
    {
        collection.decks.RemoveAt(index);
        Save();
        Refresh();
    }

    public void DoneBuilding()
    {
        building = false;
        Save();
        SpawnDecks();
        SpawnAddDeckButton();
    }

    #endregion

    #region XML

    public void Load()
    {
        if (!File.Exists(folderPath))
        {
            collection = new Collection();
            return;
        }

        XmlSerializer serializer = new XmlSerializer(typeof(Collection));
        FileStream stream = new FileStream(folderPath, FileMode.Open);
        collection = (Collection)serializer.Deserialize(stream) as Collection;
        stream.Close();
    }

    public void Save()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(Collection));
        FileStream stream = new FileStream(folderPath, FileMode.Create);
        serializer.Serialize(stream, collection);
        stream.Close();
    }

    private string MakeFolderPath(string _fileName)
    {
        char s = Path.DirectorySeparatorChar;

#if UNITY_STANDALONE

        return Application.dataPath + s + folderName + s + _fileName + ".xml";

#endif

#if UNITY_ANDROID || UNITY_IOS

        return Application.persistentDataPath + s + _fileName + ".xml";
        //return Application.dataPath + s + folderName + s + _fileName + ".xml";
#endif
    }

    #endregion
}
