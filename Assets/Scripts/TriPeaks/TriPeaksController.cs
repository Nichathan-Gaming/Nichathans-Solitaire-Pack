using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TriPeaksController : MonoBehaviour // TOP: 15, 21, 27
{
    public static TriPeaksController instance;

    private const string TRI_PEAKS_HIGHEST_COMBO_PREFS = "TriPeaksHighestCombo",
        TRI_PEAKS_HIGHEST_SCORE_PREFS = "TriPeaksHighestScore";

    public GameObject cardPrefab;

    public Transform hiddenCardHolder;

    public Transform[] bottomRow;
    public Transform[] leftPeak;
    public Transform[] middlePeak;
    public Transform[] rightPeak;

    public Text scoreText;
    public Text comboText;

    public Transform deckHolder;
    public Transform currentCardHolder;

    [SerializeField] ParentChildRelationships parentChildRelationships = new ParentChildRelationships();

    public CardDisplay currentCard;

    private int peakMultiplier = 1;
    private int baseScore = 10;
    private int combo = 0;
    private int currentGameHighestcombo = 0;

    private int playerScore = 0;

    [SerializeField] GameObject undoLimitTitle;
    [SerializeField] Text undoLimitText;

    //the list of our history used for undo
    private Stack<History> historiesList = new Stack<History>();

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        #region set Cards
        //card suit from 0-3 {H, D, S, C}
        for (int i = 0; i < 4; i++)
        {
            //card number from 0-12
            for (int j = 0; j < 13; j++)
            {
                #region instantiate new card
                GameObject newCard = Instantiate(cardPrefab, hiddenCardHolder);

                newCard.name = i + "i_" + j + "j";
                #endregion instantiate new card

                #region get and set card image
                CardDisplay cardDisplay = newCard.GetComponent<CardDisplay>();
                if (cardDisplay == null)
                {
                    throw new Exception("CardDisplay for " + newCard.name + " is null.");
                }
                else
                {
                    cardDisplay.SetCard(i, j);
                }
                #endregion get and set card image
            }
        }
        #endregion set Cards

        int[] bottomRow ={10,11,12,16,17,18,22,23,24};
        int[] middleRow = { 13, 14, 19, 20, 25, 26 };
        int[] topRow = { 15, 21, 27 };


        #region Assign Relationships
        for(int i = 0; i < 52; i++)
        {
            int[] childsChildren = null;
            int[] parents = null;
            int additive = 3 * (i < 15 ? 0 : (i < 21 ? 1 : 2));

            //cards are in the bottom row
            if (i < 10)
            {
                if (i == 0)
                {
                    childsChildren = new int[] { 10 };
                }
                else if (i == 9)
                {
                    childsChildren = new int[] { 24 };
                }
                else if (i == 3)
                {
                    childsChildren = new int[] { 12, 16 };
                }
                else if (i == 6)
                {
                    childsChildren = new int[] { 18,22 };
                }
                else
                {
                    int under10Additive = 3 * (i < 3 ? 0 : i < 6 ? 1 : 2);

                    childsChildren = new int[]
                    {
                        i+9+under10Additive,
                        i+10+under10Additive
                    };
                }

                parentChildRelationships.Add(true, i, null, childsChildren);
                continue;
            }

            if(i == 28)
            {
                //currentCard
                parentChildRelationships.Add(false, i, null, null);
                continue;
            }

            if (i == 29)
            {
                parentChildRelationships.Add(true, i, null, new int[] { i + 1 });
                continue;
            }

            //cards are now in deck
            else if(i> 29)
            {
                parentChildRelationships.Add(false, i, new int[] { i - 1 }, (i < 51 ? new int[] { i + 1 } : null));
                continue;
            }

            if (bottomRow.Contains(i))
            {
                parents = new int[]{
                    i-10-additive,
                    i-9-additive
                };

                if (new int[]{ 10,16, 22}.Contains(i)){
                    childsChildren = new int[]{i+3};
                }
                else if (new int[] { 11, 17, 23 }.Contains(i))
                {
                    childsChildren = new int[]
                    {
                        i+2,
                        i+3
                    };
                }
                else
                {
                    childsChildren = new int[]{i+2};
                }
            }

            if (middleRow.Contains(i))
            {
                parents = new int[]{i-3,i-2};

                childsChildren = new int[]{
                    i<15?15:(i<21?21:27)
                };
            }

            if (topRow.Contains(i))
            {
                parents = new int[] { i-1,i-2 };
                //children are null
            }

            parentChildRelationships.Add(false, i, parents, childsChildren);
        }
        #endregion Assign Relationships

        AssignCards();

        SettingsManager.RESET = ResetGame;
        SettingsManager.UNDO = Undo;

        HandleUndo();
    }

    private void HandleUndo()
    {
        #region handle reset from setting changes
        undoLimitTitle.SetActive(SettingsManager.instance.IsLimitUndo());
        undoLimitText.gameObject.SetActive(SettingsManager.instance.IsLimitUndo());
        undoLimitText.text = SettingsManager.instance.GetCurrentUndosLeft() + "";
        #endregion handle reset from setting changes
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ResetGame()
    {
        historiesList.Clear();

        parentChildRelationships.Reset();
        playerScore = 0;
        peakMultiplier = 1;
        scoreText.text = ""+ playerScore;

        foreach(Transform t in bottomRow.Concat(leftPeak).Concat(middlePeak).Concat(rightPeak).ToArray())
        {
            MoveChildrenToHiddenCardHolder(t);
        }
        MoveChildrenToHiddenCardHolder(deckHolder);
        MoveChildrenToHiddenCardHolder(currentCardHolder);

        void MoveChildrenToHiddenCardHolder(Transform parent)
        {
            if (parent == null) return;
            while (parent.childCount > 0)
            {
                parent.GetChild(0).SetParent(hiddenCardHolder);
            }
        }

        AssignCards();
        HandleUndo();
    }

    public void OpenSettings()
    {
        SettingsManager.instance.ToggleSettings();
    }

    public void HandleClickCard(CardDisplay cardDisplay)
    {
        if (!SettingsManager.instance.IsGameActive() || SettingsManager.instance.IsSettingsOpen()) return;

        SettingsManager.instance.PlayClickSound();

        //verify that it can be clicked
        if (!parentChildRelationships.IsActive(cardDisplay.index))
        {
            return;
        }

        if (cardDisplay.index > 27)
        {
            if (cardDisplay.index == 28) return;

            if (parentChildRelationships.IsActive(cardDisplay.index))
            {
                DrawCard(cardDisplay);
            }
            else
            {
                Debug.LogError("Could not Deactivate cardDisplay.index: " + cardDisplay.index);
            }

            return;
        }

        int numb = cardDisplay.cardNumber;

        switch (currentCard.cardNumber)
        {
            case 0:
                if (numb == 12 || numb == 1)
                {
                    HandleCorrectClick();
                }
                break;
            case 1:
                if (numb == 0 || numb == 2)
                {
                    HandleCorrectClick();
                }
                break;
            case 2:
                if (numb == 1 || numb == 3)
                {
                    HandleCorrectClick();
                }
                break;
            case 3:
                if (numb == 2 || numb == 4)
                {
                    HandleCorrectClick();
                }
                break;
            case 4:
                if (numb == 3 || numb == 5)
                {
                    HandleCorrectClick();
                }
                break;
            case 5:
                if (numb == 4 || numb == 6)
                {
                    HandleCorrectClick();
                }
                break;
            case 6:
                if (numb == 5 || numb == 7)
                {
                    HandleCorrectClick();
                }
                break;
            case 7:
                if (numb == 6 || numb == 8)
                {
                    HandleCorrectClick();
                }
                break;
            case 8:
                if (numb == 7 || numb == 9)
                {
                    HandleCorrectClick();
                }
                break;
            case 9:
                if (numb == 8 || numb == 10)
                {
                    HandleCorrectClick();
                }
                break;
            case 10:
                if (numb == 9 || numb == 11)
                {
                    HandleCorrectClick();
                }
                break;
            case 11:
                if (numb == 10 || numb == 12)
                {
                    HandleCorrectClick();
                }
                break;
            case 12:
                if (numb == 11 || numb == 0)
                {
                    HandleCorrectClick();
                }
                break;
        }

        /**
         * Move cardDisplay to currentCardHolder (set parent and localPosition=Vector3.zero),
         * Set currentCard to cardDisplay
         */
        void HandleCorrectClick()
        {
            currentCard = cardDisplay;
            cardDisplay.transform.SetParent(currentCardHolder);
            cardDisplay.transform.localPosition = Vector3.zero;
            parentChildRelationships.Deactivate(cardDisplay.index, true);

            int newScore;
            List<int> scores = new List<int>();

            //see if it has parents and can be clicked, if not, give 500 score
            if (new int[] { 15, 21, 27 }.Contains(cardDisplay.index))
            {
                newScore = 500 * peakMultiplier;
                scores.Add(newScore);
                AddScore(newScore);
                peakMultiplier++;

                SettingsManager.instance.PlayCheersSound();
            }

            //assign score
            newScore = baseScore + (baseScore * combo);
            scores.Add(newScore);
            AddScore(newScore);

            AddHistory(cardDisplay.index, scores.ToArray(), combo);

            SetCombo(combo+1);

            if (CheckVictory()) SetVictory();

            bool CheckVictory()
            {

                foreach (Transform t in leftPeak.Concat(middlePeak).Concat(rightPeak))
                {
                    if (t.GetComponentInChildren<CardDisplay>() != null) return false;
                }

                return true;
            }
        }
    }

    private void SetVictory()
    {
        if (combo > currentGameHighestcombo) currentGameHighestcombo = combo;

        string gameScore = "Score : " + playerScore + "\nHighest Combo : "+currentGameHighestcombo;

        GetTopTenVictoryScores();

        /**
         * Builds the info for the victory screen
         */
        void GetTopTenVictoryScores()
        {
            string highestScoreString = "Highest Scores", highestComboString= "Highest Combos";
            List<int> highestCombos = new List<int>();
            List<int> highestScores = new List<int>();

            bool addedCombo = false;
            bool addedScore = false;

            int lastCombo = 0;
            int lastScore = 0;

            //gather the top 10 
            for (int i = 0; i < 10; i++)
            {
                #region high combo
                //the previous time at this location
                int highestCombo = PlayerPrefs.GetInt(TRI_PEAKS_HIGHEST_COMBO_PREFS + i, 0);

                //if we haven't added a time yet and the current time is faster
                if (!addedCombo && (highestCombo == 0 || currentGameHighestcombo > highestCombo))
                {
                    addedCombo = true;
                    lastCombo = highestCombo;
                    highestCombos.Add(currentGameHighestcombo);
                }
                else
                {
                    if (addedCombo)
                    {
                        highestCombos.Add(lastCombo);
                        lastCombo = highestCombo;
                    }
                    else
                    {
                        highestCombos.Add(highestCombo);
                    }
                }
                #endregion high combo

                #region Highest Scores
                int highestScore = PlayerPrefs.GetInt(TRI_PEAKS_HIGHEST_SCORE_PREFS + i, 0);

                if (!addedScore && (highestScore == 0 || playerScore > highestScore))
                {
                    addedScore = true;
                    lastScore = highestScore;
                    highestScores.Add(playerScore);
                }
                else
                {
                    if (addedScore)
                    {
                        highestScores.Add(lastScore);
                        lastScore = highestScore;
                    }
                    else
                    {
                        highestScores.Add(highestScore);
                    }
                }
                #endregion least moves
            }

            //set the player prefs if changed
            if (addedScore || addedCombo)
            {
                for (int i = 0; i < 10; i++)
                {
                    PlayerPrefs.SetInt(TRI_PEAKS_HIGHEST_COMBO_PREFS + i, highestCombos[i]);
                    PlayerPrefs.SetInt(TRI_PEAKS_HIGHEST_SCORE_PREFS + i, highestScores[i]);
                }
            }

            //build the strings to display info
            for (int i = 0; i < 10; i++)
            {
                highestComboString += "\n" + highestCombos[i];
                highestScoreString += "\n" + highestScores[i];
            }

            SettingsManager.instance.SetVictory(gameScore, highestScoreString, highestComboString);
        }
    }

    private void AssignCards()
    {
        AssignFirst28();
        AssignDeck();

        void AssignFirst28()
        {
            int cardIndex = 0;
            //bottomRow.Concat(leftPeak).Concat(middlePeak).Concat(rightPeak).ToArray()
            MoveToPosition(bottomRow, false);
            MoveToPosition(leftPeak.Concat(middlePeak).Concat(rightPeak).ToArray(), true);

            //Place one in the current card holder
            MoveOneToPosition(currentCardHolder, false, Vector3.zero);
            currentCard = currentCardHolder.GetChild(0).GetComponent<CardDisplay>();

            void MoveToPosition(Transform[] array, bool isBack)
            {
                foreach (Transform t in array)
                {
                    MoveOneToPosition(t, isBack, Vector3.zero);
                }
            }

            void MoveOneToPosition(Transform t, bool isBack, Vector3 localPosition)
            {
                if (hiddenCardHolder.childCount < 1) return;

                //get a random card from the children of hiddencardholder
                int randomIndex = Random.Range(0, hiddenCardHolder.childCount);

                //grab the child
                Transform child = hiddenCardHolder.GetChild(randomIndex);

                //give the child a new parent
                child.SetParent(t);

                //move the child to the parent
                child.localPosition = localPosition;

                CardDisplay cardDisplay = child.GetComponent<CardDisplay>();
                cardDisplay.SetImage(!isBack);
                //cardDisplay.thisImage.sprite = isBack ? cardDisplay.backImage : cardDisplay.frontImage;
                cardDisplay.index = cardIndex;

                parentChildRelationships.SetCardDisplay(cardIndex, cardDisplay);

                cardIndex++;
            }
        }

        void AssignDeck()
        {
            int cardIndex = 51;
            //assign the rest to the deck
            if (hiddenCardHolder.childCount > 0)
            {
                for (int i = 0; hiddenCardHolder.childCount > 0; i++)
                {
                    if (hiddenCardHolder.childCount < 1) return;

                    //get a random card from the children of hiddencardholder
                    int randomIndex = Random.Range(0, hiddenCardHolder.childCount);

                    //grab the child
                    Transform child = hiddenCardHolder.GetChild(randomIndex);

                    //give the child a new parent
                    child.SetParent(deckHolder);

                    //move the child to the parent
                    child.localPosition = new Vector3(-5 * i, 0);

                    CardDisplay cardDisplay = child.GetComponent<CardDisplay>();
                    cardDisplay.SetImage(false);
                    //cardDisplay.thisImage.sprite = isBack ? cardDisplay.backImage : cardDisplay.frontImage;
                    cardDisplay.index = cardIndex;

                    parentChildRelationships.SetCardDisplay(cardIndex, cardDisplay);

                    cardIndex--;
                }
            }
        }
    }

    private void AddScore(int score)
    {
        playerScore += score;
        scoreText.text = "" + playerScore;

        SettingsManager.instance.ShowScore(score, transform);
    }

    private void SetCombo(int combo)
    {
        if (combo==0 && (this.combo > currentGameHighestcombo)) currentGameHighestcombo = this.combo;

        this.combo = combo;
        comboText.text = "" + combo;
    }

    public void DrawCard(CardDisplay cardDisplay)
    {
        AddHistory(cardDisplay.index, null, combo);

        SetCombo(0);
        cardDisplay.transform.SetParent(currentCardHolder);
        cardDisplay.transform.localPosition = Vector3.zero;
        cardDisplay.SetImage(true);
        parentChildRelationships.Deactivate(cardDisplay.index, true);
        //cardDisplay.thisImage.sprite = cardDisplay.frontImage;

        currentCard = cardDisplay;
    }

    /**
     * Stores a relationship of integer - integer array
     * 
     * Every child has either 0 or 2 parents
     * 
     * Every child has either 0 or 1 children
     * 
     * If a child has no children then it is a special number that grants an extra 500*i score
     */
    class ParentChildRelationships
    {
        private List<ParentChildRelationship> parentChildRelationships = new List<ParentChildRelationship>();
        private List<ParentChildRelationship> baseParentChildRelationships = new List<ParentChildRelationship>();

        /**
         * Searches through all relationships for a child.
         * 
         * If no children are found, returns null.
         */
        public ParentChildRelationship Search(int child)
        {
            foreach(ParentChildRelationship parentChildRelationship in parentChildRelationships)
            {
                if (parentChildRelationship.child == child)
                {
                    return parentChildRelationship;
                }
            }

            return null;
        }

        /**
         * Searches through parentChildRelationships
         * 
         * if not found, throws error
         * 
         * else, returns parentChildRelationship.isActive.
         */
        public bool IsActive(int child)
        {
            ParentChildRelationship parentChildRelationship = Search(child);

            if (parentChildRelationship != null)
            {
                return parentChildRelationship.isActive;
            }

            throw new Exception("Child " + child + " was not found.");
        }

        /**
         * Searches for a child, if none found, throws error
         * 
         * if child is active, deactivates and returns true.
         * 
         * else, returns false.
         */
        public bool Deactivate(int child, bool isUsed)
        {
            ParentChildRelationship parentChildRelationship = Search(child);

            //if we can find the child
            if (parentChildRelationship != null)
            {
                //if the child is already deactivated, do nothing
                if (!parentChildRelationship.isActive) return false;

                //deactivate this child
                parentChildRelationship.isActive = false;
                parentChildRelationship.isUsed = isUsed;

                if (isUsed)
                {
                    ChangeDisplayImage(child, true);

                    //activate children
                    if (parentChildRelationship.childsChildren != null)
                    {
                        foreach (int i in parentChildRelationship.childsChildren)
                        {
                            if (!parentChildRelationship.isActive)
                            {
                                Activate(i);
                            }
                        }
                    }
                }
                else
                {
                    ChangeDisplayImage(child, false);
                }

                return true;
            }

            return false;
            //throw new Exception("Child " + child + " was not found.");
        }

        /**
         * Searches for a child, if none found, throws error
         * 
         * if child is not active, activates and returns true.
         * 
         * else, returns false.
         */
        public bool Activate(int child)
        {
            ParentChildRelationship parentChildRelationship = Search(child);

            if (parentChildRelationship != null)
            {
                if (parentChildRelationship.isActive) return false;

                bool parentsAreDeactivated = true;

                if (parentChildRelationship.parents != null)
                {
                    for(int i = 0; i < parentChildRelationship.parents.Length; i++)
                    {
                        if (!Search(parentChildRelationship.parents[i]).isUsed)
                        {
                            parentsAreDeactivated = false;
                            break;
                        }
                    }
                }

                //parents must be deactivated to activate
                if (parentsAreDeactivated)
                {
                    parentChildRelationship.isActive = true;
                    parentChildRelationship.isUsed = false;


                    if (child<28)
                    {
                        parentChildRelationship.thisDisplay.SetImage(true);
                    }

                    //deactivate children
                    if (parentChildRelationship.childsChildren != null)
                    {
                        foreach(int i in parentChildRelationship.childsChildren)
                        {
                            Deactivate(i, false);
                        }
                    } 

                    return true;
                }

                return false;
            }

            return false;
            //throw new Exception("Child " + child + " was not found.");
        }

        /**
         * Adds a new child to the parentChildRelationships if child is valid
         */
        public bool Add(bool isActive, int child, int[] parents, int[] childsChildren)
        {
            if (Search(child) == null)
            {
                parentChildRelationships.Add(new ParentChildRelationship(isActive, child, parents, childsChildren));
                baseParentChildRelationships.Add(new ParentChildRelationship(isActive, child, parents, childsChildren));
                return true;
            }

            return false;
        }

        /**
         * Reactivates all of the children for when the game is reset
         */
        public void Reset()
        {
            parentChildRelationships.Clear();

            foreach(ParentChildRelationship parentChildRelationship in baseParentChildRelationships)
            {
                parentChildRelationships.Add(parentChildRelationship.Clone());
            }
        }

        override public string ToString()
        {
            string toString = "parentChildRelationships{";

            bool once=false;
            foreach(ParentChildRelationship parentChildRelationship in parentChildRelationships)
            {
                if (once)
                {
                    toString += ", ";
                }
                toString += parentChildRelationship.ToString();
                if (!once) once = true;
            }

            toString += "}";

            return toString;
        }

        public CardDisplay GetCardDisplay(int child)
        {
            ParentChildRelationship parentChildRelationship = Search(child);

            if (parentChildRelationship != null)
            {
                return parentChildRelationship.thisDisplay;
            }

            return null;
        }

        public bool SetCardDisplay(int child, CardDisplay cardDisplay)
        {
            ParentChildRelationship parentChildRelationship = Search(child);

            if (parentChildRelationship != null && cardDisplay !=null)
            {
                parentChildRelationship.thisDisplay = cardDisplay;
                return true;
            }

            return false;
        }

        public void ChangeDisplayImage(int child, bool frontOrBack)
        {
            ParentChildRelationship parentChildRelationship = Search(child);

            if (parentChildRelationship != null)
            {
                parentChildRelationship.thisDisplay.SetImage(frontOrBack);
            }
        }

        /**
         * A basic relationship between a parent and a child
         */
        public class ParentChildRelationship
        {
            //is the card currently active
            public bool isActive;

            public bool isUsed=false;

            //this
            public int child;

            //null if no parents
            public int[] parents;

            //-1 if no children
            public int[] childsChildren;

            public CardDisplay thisDisplay;

            public ParentChildRelationship(bool isActive, int child, int[] parents, int[] childsChildren)
            {
                this.isActive = isActive;
                this.child = child;

                if (parents == null)
                {
                    this.parents = null;
                }
                else
                {
                    this.parents = new int[parents.Length];
                    for(int i=0; i< parents.Length;i++)
                    {
                        this.parents[i] = parents[i];
                    }
                }

                if (childsChildren == null)
                {
                    this.childsChildren = null;
                }
                else
                {
                    this.childsChildren = new int[childsChildren.Length];
                    for (int i = 0; i < childsChildren.Length; i++)
                    {
                        this.childsChildren[i] = childsChildren[i];
                    }
                }
            }

            public override string ToString()
            {
                string toString = "ParentChildRelationship{" +
                        "isActive=" + isActive +
                        ", isUsed=" + isUsed+
                        ", child=" + child +
                        ", parents=["+ IntArrayToString(parents) +
                        "], childsChildren=[" + IntArrayToString(childsChildren) + 
                        "]}";

                return toString;

                string IntArrayToString(int[] array)
                {
                    if (array == null) return "null";

                    string returnable = "";

                    bool once = true;
                    foreach(int i in array)
                    {
                        if (!once)
                        {
                            returnable += ", ";
                        }
                        returnable += ""+i;
                        if (once) once = !once;
                    }

                    return returnable;
                }
            }

            public ParentChildRelationship Clone()
            {
                return new ParentChildRelationship(isActive, child, parents, childsChildren);
            }
        }
    }

    #region history
    /**
     * Tracks the game history
     */
    private class History
    {
        public int index;
        public int[] score;
        public int combo;

        public History(int index, int[] score, int combo)
        {
            this.index = index;
            if (score == null)
            {
                this.score = null;
            }
            else
            {
                this.score = new int[score.Length];
                for(int i=0;i<score.Length;i++)
                {
                    this.score[i] = score[i];
                }
            }
            this.combo = combo;
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
     * Undo 1 move
     */
    public bool Undo()
    {
        //If there is history to undo, undo the last history
        if (historiesList.Count > 0)
        {
            History history = historiesList.Pop();
            CardDisplay cardDisplay = parentChildRelationships.GetCardDisplay(history.index);

            Transform parent;
            //set in bottom[index]
            if (history.index < 28)
            {
                parent = bottomRow.Concat(leftPeak).Concat(middlePeak).Concat(rightPeak).ToArray()[history.index];
                parentChildRelationships.Activate(history.index);
            }
            else
            {
                parent = deckHolder;

                parentChildRelationships.ChangeDisplayImage(history.index, false);
                parentChildRelationships.Activate(history.index);
            }
            cardDisplay.transform.SetParent(parent);
            cardDisplay.transform.localPosition = Vector3.zero;

            if (history.score != null)
            {
                if (history.score.Length > 1)
                {
                    peakMultiplier--;
                }

                foreach(int i in history.score)
                {
                    AddScore(-i);
                }
            }

            if (currentGameHighestcombo == combo) currentGameHighestcombo = history.combo;
            SetCombo(history.combo);

            currentCard = currentCardHolder.GetChild(currentCardHolder.childCount - 1).GetComponent<CardDisplay>();

            return true;
        }

        return false;
    }

    /**
     * Adds a single move to the history.
     * 
     * Used for draw and remove king
     */
    private void AddHistory(int index, int[] score, int combo)
    {
        historiesList.Push(new History(index, score, combo));
    }
    #endregion history
}
