using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/**
 * A typical FreeCell game is played with a typical deck of 52 playing cards spread randomly throughout 8 locations.
 * Every card is played face up and the first 4 rows have an extra card.
 * 
 * There are 4 additional locations (Typically located at the top left of the play area) so that any single card can be placed there.
 * There are also another 4 additional locations (Typically located at the top right of the play area) so that a string from Ace to King can be made.
 * 
 * The game is won when all four Ace locations contain a stack of consecutive cards from Ace to King.
 * 
 * A card may be moved by being dragged, or by clicking the card and then clicking the movement location.
 * A group of cards may only be moved if there are enough empty spaces (out of the 4 to the top left and the original 8)
 * to move each card in the stack 1 by 1 from top to bottom.
 *      I.E.
 *          A group of 8,7 can be placed on top of a 9 in the original 8 locations iff there is at least 1 empty space in the free locations.
 */
public class FreeCellManager : MonoBehaviour
{
    public static FreeCellManager instance;

    //STATIC VARIABLES
    private const string FASTEST_TIME_PREFS = "FreeCellSolitaireFastestTime",
        LEAST_MOVES_PREFS = "FreeCellSolitaireLeastMoves"
    ;

    [SerializeField] GameObject undoLimitTitle;

    public static Vector3 FREECELL_CARD_PLACEMENT_LOCAL_POSITION = new Vector3(0, -30);

    // Normal raycasts do not work on UI elements, they require a special kind
    GraphicRaycaster raycaster;

    [Header("Use these objects to track the undo if that setting is turned on")]
    [SerializeField] Text undoLimitText;

    [SerializeField] Transform[] fourOpenLocations = new Transform[4];
    [SerializeField] Transform[] fourAceLocations = new Transform[4];
    [SerializeField] Transform[] eightLocations = new Transform[8];

    [SerializeField] Transform playArea;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform instantiationLocation;

    public static bool isMoving;

    //track the click history
    private Stack<History> histories = new Stack<History>();

    //Our stopwatch object used to track the time
    private StopWatch stopWatch;

    //The text to show players their scores
    private Text timerText, movesText;
    [SerializeField] int countUndoCount = 0;

    void Awake()
    {
        if (instance == null)
        {
            instance = this; // In first scene, make us the singleton.
        }
        else if (instance != this)
        {
            Destroy(instance.gameObject); // On reload, singleton already set, so destroy duplicate.
            instance = this;
        }

        // Get both of the components we need to do this
        raycaster = GetComponent<GraphicRaycaster>();

        stopWatch = transform.GetComponent<StopWatch>();

        SettingsManager.RESET = ResetGame;
        SettingsManager.UNDO = Undo;

        timerText = GameObject.Find("TimerText").GetComponent<Text>();

        movesText = GameObject.Find("MovesText").GetComponent<Text>();

        #region handle reset from setting changes
        undoLimitTitle.SetActive(SettingsManager.instance.IsLimitUndo());
        undoLimitText.gameObject.SetActive(SettingsManager.instance.IsLimitUndo());
        undoLimitText.text = SettingsManager.instance.GetCurrentUndosLeft() + "";
        #endregion handle reset from setting changes

        StartGame();

        stopWatch.StartStopWatch(SetTime);
    }

    // Update is called once per frame
    void Update()
    {
        if (
            SettingsManager.instance.IsGameActive() && 
            !SettingsManager.instance.IsSettingsOpen() && 
            Input.GetMouseButtonDown(0) && 
            !isMoving
        )
        {
            //Set up the new Pointer Event
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            List<RaycastResult> results = new List<RaycastResult>();

            //Raycast using the Graphics Raycaster and mouse click position
            pointerData.position = Input.mousePosition;
            raycaster.Raycast(pointerData, results);

            //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
            foreach (RaycastResult result in results)
            {
                FreeCellCard card = result.gameObject.GetComponent<FreeCellCard>();

                if (card != null && CanStackMove(result.gameObject.transform))
                {
                    card.CardClicked();
                    return;
                }
            }
        }

        if (Input.GetMouseButton(1))
        {
            AutoComplete();
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
     * Change the display of the timer
     * 
     * This is a callback sent to StopWatch
     */
    public void SetTime(float time)
    {
        timerText.text = SettingsManager.instance.FormatTime(time);
    }

    /**
     * Resets the game
     */
    public void ResetGame()
    {
        SettingsManager.instance.SetIsGameActive(false);

        histories.Clear();

        countUndoCount = 0;

        stopWatch.ResumeStopWatch();

        stopWatch.RestartStopWatch();

        movesText.text = "" + (SettingsManager.instance.IsCountUndo() ? countUndoCount : histories.Count);

        #region handle reset from setting changes
        undoLimitTitle.SetActive(SettingsManager.instance.IsLimitUndo());
        undoLimitText.gameObject.SetActive(SettingsManager.instance.IsLimitUndo());
        undoLimitText.text = SettingsManager.instance.GetCurrentUndosLeft() + "";
        #endregion handle reset from setting changes

        //moves all last children cards to instantiationLocation and then AssignCards
        foreach (FreeCellCard playChild in playArea.GetComponentsInChildren<FreeCellCard>())
        {
            playChild.transform.SetParent(instantiationLocation);
            playChild.SetIsClicked(false);
        }

        AssignCards();
        SettingsManager.instance.SetIsGameActive(true);
        isMoving = false;
    }

    /**
     * Turns on and off the settings menu
     */
    public void ToggleSettings()
    {
        SettingsManager.instance.ToggleSettings();
    }

    /**
     * Looks through all openLocations and eightLocations,
     * if no cards, setVictory
     * 
     * else, no Victory
     */
    private void CheckForVictory()
    {
        if (HasVictory())
        {
            SetVictoryScreen();
        }

        bool HasVictory()
        {
            foreach (Transform loc in eightLocations.Concat(fourOpenLocations))
            {
                if (loc.childCount > 0) return false;
            }

            return true;
        }
    }

    /**
     * Systematically searches the bottomChild in the Ace locations and find the next card needed.
     * 
     * Then, searches the bottomChild in the eightLocations for the card needed.
     */
    private void AutoComplete()
    {
        // if the bottomChild in the Ace location is King, continue
        //else, get the card and see if the bottom of eight is nextCard

        //get the lowest child of each Ace location
        foreach(Transform aceLocation in fourAceLocations)
        {
            Transform lowestChild = GetLowestChild(aceLocation);

            //see if child has a FreeCellCard
            FreeCellCard card = lowestChild.GetComponent<FreeCellCard>();

            if(card == null)
            {
                //look for Ace ... any ace
                FreeCellCard foundCard = FindCard();

                if(foundCard != null)
                {
                    foundCard.SetParentOnMove(foundCard.transform.parent);
                    foundCard.SetTransformOnMove(foundCard.transform.localPosition);

                    PlaceCard(foundCard, lowestChild, Vector3.zero);
                    AutoComplete();
                    return;
                }
            }
            else
            {
                foreach (Transform eightChild in eightLocations.Concat(fourOpenLocations))
                {
                    Transform lowesteightChild = GetLowestChild(eightChild);

                    //see if child has a FreeCellCard
                    FreeCellCard childCard = lowesteightChild.GetComponent<FreeCellCard>();

                    if (card != null && card.IsNextCardAtoK(childCard))
                    {
                        childCard.SetParentOnMove(childCard.transform.parent);
                        childCard.SetTransformOnMove(childCard.transform.localPosition);

                        PlaceCard(childCard, lowestChild, Vector3.zero);
                        AutoComplete();
                        return;
                    }
                }
            }
        }

        FreeCellCard FindCard()
        {
            foreach(Transform eightChild in eightLocations.Concat(fourOpenLocations))
            {
                Transform lowesteightChild = GetLowestChild(eightChild);

                //see if child has a FreeCellCard
                FreeCellCard card = lowesteightChild.GetComponent<FreeCellCard>();

                if(card != null && card.GetNumber() == 0)
                {
                    return card;
                }
            }

            return null;
        }
    }

    /**
     * Count the number of children here.
     * 
     * count the number of open spaces in openLocations and eightLocations
     * 
     * if there is enough space, return true, else, return false.
     * 
     * Use formula (2^openEight)*(openFour+1)
     */
    private bool CanStackMove(Transform stack)
    {
        int count = CountChildren(stack, 0);

        int numberCanMove = CountCardsCanMove();

        return count < numberCanMove;

        int CountChildren(Transform transform, int count)
        {
            return transform.childCount > 0 ? CountChildren(transform.GetChild(0), count + 1) : count;
        }

        int CountCardsCanMove()
        {
            int openEight = CountOpenEight();
            int openFour = CountOpenFour();

            return ((int) Mathf.Pow(2, openEight))*(openFour+1);

            int CountOpenEight()
            {
                int eightCount = 0;
                foreach(Transform eightTransform in eightLocations)
                {
                    if (eightTransform.childCount < 1) eightCount++;
                }
                return eightCount;
            }

            int CountOpenFour()
            {
                int openFour = 0;
                foreach (Transform fourTransform in fourOpenLocations)
                {
                    if (fourTransform.childCount < 1) openFour++;
                }
                return openFour;
            }
        }
    }

    private void StartGame()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 13; j++)
            {
                GameObject newCard = Instantiate(cardPrefab, instantiationLocation);

                newCard.name = i + "i_" + j + "j";

                FreeCellCard freeCellCard = newCard.GetComponent<FreeCellCard>();

                if (freeCellCard != null)
                {
                    freeCellCard.SetCard(i, j, playArea);
                    freeCellCard.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/SolitaireCards/" + freeCellCard.GetSuitWord() + "/" + freeCellCard.GetNumberAsChar() + freeCellCard.GetSuit());
                }
                else
                {
                    throw new System.Exception("Prefab does not have a FreeCellCard.");
                }
            }
        }

        //set the cards
        AssignCards();
    }

    public void AssignCards()
    {
        int placement = 0;
        bool reachedOnce = false;
        while(instantiationLocation.childCount > 0)
        {
            int randIndex = Random.Range(0, instantiationLocation.childCount);
            Transform child = instantiationLocation.GetChild(randIndex);

            child.SetParent(GetLowestChild(eightLocations[placement]));
            child.localPosition = reachedOnce ? FREECELL_CARD_PLACEMENT_LOCAL_POSITION : Vector3.zero;

            placement++;
            if (placement > 7)
            {
                placement = 0;
                reachedOnce = true;
            }
        }
    }

    public bool TryToPlace(FreeCellCard freeCellCard)
    {
        isMoving = false;

        RectTransform rectTransformTop = (RectTransform)fourOpenLocations[0],
            rectTransformBottom = (RectTransform)eightLocations[0];
        float topYLimit = rectTransformTop.localPosition.y - (rectTransformTop.rect.height / 2),
            bottomYLimit = rectTransformBottom.localPosition.y + (rectTransformBottom.rect.height / 2);

        RectTransform cardRectTransform = (RectTransform)freeCellCard.transform;
        float cardTop = cardRectTransform.localPosition.y + (cardRectTransform.rect.height / 2),
            cardBottom = cardRectTransform.localPosition.y - (cardRectTransform.rect.height / 2);

        //check Y position
        if (cardTop > topYLimit)
        {
            if (TryToPlaceUpper(freeCellCard)) return true;
        }
        else if (cardBottom < bottomYLimit)
        {
            if (TryToPlaceLower(freeCellCard)) return true;
        }

        return false;
    }

    private bool TryToPlaceUpper(FreeCellCard freeCellCard)
    {
        RectTransform rectTransform = (RectTransform)freeCellCard.transform;
        float leftXLimit = rectTransform.localPosition.x - (rectTransform.rect.width / 2),
            rightXLimit = rectTransform.localPosition.x + (rectTransform.rect.width / 2);

        //grab the min and max of the upper 4
        foreach (RectTransform t in fourAceLocations)
        {
            float minX = t.localPosition.x - (t.rect.width / 2),
                maxX = t.localPosition.x + (t.rect.width / 2);

            if (minX < leftXLimit && maxX > leftXLimit)
            {
                if (PlaceCardOnThis(freeCellCard, GetLowestChild(t), 2)) return true;
            }

            if (minX < rightXLimit && maxX > rightXLimit)
            {
                if (PlaceCardOnThis(freeCellCard, GetLowestChild(t), 2)) return true;
            }
        }
        foreach (RectTransform t in fourOpenLocations)
        {
            //make sure that card does not have children
            if (freeCellCard.transform.childCount > 0) break;

            float minX = t.localPosition.x - (t.rect.width / 2),
                maxX = t.localPosition.x + (t.rect.width / 2);

            if (minX < leftXLimit && maxX > leftXLimit)
            {
                if (PlaceCardOnThis(freeCellCard, GetLowestChild(t), 1)) return true;
            }

            if (minX < rightXLimit && maxX > rightXLimit)
            {
                if (PlaceCardOnThis(freeCellCard, GetLowestChild(t), 1)) return true;
            }
        }

        return false;
    }

    private bool TryToPlaceLower(FreeCellCard freeCellCard)
    {
        RectTransform rectTransform = (RectTransform)freeCellCard.transform;
        float leftXLimit = rectTransform.localPosition.x - (rectTransform.rect.width / 2),
            rightXLimit = rectTransform.localPosition.x + (rectTransform.rect.width / 2);

        //grab the min and max of the upper 4
        foreach (RectTransform t in eightLocations)
        {
            if (t.childCount<1 && !CanMoveLower(freeCellCard.transform)) continue;

            float minX = t.localPosition.x - (t.rect.width / 2),
                maxX = t.localPosition.x + (t.rect.width / 2);

            if (minX < leftXLimit && maxX > leftXLimit)
            {
                if (PlaceCardOnThis(freeCellCard, GetLowestChild(t), 0)) return true;
            }

            if (minX < rightXLimit && maxX > rightXLimit)
            {
                if (PlaceCardOnThis(freeCellCard, GetLowestChild(t), 0)) return true;
            }
        }

        return false;

        bool CanMoveLower(Transform stack)
        {
            int count = CountChildren(stack, 0);

            int numberCanMove = CountCardsCanMove();

            return count < numberCanMove;

            int CountChildren(Transform transform, int count)
            {
                return transform.childCount > 0 ? CountChildren(transform.GetChild(0), count + 1) : count;
            }

            int CountCardsCanMove()
            {
                int openEight = CountOpenEight()-1;
                int openFour = CountOpenFour();

                return ((int)Mathf.Pow(2, openEight)) * (openFour + 1);

                int CountOpenEight()
                {
                    int eightCount = 0;
                    foreach (Transform eightTransform in eightLocations)
                    {
                        if (eightTransform.childCount < 1) eightCount++;
                    }
                    return eightCount;
                }

                int CountOpenFour()
                {
                    int openFour = 0;
                    foreach (Transform fourTransform in fourOpenLocations)
                    {
                        if (fourTransform.childCount < 1) openFour++;
                    }
                    return openFour;
                }
            }
        }
    }

    private bool PlaceCardOnThis(FreeCellCard card, Transform t, int location)
    {
        if (card == null || t == null) return false;

        //see if transform has a card
        FreeCellCard freeCellCard = t.GetComponent<FreeCellCard>();

        if (freeCellCard == null)
        {
            if (location == 2 && card.GetNumber() != 0) return false;

            //place this as a child
            PlaceCard(card, t, Vector3.zero);
            return true;
        }
        else
        {
            switch (location)
            {
                case 0:
                    if (freeCellCard.IsNextCardKtoA(card))
                    {
                        PlaceCard(card, t, FREECELL_CARD_PLACEMENT_LOCAL_POSITION);
                        return true;
                    }
                    return false;
                case 1:
                    return false;
                case 2:
                    if (freeCellCard.IsNextCardAtoK(card))
                    {
                        PlaceCard(card, t, Vector3.zero);
                        return true;
                    }
                    return false;
            }
        }

        return false;
    }

    private void PlaceCard(FreeCellCard card, Transform parent, Vector3 position)
    {
        AddHistory(card, card.GetParentOnMove(), card.GetTransformOnMove());

        card.transform.SetParent(parent);
        card.transform.localPosition = position;
        card.SetIsClicked(false);

        CheckForVictory();
    }

    public Transform GetFourOpenLocation(int i)
    {
        return GetLowestChild(fourOpenLocations[i]);
    }

    public Transform GetFourAceLocation(int i)
    {
        return GetLowestChild(fourAceLocations[i]);
    }

    public Transform GetEightLocation(int i)
    {
        return GetLowestChild(eightLocations[i]);
    }

    private Transform GetLowestChild(Transform t)
    {
        if (t == null) return null;

        if (t.childCount < 1) return t;

        Transform child = t.GetChild(0);

        while (child.childCount > 0)
        {
            child = child.GetChild(0);
        }

        return child;
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
            if (!addedTime && (fastestTime == 0 || stopWatchTime < fastestTime))
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

            if (!addedMove && (leastMove == 0 || moves < leastMove))
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

        for (int i = 0; i < 10; i++)
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

    #region history
    /**
     * A special class to track move history
     */
    class History
    {
        public FreeCellCard childCard;

        public Transform lastParent;

        public Vector3 localPosition;

        public History(FreeCellCard childCard, Transform lastParent, Vector3 localPosition)
        {
            this.childCard = childCard;
            this.lastParent = lastParent;
            this.localPosition = localPosition;
        }

        override public string ToString()
        {
            return "History [ "
                + "childCard " + childCard.name
                + " lastParent " + lastParent.name
                + " localPosition " + localPosition
                + " ]";
        }
    }

    /**
     * undoes one of the moves saved in the history stack
     */
    public bool Undo()
    {
        if (histories.Count < 1) return false;
        History history = histories.Pop();

        countUndoCount++;
        history.childCard.transform.SetParent(history.lastParent);
        history.childCard.transform.localPosition = history.localPosition;

        //set the display to show the new number of moves
        movesText.text = "" + (SettingsManager.instance.IsCountUndo() ? countUndoCount : histories.Count);

        //set undos for undo limit (limit is 5)
        undoLimitText.text = SettingsManager.instance.GetCurrentUndosLeft() + "";

        return true;
    }

    /**
     * Adds a normal move history element to the histories stack
     */
    public void AddHistory(FreeCellCard childCard, Transform lastParent, Vector3 localPosition)
    {
        countUndoCount++;

        histories.Push(new History(childCard, lastParent, localPosition));
        movesText.text = "" + (SettingsManager.instance.IsCountUndo() ? countUndoCount : histories.Count);
    }
    #endregion history
}
