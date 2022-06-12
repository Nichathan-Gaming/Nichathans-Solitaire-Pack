using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KlondikeSolitaire : MonoBehaviour
{
    public static KlondikeSolitaire instance;

    //STATIC VARIABLES
    private const string FASTEST_TIME_PREFS= "KlondikeSolitaireFastestTime",
        LEAST_MOVES_PREFS= "KlondikeSolitaireLeastMoves"
    ;

    [SerializeField] GameObject undoLimitTitle;

    public int CARD_PLACEMENT_DIFFERENCE = 30;
    [SerializeField] int countUndoCount = 0;

    #region public variables
    [Header("Use these objects to track the undo if that setting is turned on")]
    [SerializeField] Text undoLimitText;

    [Header("The starting locations of the cards")]
    public GameObject[] sevenLocations;

    [Header("When checking for victory, look at their children")]
    public GameObject[] fourLocations;

    [Header("On Start, 52 cards are placed as a child here before being placed as a child as deck")]
    [SerializeField] GameObject instantiationLocation;

    [Header("has cards that are children of children.")]
    //Gets lowest child on Draw()
    public GameObject deck;

    [Header("This is the base that cards are created with")]
    [SerializeField] GameObject cardPrefab;

    [Header("Helps with onEndDrag raycasting")]
    [SerializeField] GraphicRaycaster graphicRaycaster;

    [Header("Places cards from deck to here on draw")]
    public GameObject drawnCardHolder;

    [Header("Our stopwatch object used to track the time")]
    [SerializeField] private StopWatch stopWatch;

    [SerializeField] Image drawDeck;

    [Header("Do not assign in inspector")]
    public FlippableCard lastClickedFlippableCard = null;

    public FlippableCard draggingCard;

    //public DrawController drawController;

    //displays the refreshes left
    public Text deckRefreshText;
    #endregion public variables

    #region private variables
    private UnityAction drawCard;

    //track the click history
    private Stack<History> histories = new Stack<History>();

    //The text to show players their scores
    [SerializeField] Text timerText;
    [SerializeField] Text movesText;
    #endregion private variables

    #region Unity Default Override
    private void Awake()
    {
        if (instance == null)
        {
            instance = this; // In first scene, make us the singleton.
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(instance.gameObject); // On reload, singleton already set, so destroy duplicate.
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {
        drawDeck.sprite = SettingsManager.instance.GetCardBack();

        drawCard = DrawController.instance.Draw;

        SettingsManager.RESET = ResetGame;
        SettingsManager.UNDO = Undo;

        StartGame();
    }

    private void Update()
    {
        if (SettingsManager.instance.IsSettingsOpen()) return;

        //right click, see what can be auto completed
        if (Input.GetMouseButtonDown(1)) SearchForCompletion();

        //turn off border on click outside area
        if (Input.GetMouseButtonUp(0))
        {
            //Set up the new Pointer Event
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            List<RaycastResult> results = new List<RaycastResult>();

            //Raycast using the Graphics Raycaster and mouse click position
            pointerData.position = Input.mousePosition;
            if (graphicRaycaster == null) graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
            graphicRaycaster.Raycast(pointerData, results);

            //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
            if (results.Count > 0 && results[0].gameObject.GetComponent<FlippableCard>()==null && lastClickedFlippableCard!=null)
            {
                lastClickedFlippableCard.ToggleColumnOutlineAlpha(lastClickedFlippableCard.transform, false);
                
                NullifyLastClicked();
            }
        }

        //if (Input.GetMouseButtonDown(2))
        //{
        //    SetVictoryScreen();
        //}
    }
    #endregion Unity Default Override

    /**
     * If a card is clicked, unclick it
     */
    public void NullifyLastClicked()
    {
        if (lastClickedFlippableCard != null)
        {
            Outline outline = lastClickedFlippableCard.GetComponent<Outline>();

            if (outline != null)
            {
                outline.effectColor = Color.black;
            }

            lastClickedFlippableCard = null;
        }
    }

    /**
     * Change the display of the timer
     * 
     * This is a callback sent to StopWatch
     */
    public void SetTime(float time)
    {
        timerText.text = SettingsManager.instance.FormatTime(time);
    }

    #region Game Controls (Start, reset, victory)
    /**
     * Sets values that needs to be set and begins the game
     */
    public void StartGame()
    {
        for(int i = 0; i < 4; i++)
        {
            for(int j = 0; j < 13; j++)
            {
                GameObject newCard = Instantiate(cardPrefab, instantiationLocation.transform);

                newCard.name = i + "i_" + j + "j";

                FlippableCard flippableCards = newCard.GetComponent<FlippableCard>();

                flippableCards.number = j;
                flippableCards.suit = i;
            }
        }

        //set the cards
        AssignCards(false);

        stopWatch.StartStopWatch(SetTime);

        ManageUI();
    }

    void ManageUI()
    {
        if (SettingsManager.instance.IsLimitDeckRefresh())
        {
            deckRefreshText.text = "" + SettingsManager.instance.GetDeckRefreshesLeft();
        }
        else
        {
            deckRefreshText.text = "";
        }

        movesText.text = "" + (SettingsManager.instance.IsCountUndo() ? countUndoCount : histories.Count);

        #region handle reset from setting changes
        bool isLimitUndo = SettingsManager.instance.IsLimitUndo();
        undoLimitTitle.SetActive(isLimitUndo);
        undoLimitText.gameObject.SetActive(isLimitUndo);
        undoLimitText.text = SettingsManager.instance.GetCurrentUndosLeft() + "";
        #endregion handle reset from setting changes
    }

    /**
     * resets the game to how it was at the end of StartGame
     */
    public void ResetGame()
    {
        //move from fourLocations to instantiationLocation 
        foreach (GameObject go in fourLocations)
        {
            MoveChildrenToInstantiationLocation(go);
        }

        //move from sevenLocations to instantiationLocation 
        foreach (GameObject go in sevenLocations)
        {
            MoveChildrenToInstantiationLocation(go);
        }

        //move from deck to instantiationLocation 
        MoveChildrenToInstantiationLocation(deck);

        //move from drawCardHolder to instantiationLocation 
        MoveChildrenToInstantiationLocation(drawnCardHolder);

        //set the cards
        AssignCards(true);

        //clear the history
        histories.Clear();
        countUndoCount = 0;

        stopWatch.ResumeStopWatch();

        stopWatch.RestartStopWatch();

        ManageUI();
    }

    public void DrawCard()
    {
        drawCard();
    }

    public void OpenSettings()
    {
        SettingsManager.instance.ToggleSettings();
    }

    /**
     * grabs all of the parents children and moves them to the instantiationLocation
     * grabs the bottom most child first
     * 
     * also, resets the FlippableCard
     */
    private void MoveChildrenToInstantiationLocation(GameObject parent)
    {
        while (parent.transform.childCount > 0)
        {
            Transform bottomMostChild = GetLastChild(parent).transform;
            bottomMostChild.SetParent(instantiationLocation.transform);
            //bottomMostChild.localPosition = Vector3.zero;
            bottomMostChild.GetComponent<FlippableCard>().MoveTo(Vector3.zero);

            FlippableCard flippableCard = bottomMostChild.GetComponent<FlippableCard>();
            if (flippableCard != null)
            {
                flippableCard.isDraw = true;
                flippableCard.isBack = false;
                flippableCard.Flip();
            }
        }
    }

    /**
     * requires all cards to be in instantiationLocation
     */
    private void AssignCards(bool isInitialized)
    {
        //assign cards to the 7 locations
        for (int i = 0; i < 7; i++)
        {
            GameObject lastCard = sevenLocations[i];
            //get this many cards created
            for (int j = 0; j < i + 1; j++)
            {
                //parent = instantiationLocation
                //get index of a child of parent
                int index = Random.Range(0, instantiationLocation.transform.childCount);

                //get the random child
                GameObject newCard = instantiationLocation.transform.GetChild(index).gameObject;

                //move this card to sevenLocs at i
                newCard.transform.SetParent(lastCard.transform);

                //set the position of the new card
                Vector3 newPosition = new Vector3(0, j > 0 ? -CARD_PLACEMENT_DIFFERENCE : 0);
                //newCard.transform.localPosition = newPosition;

                FlippableCard flippableCards = newCard.GetComponent<FlippableCard>();
                if (flippableCards != null)
                {
                    flippableCards.isDraw = false;
                    flippableCards.MoveTo(newPosition, Time.time+(j*0.1f));
                }

                lastCard = newCard;
            }

            FlippableCard flippableCard = lastCard.GetComponent<FlippableCard>();
            if (isInitialized)
            {
                flippableCard.isBack = true;
                flippableCard.Flip();
            }
            else
            {
                flippableCard.isBack = false;
            }
        }

        GameObject lastParent = deck;
        //move the rest of the children to the deck location
        while (instantiationLocation.transform.childCount > 0)
        {
            int index = Random.Range(0, instantiationLocation.transform.childCount);

            //get the random child
            GameObject newCard = instantiationLocation.transform.GetChild(index).gameObject;

            //move this card to sevenLocs at i
            newCard.transform.SetParent(lastParent.transform);

            if (isInitialized)
            {
                //set the flippableCard components
                FlippableCard flippableCard = newCard.GetComponent<FlippableCard>();
                flippableCard.isDraw = true;
            }
            
            lastParent = newCard;
        }
    }

    /**
     * Gets the next card needed in the line fourLocations from Ace to King
     * as well as their suits
     * 
     * then, if that card is found as the bottomMost child of sevenLocations or drawnDeckHolder
     * add it to the fourLocations
     * 
     * if a match is found, call this function again and return.
     */
    private void SearchForCompletion()
    {
        NullifyLastClicked();

        //loop through the fourLocations
        foreach (GameObject fourLocationsObject in fourLocations)
        {
            Transform fourLocationsParent = GetLastChild(fourLocationsObject).transform;

            if (fourLocationsObject.transform.childCount > 0)
            {
                //get the last child here then look for the next card
                FlippableCard fourLocLastChildFlip = fourLocationsParent.GetComponent<FlippableCard>();
                if (fourLocLastChildFlip == null) continue;

                //get the next card
                int nextNumber = fourLocLastChildFlip.GetNextCard();
                if (nextNumber == 0) continue;

                //search drawnDeckHolder for this number
                SearchForNumber(fourLocationsParent, nextNumber, fourLocLastChildFlip.suit);
            }
            else
            {
                if(!SearchForNumber(fourLocationsParent, 0, 1))
                {
                    if(!SearchForNumber(fourLocationsParent, 0, 0))
                    {
                        if (!SearchForNumber(fourLocationsParent, 0, 2))
                        {
                            if (!SearchForNumber(fourLocationsParent, 0, 3))
                            {
                                continue;
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion Game Controls (Start, reset, victory)

    /**
     * Searches the object for a number and suit
     */
    private bool SearchForNumber(Transform fourLocationsParent, int number, int suit)
    {
        //find any Ace in drawnCardHolder or sevenLocations
        if (drawnCardHolder.transform.childCount > 0)
        {
            //look in the drawnCardHolder
            FlippableCard lastDrawn = GetLastChild(drawnCardHolder).GetComponent<FlippableCard>();
            if (lastDrawn != null && lastDrawn.number == number && lastDrawn.suit==suit)
            {
                //add to histories
                AddHistory(lastDrawn.gameObject, lastDrawn.transform.parent.gameObject, lastDrawn.transform.localPosition, false, false);

                //set parent to fourLocationsParent 
                Transform lastDrawnTransform = lastDrawn.gameObject.transform;
                lastDrawnTransform.SetParent(fourLocationsParent);
                //lastDrawnTransform.localPosition = Vector3.zero;

                FlippableCard flippableCard = lastDrawnTransform.GetComponent<FlippableCard>();
                flippableCard.MoveTo(Vector3.zero);
                flippableCard.isInAce = true;

                if(SettingsManager.instance.IsDrawThree()) DrawController.instance.ResetDeckThree();

                //match found, reset
                SearchForCompletion();
                SettingsManager.instance.PlayCheersSound();

                CheckVictory();

                return true;
            }
        }

        //Loop through the sevenLocations and look for an ace
        foreach (GameObject sevenLocationsObject in sevenLocations)
        {
            //ensure that there is a child to grab
            if (sevenLocationsObject.transform.childCount > 0)
            {
                //grab the last child
                FlippableCard lastDrawn = GetLastChild(sevenLocationsObject).GetComponent<FlippableCard>();
                if (lastDrawn != null && lastDrawn.number == number && lastDrawn.suit == suit)
                {
                    bool parentFlip = false;

                    //see if we need to flip the parent
                    FlippableCard lastParent = lastDrawn.gameObject.transform.parent.GetComponent<FlippableCard>();
                    if (lastParent != null && lastParent.isBack)
                    {
                        lastParent.Flip();
                        parentFlip = true;
                    }

                    //add to histories
                    AddHistory(lastDrawn.gameObject, lastDrawn.transform.parent.gameObject, lastDrawn.transform.localPosition, false, parentFlip);

                    //set parent to fourLocationsParent 
                    Transform lastDrawnTransform = lastDrawn.gameObject.transform;
                    lastDrawnTransform.SetParent(fourLocationsParent);
                    //lastDrawnTransform.localPosition = Vector3.zero;

                    lastDrawn.MoveTo(Vector3.zero);
                    lastDrawn.isInAce = true;

                    //match found, reset
                    SearchForCompletion();
                    SettingsManager.instance.PlayCheersSound();

                    CheckVictory();
                    return true;
                }
            }
        }
        return false;
    }

    /**
     * returns parent if there are no children
     */
    public GameObject GetLastChild(GameObject parent)
    {
        //the parent object
        GameObject lastChild = parent;

        /*
        Initialize a checkChild element
        
        while check child is not null, continue checking

        assign the checkChild to its child
         */
        for (GameObject checkChild = parent; checkChild != null; checkChild = checkChild.transform.GetChild(0).gameObject)
        {
            lastChild = checkChild;
            if (checkChild.transform.childCount == 0)
            {
                break;
            }
        }

        return lastChild;
    }

    /**
     * Looks through all fourLocations to see if their children are from A to K
     */
    public void CheckVictory()
    {
        for(int i = 0; i < fourLocations.Length; i++)
        {
            //see if the base has a child
            if (fourLocations[i].transform.childCount > 0)
            {
                //see if there are subsequent children from ACE to KING
                FlippableCard lastChild = GetLastChild(fourLocations[i]).GetComponent<FlippableCard>();
                if (lastChild == null || lastChild.number != 12)
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        SetVictoryScreen();
    }

    /**
     * Sets the victory screen
     */
    private void SetVictoryScreen()
    {
        if (!SettingsManager.instance.IsGameActive()) return;

        SettingsManager.instance.SetIsGameActive(false);

        float stopWatchTime = stopWatch.timeElapsed;
        stopWatch.PauseStopWatch();

        string fastestTimesString = "Fastest Times", leastMovesString = "Least Moves";
        List<float> fastestTimes = new List<float>();
        List<int> leastMoves = new List<int>();

        bool addedTime = false;
        bool addedMove = false;

        float lastTime = 0;
        int lastMove = 0;

        int moves = (SettingsManager.instance.IsCountUndo() ? countUndoCount : histories.Count);

        for (int i = 0; i < 10; i++)
        {
            #region fast time
            //the previous time at this location
            float fastestTime = PlayerPrefs.GetFloat(FASTEST_TIME_PREFS + i, 0);

            //if we haven't added a time yet and the current time is faster
            if (!addedTime && (fastestTime==0 || stopWatchTime < fastestTime))
            {
                addedTime = true;
                lastTime = fastestTime;
                fastestTimes.Add(stopWatchTime);
            }
            else
            {
                if (addedTime)
                {
                    fastestTimes.Add(lastTime);
                    lastTime = fastestTime;
                }
                else
                {
                    fastestTimes.Add(fastestTime);
                }
            }
            #endregion fast time

            #region least moves
            int leastMove = PlayerPrefs.GetInt(LEAST_MOVES_PREFS + i, 0);

            if (!addedMove && (leastMove==0 || moves < leastMove))
            {
                addedMove = true;
                lastMove = leastMove;
                leastMoves.Add(moves);
            }
            else
            {
                if (addedMove)
                {
                    leastMoves.Add(lastMove);
                    lastMove = leastMove;
                }
                else
                {
                    leastMoves.Add(leastMove);
                }
            }
            #endregion least moves
        }

        for(int i = 0; i < 10; i++)
        {
            PlayerPrefs.SetFloat(FASTEST_TIME_PREFS + i, fastestTimes[i]);
            PlayerPrefs.SetInt(LEAST_MOVES_PREFS + i, leastMoves[i]);
        }

        for (int i = 0; i < 10; i++)
        {
            fastestTimesString += "\n" + SettingsManager.instance.FormatTime(fastestTimes[i]);
            leastMovesString += "\n" + leastMoves[i];
        }
        
        string currentScore = "Time: " + timerText.text + "\nMoves: " + (SettingsManager.instance.IsCountUndo() ? countUndoCount : histories.Count); ;

        SettingsManager.instance.SetVictory(currentScore, fastestTimesString, leastMovesString);
    }

    /**
     * Ensures the flippable cards for ever card has the correct is draw is in ace is back
     */
    public void LabelAllCards()
    {
        //get cards in deck
        Transform parent = deck.transform;

        while (parent.childCount > 0)
        {
            parent = parent.GetChild(0);
            FlippableCard flippableCard = parent.GetComponent<FlippableCard>();
            if (flippableCard != null)
            {
                flippableCard.isInAce = false;
                flippableCard.isDraw = true;
                if (!flippableCard.isBack)
                {
                    flippableCard.Flip();
                }
            }
        }

        //get cards in the drawnCardHolder
        parent = drawnCardHolder.transform;

        while (parent.childCount > 0)
        {
            parent = parent.GetChild(0);
            FlippableCard flippableCard = parent.GetComponent<FlippableCard>();
            if (flippableCard != null)
            {
                flippableCard.isInAce = false;
                flippableCard.isDraw = true;
                if (flippableCard.isBack)
                {
                    flippableCard.Flip();
                }
            }
        }

        //get cards in sevenLocations
        foreach(GameObject go in sevenLocations)
        {
            //get cards in the drawnCardHolder
            parent = go.transform;

            while (parent.childCount > 0)
            {
                parent = parent.GetChild(0);
                FlippableCard flippableCard = parent.GetComponent<FlippableCard>();
                if (flippableCard != null)
                {
                    flippableCard.isInAce = false;
                    flippableCard.isDraw = false;
                }
            }
        }

        //get cards in fourLocations
        foreach (GameObject go in fourLocations)
        {
            //get cards in the drawnCardHolder
            parent = go.transform;

            while (parent.childCount > 0)
            {
                parent = parent.GetChild(0);
                FlippableCard flippableCard = parent.GetComponent<FlippableCard>();
                if (flippableCard != null)
                {
                    flippableCard.isInAce = true;
                    flippableCard.isDraw = false;
                }
            }
        }
    }

    //#region Sound Section
    //public void PlayDealSound()
    //{
    //    SettingsManager.instance.PlayDealSound();
    //}

    //public void PlayVictoryCheerSound()
    //{
    //    SettingsManager.instance.PlayVictoryCheerSound();
    //}

    //public void PlayClickSound()
    //{
    //    SettingsManager.instance.PlayClickSound();
    //}

    //public void PlayFlipSound()
    //{
    //    SettingsManager.instance.PlayFlipSound();
    //}

    //public void PlayMovedToAce()
    //{
    //    SettingsManager.instance.PlayCheersSound();
    //}
    //#endregion Sound Section

    #region history
    /**
     * A special class to track move history
     */
    class History
    {
        public GameObject child, lastParent;

        public Vector3 localPosition;

        public bool isDrawnDeckRefresh=false, childFlip, parentFlip, undoRepeat=false;

        public History()
        {
            isDrawnDeckRefresh = true;
        }

        public History(GameObject child, GameObject lastParent, Vector3 localPosition, bool childFlip, bool parentFlip)
        {
            this.child = child;
            this.lastParent = lastParent;
            this.localPosition = localPosition;
            this.childFlip = childFlip;
            this.parentFlip = parentFlip;
        }

        public History(GameObject child, GameObject lastParent, Vector3 localPosition, bool childFlip, bool parentFlip, bool undoRepeat)
            : this (child, lastParent, localPosition, childFlip, parentFlip)
        {
            this.undoRepeat = undoRepeat;
        }

        override public string ToString()
        {
            return "History [ "
                +"child "+child.name
                + " lastParent " + lastParent.name
                + " localPosition " + localPosition
                + " childFlip " + childFlip
                + " parentFlip " + parentFlip
                + " undoRepeat " + undoRepeat
                + " ]";
        }
    }

    /**
     * Runs the undo move
     */
    public void RunUndo()
    {
        SettingsManager.instance.UndoMove();
    }

    /**
     * undoes one of the moves saved in the history stack
     */
    public bool Undo()
    {
        if (histories.Count<1) return false;
        History history = histories.Pop();

        if (history.isDrawnDeckRefresh)
        {
            SettingsManager.instance.UndoRefreshDeck(deckRefreshText);

            //add the back to this card and set alpha back to normal
            while (deck.transform.childCount > 0)
            {
                GameObject lastChild = GetLastChild(deck);

                Transform parentTransform = GetLastChild(drawnCardHolder).transform;

                lastChild.transform.SetParent(parentTransform);
                //lastChild.transform.localPosition = Vector3.zero;
                FlippableCard flippable = lastChild.GetComponent<FlippableCard>();
                flippable.Flip();
                flippable.isDraw = true;
                flippable.MoveTo(Vector3.zero);
            }

            countUndoCount++;
        }
        else if (history.undoRepeat)
        {
            //while the next is undo repeat, then undo it.
            while (true)
            {
                UndoOne(history);

                //if the current history is not repeat then stop repeating
                if (!history.undoRepeat) break;

                history = histories.Pop();
            }

            DrawController.instance.ResetDeckThree();
        }
        else
        {
            UndoOne(history);
        }

        //set the display to show the new number of moves
        movesText.text = "" + (SettingsManager.instance.IsCountUndo() ? countUndoCount : histories.Count);

        //set undos for undo limit (limit is 5)
        undoLimitText.text = SettingsManager.instance.GetCurrentUndosLeft() + "";

        return true;
    }

    private void UndoOne(History history)
    {
        countUndoCount++;

        history.child.transform.SetParent(history.lastParent.transform);
        //history.child.transform.localPosition = history.localPosition;

        FlippableCard flippableCardsChild = history.child.GetComponent<FlippableCard>();
        if (flippableCardsChild != null)
        {
            //TODO: update history later
            //flippableCardsChild.MoveTo(history.localPosition);
            //flippableCardsChild.transform.localPosition = history.localPosition;
            flippableCardsChild.MoveTo(history.localPosition);

            if (history.childFlip)
            {
                flippableCardsChild.Flip();
            }
        }

        FlippableCard flippableCardsParent = history.lastParent.GetComponent<FlippableCard>();
        if (flippableCardsParent != null)
        {
            if (history.parentFlip)
            {
                flippableCardsParent.Flip();
            }

            flippableCardsChild.isDraw = flippableCardsParent.isDraw;
        }
    }

    /**
     * Adds a draw deck reset to history
     */
    public void AddHistory()
    {
        histories.Push(new History());
        countUndoCount++;
        movesText.text = "" + (SettingsManager.instance.IsCountUndo() ? countUndoCount : histories.Count);
    }

    /**
     * Adds a normal move history element to the histories stack
     */
    public void AddHistory(GameObject child, GameObject lastParent, Vector3 localPosition, bool childFlip, bool parentFlip)
    {
        histories.Push(new History(child, lastParent, localPosition, childFlip, parentFlip));
        countUndoCount++;
        movesText.text = "" + (SettingsManager.instance.IsCountUndo() ? countUndoCount : histories.Count);
    }

    /**
     * Adds a normal move history element to the histories stack
     */
    public void AddHistory(GameObject child, GameObject lastParent, Vector3 localPosition, bool childFlip, bool parentFlip, bool undoRepeat)
    {
        histories.Push(new History(child, lastParent, localPosition, childFlip, parentFlip, undoRepeat));
        countUndoCount++;
        movesText.text = "" + (SettingsManager.instance.IsCountUndo() ? countUndoCount : histories.Count);
    }
    #endregion history
}