using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpiderController : MonoBehaviour
{
    public static SpiderController instance;

    //STATIC VARIABLES
    private const string FASTEST_TIME_PREFS = "SpiderSolitaireFastestTime",
        LEAST_MOVES_PREFS = "SpiderSolitaireLeastMoves"
    ;

    [Header("The 10 columns.")]
    [SerializeField] Transform[] columns;

    [Header("The 5 draw images, turn off on click or show on undo.")]
    [SerializeField] GameObject[] drawImages;

    [Header("The 5 areas where the cards that are played on drawImages click are stored. Each area has 10 cards.")]
    [SerializeField] Transform[] drawStorageAreas;
    [SerializeField] int currentDraw = 0;

    [Header("Preset the 104 game cards to save instantiation lag")]
    [SerializeField] Transform cardHolder;
    [SerializeField] SpiderCard[] playingCards;

    [Header("The currently selected card")]
    [SerializeField] SpiderCard lastClicked;

    [Header("The number of suits in the game. From 1-4 (inclusive)")]
    [SerializeField] int numberOfSuits=1;

    [SerializeField] Transform run12Location;

    //track the click history
    private Stack<History> histories = new Stack<History>();

    //Our stopwatch object used to track the time
    [SerializeField] StopWatch stopWatch;

    //The text to show players their scores
    [SerializeField] Text timerText, movesText;

    public static bool isMoving;

    // Normal raycasts do not work on UI elements, they require a special kind
    GraphicRaycaster raycaster;

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        BuildCards();

        SettingsManager.RESET = ResetGame;
        SettingsManager.UNDO = Undo;

        // Get both of the components we need to do this
        raycaster = GetComponent<GraphicRaycaster>();

        stopWatch.StartStopWatch(SetTime);
    }

    // Update is called once per frame
    void Update()
    {
        if (
            Input.GetMouseButtonDown(0) && 
            !SettingsManager.instance.IsSettingsOpen() && 
            SettingsManager.instance.IsGameActive() &&
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
                if (HandleClicked(result.gameObject.transform)) return;
            }

            NullifyLastClicked();
        }

        if (Input.GetMouseButton(1))
        {
            AutoComplete();
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

    private bool HandleClicked(Transform clickedObject)
    {
        SpiderCard card = clickedObject.GetComponent<SpiderCard>();

        if(lastClicked == null && card != null)
        {
            if (!card.CanCardMove()) return false;
        }

        if (card != null && card.GetIsCardFaceUp())
        {
            if (lastClicked == null)
            {
                lastClicked = card;

                card.TryToMove();

                ToggleHighlight(clickedObject, true);
                return true;
            }
            
            return CardClicked(card);
        }
        else if(clickedObject.name.Contains("column") && lastClicked != null)
        {
            ColumnClicked(clickedObject);
            return true;
        }
        else if (clickedObject.name.Contains("DrawImage"))
        {
            DrawClicked(clickedObject);
            return true;
        }

        NullifyLastClicked();

        VerifyCardsFlipped();

        return false;

        void VerifyCardsFlipped()
        {
            foreach(Transform t in columns)
            {
                SpiderCard card = GetLowestChild(t).GetComponent<SpiderCard>();

                if(card!= null)
                {
                    if (!card.GetIsCardFaceUp())
                    {
                        card.SetIsCardFaceUp(true);
                    }
                }
            }
        }
    }

    private void DrawClicked(Transform transform)
    {
        //make sure all columns have children
        foreach(Transform column in columns)
        {
            if (column.childCount < 1) return;
        }

        GetLowestChild(transform).gameObject.SetActive(false);

        for(int i=0;i<10;i++)
        {
            Transform t = drawStorageAreas[currentDraw].GetChild(0);

            t.SetParent(GetLowestChild(columns[i]));
            t.localPosition = SpiderCard.CARD_PLACEMENT_DIFFERENCE;
        }

        AddHistory();

        currentDraw++;
    }

    private bool CardClicked(SpiderCard card)
    {
        if (card.IsNextCard(lastClicked))
        {
            lastClicked.PlaceCard(card.transform, SpiderCard.CARD_PLACEMENT_DIFFERENCE, true, false);
            NullifyLastClicked();
            return true;
        }
        return false;
    }

    private void ColumnClicked(Transform column)
    {
        lastClicked.PlaceCard(column, Vector3.zero, false, false);
        NullifyLastClicked();
    }

    void NullifyLastClicked()
    {
        if (lastClicked != null)
        {
            ToggleHighlight(lastClicked.transform, false);
        }
        lastClicked = null;
    }

    public void ToggleHighlight(Transform parent, bool onOrOff)
    {
        parent.GetComponent<Outline>().effectColor = onOrOff ? Color.blue : Color.black;
        if (parent.childCount > 0)
        {
            ToggleHighlight(parent.GetChild(0), onOrOff);
        }
    }

    public void OpenSettings()
    {
        SettingsManager.instance.ToggleSettings();
    }

    private void AutoComplete()
    {

    }

    public bool TryToPlace(SpiderCard spiderCard)
    {
        foreach(Transform column in columns)
        {
            //get the minX and maxX from the column
            RectTransform columnRectTransform = (RectTransform)column.transform;
            float columnMinX = columnRectTransform.position.x - (columnRectTransform.rect.width / 2),
                columnMaxX = columnRectTransform.position.x + (columnRectTransform.rect.width / 2);

            if (columnMinX < spiderCard.transform.position.x &&
                columnMaxX > spiderCard.transform.position.x)
            {
                SpiderCard card = GetLowestChild(column).GetComponent<SpiderCard>();
                if (card != null && !card.GetIsCardFaceUp()) return false;

                return HandleClicked(GetLowestChild(column));
            }
        }

        return false;
    }

    /**
     * Get bottom card here if it is A, check parent for numb+1 if reached numb==12 
     *      move to run12Location and
     *      return true
     *      
     *  else, return false.
     */
    public void CheckForRunKToA(Transform t)
    {
        SpiderCard parent = CheckParent(GetLowestChild(t), 0);
        if (parent != null)
        {
            parent.SetParentBeforeMove(parent.transform.parent);
            parent.PlaceCard(GetLowestChild(run12Location), new Vector3(30, 0), false, true);

            SetPosition(parent.transform.GetChild(0));
            CheckVictory();

            void SetPosition(Transform t)
            {
                t.localPosition = Vector3.zero;
                if (t.childCount > 0) SetPosition(t.GetChild(0));
            }
        }

        SpiderCard CheckParent(Transform child, int expextedNumber)
        {
            SpiderCard card = child.GetComponent<SpiderCard>();

            if (card == null) return null;

            if (card.GetNumber() == 12 && expextedNumber == 12) return card;

            return card.GetNumber()==expextedNumber?CheckParent(child.parent, expextedNumber+1):null;
        }
    }

    /**
     * if all cards are gone, return true.
     * else return false.
     */
    private void CheckVictory()
    {
        foreach(Transform t in columns)
        {
            if (t.childCount > 0) return;
        }

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

        int moves = histories.Count;

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

        string currentScore = "Time: " + timerText.text + "\nMoves: " + histories.Count;

        SettingsManager.instance.SetVictory(currentScore, fastestTimesString, leastMovesString);
    }

    /**
     * builds all 104 cards and assigns them to their locations.
     */
    private void BuildCards()
    {
        //create an int list from 0-103
        List<int> intList = new List<int>();
        for(int i = 0; i < 104; i++)
        {
            intList.Add(i);
        }
        intList = Shuffle(intList);

        List<int> Shuffle(List<int> toShuffle)
        {
            List<int> returnableList = new List<int>();
            while (toShuffle.Count > 0)
            {
                int randIndex = Random.Range(0, toShuffle.Count);
                returnableList.Add(toShuffle[randIndex]);
                toShuffle.RemoveAt(randIndex);
            }

            return returnableList;
        }

        int suit = 0;
        int storageNumber = 0;
        int storageArea = 0;

        for(int i=0; i < 104; i++)
        {
            int number = intList[i] % 13;
            int parent = i % 10;

            if (i < 54)
            {
                playingCards[intList[i]].transform.SetParent(GetLowestChild(columns[parent]));

                if (i < 10)
                {
                    playingCards[intList[i]].transform.localPosition = Vector3.zero;
                }
                else
                {
                    playingCards[intList[i]].transform.localPosition = SpiderCard.CARD_PLACEMENT_DIFFERENCE;
                }

                if (i <44)
                {
                    playingCards[intList[i]].SetCard(suit, number, false);
                }
                else
                {
                    playingCards[intList[i]].SetCard(suit, number, true);
                }
            }
            else
            {
                playingCards[intList[i]].transform.SetParent(drawStorageAreas[storageArea]);
                playingCards[intList[i]].SetCard(suit, number, true);

                storageNumber++;
                if(storageNumber > 9)
                {
                    storageNumber = 0;
                    storageArea++;
                }
            }

            if (i>12 && number == 0) suit = (suit + 1) % numberOfSuits;
        }
    }
    
    /**
     * Requires transform parent != null
     * 
     * if parent has children, recursively look at first child until a child is no longer there.
     * 
     * return the lowest child (or parent if no children) found
     */
    private Transform GetLowestChild(Transform parent)
    {
        if (parent == null) return null;

        if (parent.childCount > 0)
        {
            Transform child = parent.GetChild(0);

            if (child.gameObject.activeInHierarchy)
            {
                return GetLowestChild(child);
            }
        }

        return parent;
    }

    private void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    #region history
    /**
     * A special class to track move history
     */
    class History
    {
        public Transform child, lastParent;

        public Vector3 localPosition;

        public bool isFlipParent = false, isUndoTwice = false, isDrawCards = false;

        #region methods
        //on Draw cards
        public History()
        {
            isDrawCards = true;
        }

        public History(Transform child, Transform lastParent, Vector3 localPosition)
        {
            this.child = child;
            this.lastParent = lastParent;
            this.localPosition = localPosition;
        }

        public History(Transform child, Transform lastParent, Vector3 localPosition, bool isFlipParent)
            : this(child, lastParent, localPosition)
        {
            this.isFlipParent = isFlipParent;
        }

        public History(Transform child, Transform lastParent, Vector3 localPosition, bool isFlipParent, bool isUndoTwice)
            : this(child, lastParent, localPosition)
        {
            this.isFlipParent = isFlipParent;
            this.isUndoTwice = isUndoTwice;
        }

        override public string ToString()
        {
            return "History [ "
                + "child " + child.name
                + " lastParent " + lastParent.name
                + " localPosition " + localPosition
                + " isFlipParent " + isFlipParent
                + " isUndoTwice " + isUndoTwice
                + " isDrawCards " + isDrawCards
                + " ]";
        }
        #endregion methods
    }

    /**
     * undoes one of the moves saved in the history stack
     */
    public bool Undo()
    {
        if (histories.Count < 1) return false;
        History history = histories.Pop();

        if (history.isDrawCards)
        {
            UndoDraw();
        }
        else
        {
            UndoOne(history);
        }

        movesText.text = histories.Count + "";
        return true;
    }

    private void UndoDraw()
    {
        currentDraw--;

        drawImages[currentDraw].SetActive(true);

        foreach (Transform column in columns)
        {
            GetLowestChild(column).SetParent(drawStorageAreas[currentDraw]);
        }
    }

    private void UndoOne(History history)
    {
        history.child.SetParent(history.lastParent);
        history.child.localPosition = history.localPosition;

        Transform child = history.child;
        if (child.childCount > 0) SetChildrenPos(child.GetChild(0));

        if (history.isFlipParent)
        {
            history.lastParent.GetComponent<SpiderCard>().SetIsCardFaceUp(false);
        }

        if (history.isUndoTwice) Undo();

        void SetChildrenPos(Transform start)
        {
            start.localPosition = SpiderCard.CARD_PLACEMENT_DIFFERENCE;
            if (start.childCount > 0) SetChildrenPos(start.GetChild(0));
        }
    }

    /**
     * Adds a draw deck reset to history
     */
    public void AddHistory()
    {
        histories.Push(new History());

        movesText.text = histories.Count+"";
    }

    /**
     * Adds a normal move history element to the histories stack
     */
    public void AddHistory(Transform child, Transform lastParent, Vector3 localPosition)
    {
        histories.Push(new History(child, lastParent, localPosition));
        movesText.text = histories.Count + "";
    }

    /**
     * Adds a normal move history element to the histories stack
     */
    public void AddHistory(Transform child, Transform lastParent, Vector3 localPosition, bool isFlipParent)
    {
        histories.Push(new History(child, lastParent, localPosition, isFlipParent));
        movesText.text = histories.Count + "";
    }

    /**
     * Adds a normal move history element to the histories stack
     */
    public void AddHistory(Transform child, Transform lastParent, Vector3 localPosition, bool isFlipParent, bool isUndoTwice)
    {
        histories.Push(new History(child, lastParent, localPosition, isFlipParent, isUndoTwice));
        movesText.text = histories.Count + "";
    }
    #endregion history
}
