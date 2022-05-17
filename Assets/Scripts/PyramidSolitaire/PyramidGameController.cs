using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PyramidGameController : MonoBehaviour
{
    #region variables
    private const string PYRAMID_FASTEST_TIME_PREFS = "PyramidFastestTime",
        PYRAMID_HIGHEST_SCORE_PREFS = "PyramidHighestScore";
    private static float CARD_WIDTH, CARD_HEIGHT;
    private Color OUTLINE_COLOR = Color.green;

    #region Transforms for placing objects
    [Header("The 7 row positions")]
    //0 top, 6 bottom
    public Rows[] rowCardPosition;
    private static Rows[] staticRowCardPosition;

    [Header("The Transform that holds the deck")]
    public Transform deckHolder;
    public static Transform staticDeckHolder;

    [Header("The Transform that holds the drawn cards")]
    public Transform drawnCardHolder;
    public static Transform staticDrawnCardHolder;

    [Header("The Transform that hides cards out of the game")]
    public Transform hiddenCardHolder;

    [Header("The Pyramid card prefab")]
    public GameObject cardPrefab;
    #endregion Transforms for placing objects

    #region score tracking section
    [Header("The score text")]
    public Text scoreText;
    private const string SCORE_LABEL = "Score : ";
    //the score of the game
    private int score = 0;
    private static int scoreReduceOnDraw = 1;
    private static int scoreReduceOnDrawRefresh = 100;
    private const int ROW_CLEAR_MULTIPLIER = 250;
    #endregion score tracking section

    #region timer area
    public StopWatch stopWatch;
    public Text timerText;
    private const string TIME_LABEL = "Time : ";
    #endregion timer area

    #region row clearing section
    //add 1 for every row cleared, at 7 - call victory
    private static int rowsCleared = 0;

    //the image of the row that we are animating
    private Image rowAnimationImage;
    private bool showRowAnimation;

    //Do we move the color alpha to 1 or 0
    private bool alphaTo1;

    private const float ALPHA_INCREMENT_DEGREE = 0.05f;
    #endregion row clearing section

    /**
     * the currently selected card
     * 
     * if the nect card clicked cannot be clicked, do nothing
     * 
     * if the next card clicked + this = 13, remove both 
     * 
     * else switch this to the new card
     */
    private Transform selectedCard;

    //the list of our history used for undo
    private Stack<History> historiesList = new Stack<History>();
    #endregion variables

    #region default Unity methods
    // Start is called before the first frame update
    void Start()
    {
        #region initialize variables
        //start the stopwatch
        stopWatch.StartStopWatch(SetTime);

        //load the rows
        staticRowCardPosition = rowCardPosition;

        //get the deck holder
        staticDeckHolder = deckHolder;

        //get the deck holder
        staticDrawnCardHolder = drawnCardHolder;
        #endregion initialize variables

        //get defaults
        RectTransform cardPrefabRectTransform = cardPrefab.GetComponent<RectTransform>();
        if (cardPrefabRectTransform == null)
        {
            throw new Exception("Card prefab RectTransform is null.");
        }
        else
        {
            CARD_WIDTH = cardPrefabRectTransform.rect.width;
            CARD_HEIGHT = cardPrefabRectTransform.rect.height;
        }

        #region set Cards
        //card suit from 0-3 {H, D, S, C}
        for (int i = 0; i < 4; i++)
        {
            //card number from 0-12
            for (int j = 0; j < 13; j++)
            {
                #region instantiate new card
                GameObject newCard = Instantiate(cardPrefab, hiddenCardHolder.transform);

                newCard.name = i + "i_" + j + "j";
                #endregion instantiate new card

                #region get and set card component
                PyramidCard card = newCard.GetComponent<PyramidCard>();
                if (card == null)
                {
                    throw new Exception("PyramidCard for " + newCard.name + " is null.");
                }
                else
                {
                    card.setCardNumber(j).setSuit(i);
                }
                #endregion get and set card component

                #region get and set card image
                Image cardImage = newCard.GetComponent<Image>();
                if (cardImage == null)
                {
                    throw new Exception("Card image for " + newCard.name + " is null.");
                }
                else
                {
                    cardImage.sprite = Resources.Load<Sprite>("Images/SolitaireCards/"+ card.getSuitAsString()+"/"+card.getCardNumberAsChar()+ card.getSuitAsChar());
                }
                #endregion get and set card image
            }
        }
        #endregion set Cards

        SettingsManager.RESET = ResetGame;
        SettingsManager.UNDO = Undo;

        //move the cards into their positions
        AssignCards();
    }

    // Update is called once per frame
    void Update()
    {
        if (SettingsManager.instance.IsSettingsOpen()) return;

        #region check left click
        //left mouse button has been clicked or single phone tap has been made
        if (rowsCleared < 7 && Input.GetMouseButtonDown(0))
        {
            //get mouse position and check the card clicked
            ClickOnCard(Input.mousePosition);
        }
        #endregion check left click

        #region animate row image on row cleared
        //do we have an image to animate
        if (showRowAnimation)
        {
            //do we move the color alpha to 1 or 0
            if (alphaTo1)
            {
                float alpha;

                //see if we should stop animating up and start animaitng down
                if (rowAnimationImage.color.a > 0.95f)
                {
                    alpha = 1;

                    alphaTo1 = false;
                }
                else
                {
                    alpha = rowAnimationImage.color.a + ALPHA_INCREMENT_DEGREE;
                }

                //increment the alpha
                rowAnimationImage.color = new Color(
                    rowAnimationImage.color.r,
                    rowAnimationImage.color.g,
                    rowAnimationImage.color.b,
                    alpha
                );
            }
            else
            {
                //see if we should stop animating up and start animaitng down
                if (rowAnimationImage.color.a < 0.05f)
                {
                    //increment the alpha
                    rowAnimationImage.color = new Color(
                        rowAnimationImage.color.r,
                        rowAnimationImage.color.g,
                        rowAnimationImage.color.b,
                        0
                    );

                    showRowAnimation = false;
                }
                else
                {
                    //increment the alpha
                    rowAnimationImage.color = new Color(
                        rowAnimationImage.color.r,
                        rowAnimationImage.color.g,
                        rowAnimationImage.color.b,
                        rowAnimationImage.color.a - ALPHA_INCREMENT_DEGREE
                    );
                }
            }
        }
        #endregion animate row image on row cleared

        //if (Input.GetMouseButtonDown(2))
        //{
        //    SetVictory();
        //}
    }
    #endregion default Unity methods

    /**
     * Resets the game
     */
    public void ResetGame()
    {
        SetSelected(null);

        #region reset cards
        //move all cards from rows to hiddenCardHolder
        foreach (Rows rows in rowCardPosition)
        {
            foreach(Transform transform in rows.row)
            {
                //make sure none of the transforms have children
                while (transform.childCount > 0)
                {
                    transform.GetChild(0).SetParent(hiddenCardHolder);
                }
            }
        }

        //move all cards from deckHolder to hiddenCardHolder
        while (deckHolder.childCount > 0)
        {
            deckHolder.GetChild(0).SetParent(hiddenCardHolder);
        }

        //move all cards from drawnCardHolder to hiddenCardHolder
        while (drawnCardHolder.childCount > 0)
        {
            drawnCardHolder.GetChild(0).SetParent(hiddenCardHolder);
        }

        //assign the cards
        AssignCards();
        #endregion reset cards

        //reset score, timer and rowsCleared
        score = 0;
        scoreText.text = score+"";
        rowsCleared = 0;

        //reset stopwatch
        stopWatch.RestartStopWatch();

        //clear history
        historiesList.Clear();
    }

    /**
     * Updates the timerText.text
     */
    public void SetTime(float time)
    {
        timerText.text = SettingsManager.instance.FormatTime(time);
    }

    #region Game controls
    /**
     * If there is at least 1 card in deck, 
     * 
     * move the last card to drawn
     * 
     * SetSelected to null
     */
    public void MoveDeckToDrawnClicked()
    {
        //do nothing if there are no cards to move
        if (deckHolder.childCount < 1)
        {
            /**
             * moves all children from drawnDeckHolder to deckHolder
             * 
             * While there are children, takes child(0)
             */
            while (drawnCardHolder.childCount > 0)
            {
                Transform child = drawnCardHolder.GetChild(drawnCardHolder.childCount - 1);
                child.SetParent(deckHolder);
                child.localPosition = Vector3.zero;
            }

            historiesList.Push(new History(this, deckHolder, drawnCardHolder, false));
            AddScore(-scoreReduceOnDrawRefresh);
        }
        else
        {
            //move the card
            Transform movingCard = deckHolder.GetChild(deckHolder.childCount - 1);
            movingCard.SetParent(drawnCardHolder);
            movingCard.localPosition = Vector3.zero;

            historiesList.Push(new History(this, deckHolder, drawnCardHolder, true));
            AddScore(-scoreReduceOnDraw);
        }

        //nullify SetSelected
        SetSelected(null);
    }

    /**
     * Searches all of the cards for the one that was clicked
     * 
     * Debug.Log that card
     */
    private void ClickOnCard(Vector3 clickPosition)
    {
        //create the checker
        CheckForClick checkForClick = new CheckForClick(clickPosition);

        #region Check decks for click
        //First, see if this is deck level
        if (deckHolder.childCount > 0 && checkForClick.Check(deckHolder.position))
        {
            //select the last child
            HandleSelectedCard(deckHolder.GetChild(deckHolder.childCount - 1));
            return;
        }

        //Then, check drawnCardHolder for a click
        if (drawnCardHolder.childCount > 0 && checkForClick.Check(drawnCardHolder.position))
        {
            //select the last child
            HandleSelectedCard(drawnCardHolder.GetChild(drawnCardHolder.childCount - 1));
            return;
        }
        #endregion Check decks for click

        #region Check Rows for click
        //look through the rows from top to bottom
        for(int i = rowCardPosition.Length-1; i > -1; i--)
        {
            //look through the columns of the row
            for(int j = 0; j < rowCardPosition[i].Length();j++)
            {
                Transform selectedTransform = rowCardPosition[i].row[j];
                Vector3 objectPosition = selectedTransform.position;

                //if the column has a child, and that child was touched
                if (selectedTransform.childCount > 0 && checkForClick.Check(objectPosition))
                {
                    //Make sure the child does not have parents guarding it.
                    if (!CheckForCardParents(objectPosition, i))
                    {
                        HandleSelectedCard(selectedTransform.GetChild(0));
                    }

                    //if an item was found here, stop looking whether it's a valid click or not.
                    return;
                }
            }
        }
        #endregion Check Rows for click
    }

    /**
     * removes a single card, adds to score and checks for victory
     */
    private void RemoveCard(Transform card)
    {
        card.SetParent(hiddenCardHolder);
        card.localPosition = Vector3.zero;

        CheckForRowCleared();
    }

    /**
     * Removes a match, used in CheckSelectedCard
     */
    private void RemoveCard(Transform card1, Transform card2)
    {
        RemoveCard(card1);
        RemoveCard(card2);
    }

    /**
     * Adds 1 to score on every call
     * 
     * If row is cleared, add to score
     * 
     * on victory, SetVictory
     */
    private void CheckForRowCleared()
    {
        if (rowsCleared > 6) return;

        //add 1 to score
        AddScore(1);

        //look through the cards at the row position
        foreach (Transform card in rowCardPosition[6 - rowsCleared].row)
        {
            //if we find a card with a child, return, the row is not cleared
            if (card.childCount > 0) return;
        }

        //animate the row that was just cleared
        AnimateOnRowCleared(6 - rowsCleared);

        //row is cleared, add to score
        rowsCleared++;

        //update score
        AddScore(ROW_CLEAR_MULTIPLIER * rowsCleared);

        //check for victory
        if (rowsCleared > 6)
        {
            SetVictory();
        }
    }

    /**
     * opens the ui and updates player prefs for victory
     */
    private void SetVictory()
    {
        //get the time
        float stopWatchTime = stopWatch.timeElapsed;

        //stop the timer
        stopWatch.PauseStopWatch();

        //set the current score and time
        string gameScore = TIME_LABEL + SettingsManager.instance.FormatTime(stopWatchTime) + "\n"
                                    + SCORE_LABEL + score;

        string fastestTimesString = "Fastest Times", highestScoresString = "Highest Scores";
        List<float> fastestTimes = new List<float>();
        List<int> highestScores = new List<int>();

        bool addedTime = false;
        bool addedScore = false;

        float lastTime = 0;
        int lastScore = 0;

        //gather the top 10 
        for (int i = 0; i < 10; i++)
        {
            #region fast time
            //the previous time at this location
            float fastestTime = PlayerPrefs.GetFloat(PYRAMID_FASTEST_TIME_PREFS + i, 0);

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

            #region Highest Scores
            int highestScore = PlayerPrefs.GetInt(PYRAMID_HIGHEST_SCORE_PREFS + i, 0);

            if (!addedScore && (highestScore == 0 || score > highestScore))
            {
                addedScore = true;
                lastScore = highestScore;
                highestScores.Add(score);
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
        if (addedScore || addedTime)
        {
            for (int i = 0; i < 10; i++)
            {
                PlayerPrefs.SetFloat(PYRAMID_FASTEST_TIME_PREFS + i, fastestTimes[i]);
                PlayerPrefs.SetInt(PYRAMID_HIGHEST_SCORE_PREFS + i, highestScores[i]);
            }
        }

        //build the strings to display info
        for (int i = 0; i < 10; i++)
        {
            fastestTimesString += "\n" + SettingsManager.instance.FormatTime(fastestTimes[i]);
            highestScoresString += "\n" + highestScores[i];
        }

        SettingsManager.instance.SetVictory(gameScore, fastestTimesString, highestScoresString);
    }

    /**
     * Adds to the score and instantiates a rising score object
     */
    public void AddScore(int addScore)
    {
        score += addScore;
        scoreText.text = ""+score;

        SettingsManager.instance.ShowScore(addScore, transform);
    }

    /**
     * Animates the row
     */
    private void AnimateOnRowCleared(int row)
    {
        showRowAnimation = true;

        if (rowAnimationImage != null)
        {
            rowAnimationImage.color = new Color(
                rowAnimationImage.color.r,
                rowAnimationImage.color.g,
                rowAnimationImage.color.b,
                0
            );
        }

        rowAnimationImage = rowCardPosition[row].row[0].parent.gameObject.GetComponent<Image>();

        alphaTo1 = true;
    }

    /**
     * Checks to see how we handle a selection
     */
    private void HandleSelectedCard(Transform child)
    {
        //play sound
        SettingsManager.instance.PlayClickSound();

        //see if this card is a king
        PyramidCard currentlyClickedCard = child.GetComponent<PyramidCard>();
        if (currentlyClickedCard == null)
        {
            throw new Exception("The clicked card does not have a Card component.");
        }
        else
        {
            //if the card is king, SetSelected to null and remove this.
            if (currentlyClickedCard.cardNumber == 12)
            {
                //add to history
                AddHistory(child, child.parent);

                SetSelected(null);
                RemoveCard(child);
                return;
            }
        }

        //see if we have something to compare with
        if (selectedCard == null)
        {
            SetSelected(child);
            return;
        }



        #region check if card+card=13
        //we need to check to see if these two cards equal 13
        PyramidCard previouslyClickedCard = selectedCard.GetComponent<PyramidCard>();
        if (previouslyClickedCard == null)
        {
            throw new Exception("Previously clicked card does not have a PyramidCard component.");
        }
        else
        {
            if (currentlyClickedCard.cardNumber + previouslyClickedCard.cardNumber == 11)
            {
                //add to history
                AddHistory(selectedCard, selectedCard.parent, child, child.parent);

                RemoveCard(selectedCard, child);
                SetSelected(null);
                return;
            }
        }
        #endregion check if card+card=13

        #region set selected
        //Select the child
        SetSelected(
            selectedCard.Equals(child)
            ? null
            : child
        );
        #endregion set selected
    }

    public void OpenSettings()
    {
        SettingsManager.instance.ToggleSettings();
    }

    /**
     * Turns off the previous outline, 
     * 
     * sets this as the new selected and 
     * 
     * if not null, turns on this outline.
     */
    private void SetSelected(Transform newSelected)
    {
        if (selectedCard != null)
        {
            selectedCard.GetComponent<Outline>().effectColor = Color.black;
        }

        selectedCard = newSelected;
        if (selectedCard == null) return;

        Outline outline = selectedCard.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = OUTLINE_COLOR;
        }
    }

    /**
     * Tells you if a child has parents guarding it or not
     * 
     * uses the card that you are checking and its current row
     */
    private bool CheckForCardParents(Vector3 card, int row)
    {
        //lowest row, there are no parents to guard this
        if (row > 5) return false;

        //get the bottom right or left
        Vector3 bottomLeft = new Vector3(card.x - (CARD_WIDTH / 2), card.y- (CARD_HEIGHT / 2)),
            bottomRight = new Vector3(card.x + (CARD_WIDTH / 2), card.y - (CARD_HEIGHT / 2));

        //search all of the positions in the preceding row to see if there are parents guarding this card
        bool checkCorner(Vector3 corner)
        {
            //create a new checker
            CheckForClick checkForClick = new CheckForClick(corner);

            //search through the row
            foreach(Transform transform in rowCardPosition[row+1].row)
            {
                //a check was found, so it is true.
                if (transform.childCount > 0 && checkForClick.Check(transform.position)) return true;
            }

            //a check was not found, so it is false.
            return false;
        }

        //can we detect a click on the bottom right or left?
        return checkCorner(bottomLeft) || checkCorner(bottomRight);
    }

    /**
     * Assigns all cards in the hiddenCardHolder to either deck or 7 rows
     * 
     * requires all cards to be in hiddenCardHolder
     */
    private void AssignCards()
    {
        #region assign the children to the rows
        //move cards to the 7 rows
        for (int i = 0; i < 7; i++)
        {
            //get a random card i+1 times from hiddenCardHolder and assign to rowCardPosition[i][j]
            for(int j = 0; j < (i + 1); j++)
            {
                //get a random card from the children of hiddenCardHolder
                int index = UnityEngine.Random.Range(0, hiddenCardHolder.transform.childCount);

                //grab the child
                Transform child = hiddenCardHolder.GetChild(index);

                //Give the child a new parent
                child.SetParent(rowCardPosition[i].row[j]);

                //Move the child to the parent
                child.localPosition = Vector3.zero;
            }
        }
        #endregion assign the children to the rows

        //move the rest of the children to the deckHolder
        while (hiddenCardHolder.childCount > 0)
        {
            //take a child and move it to deckHolder
            Transform child = hiddenCardHolder.GetChild(0);

            //change the child's parent
            child.SetParent(deckHolder);

            //move the child to its new parent.
            child.localPosition = Vector3.zero;
        }
    }

    /**
     * A checking object that is set with a mouse position and then can verify if a card has the mouse position inside.
     */
    private class CheckForClick
    {
        private Vector3 mousePosition;

        public CheckForClick(Vector3 mousePosition)
        {
            this.mousePosition = mousePosition;
        }

        /**
         * Returns true if mouse position is inside of card
         */
        public bool Check(Vector3 card)
        {
            //get card values
            float cardMaxX = card.x + (CARD_WIDTH / 2),
                cardMinX = card.x - (CARD_WIDTH / 2),
                cardMaxY = card.y + (CARD_HEIGHT / 2),
                cardMinY = card.y - (CARD_HEIGHT / 2);

            //if the position is greater than the minimums and less than the maximums
            return mousePosition.x > cardMinX && mousePosition.x < cardMaxX && mousePosition.y > cardMinY && mousePosition.y < cardMaxY;
        }
    }
    #endregion Game controls

    #region history
    /**
     * Tracks the game history
     */
    private class History
    {
        PyramidGameController gameController;
        Transform deckHolder;
        Transform drawnCardHolder;
        private bool isDraw;
        private bool isDrawReset;

        //The transforms that we use to move around the cards
        private Transform firstRemovedCard, firstRemovedCardParent,
            secondRemovedCard, secondRemovedCardParent;

        /**
         * The base constructor for a single card
         */
        public History(PyramidGameController gameController, Transform firstRemovedCard, Transform firstRemovedCardParent)
        {
            this.gameController = gameController;
            this.firstRemovedCard = firstRemovedCard;
            this.firstRemovedCardParent = firstRemovedCardParent;
        }

        /**
         * The base constructor for two cards
         */
        public History(PyramidGameController gameController, Transform firstRemovedCard, Transform firstRemovedCardParent, Transform secondRemovedCard, Transform secondRemovedCardParent)
        {
            this.gameController = gameController;
            this.firstRemovedCard = firstRemovedCard;
            this.firstRemovedCardParent = firstRemovedCardParent;
            this.secondRemovedCard = secondRemovedCard;
            this.secondRemovedCardParent = secondRemovedCardParent;
        }

        /**
         * The constructor used to set history as a draw or draw reset
         */
        public History(PyramidGameController gameController, Transform deckHolder, Transform drawnCardHolder, bool isDrawOrReset)
        {
            this.deckHolder = deckHolder;
            this.drawnCardHolder = drawnCardHolder;
            this.gameController = gameController;
            if (isDrawOrReset)
            {
                isDraw = true;
            }
            else
            {
                isDrawReset = true;
            }
        }

        /**
         * Undoes the current history
         */
        public void Undo()
        {
            if (isDraw)
            {
                Transform child = drawnCardHolder.GetChild(drawnCardHolder.childCount - 1);
                child.SetParent(deckHolder);
                child.localPosition = Vector3.zero;
                gameController.AddScore(scoreReduceOnDraw);
                return;
            }

            if (isDrawReset)
            {
                while (drawnCardHolder.childCount > 0)
                {
                    Transform child = deckHolder.GetChild(deckHolder.childCount - 1);
                    child.SetParent(drawnCardHolder);
                    child.localPosition = Vector3.zero;
                    gameController.AddScore(scoreReduceOnDrawRefresh);
                }
                return;
            }

            if (firstRemovedCard == null || firstRemovedCardParent == null) throw new Exception("The firstRemoved is null.");

            firstRemovedCard.SetParent(firstRemovedCardParent);
            firstRemovedCard.localPosition = Vector3.zero;

            if (secondRemovedCard != null && secondRemovedCardParent != null)
            {
                secondRemovedCard.SetParent(secondRemovedCardParent);
                secondRemovedCard.localPosition = Vector3.zero;
                HandleScoreChanged(true);
            }
            else
            {
                HandleScoreChanged(false);
            }
        }

        /**
         * changes the score and looks to see if the rowsCleared should be undone
         */
        private void HandleScoreChanged(bool undoTwice)
        {
            gameController.AddScore(-1 * (undoTwice ? 2 : 1));

            //verify that the last row is still cleared
            if (rowsCleared > 0)
            {
                //look through the cards at the row position
                foreach (Transform card in staticRowCardPosition[7 - rowsCleared].row)
                {
                    //if we find a card with a child, remove the score and reduce rowsCleared
                    if (card.childCount > 0)
                    {
                        gameController.AddScore(-1 * (ROW_CLEAR_MULTIPLIER * rowsCleared));
                        rowsCleared--;
                        return;
                    }
                }
            }
        }
    }

    /**
     * Undo 1 move
     */
    public bool Undo()
    {
        if (rowsCleared > 6) return false;

        //If there is history to undo, undo the last history
        if (historiesList.Count > 0) historiesList.Pop().Undo();

        return true;
    }

    /**
     * Adds a single move to the history.
     * 
     * Used for draw and remove king
     */
    private void AddHistory(Transform firstRemovedCard, Transform firstRemovedCardParent)
    {
        historiesList.Push(new History(this, firstRemovedCard, firstRemovedCardParent));
    }

    /**
     * Adds a match to history
     */
    private void AddHistory(Transform firstRemovedCard, Transform firstRemovedCardParent, Transform secondRemovedCard, Transform secondRemovedCardParent)
    {
        historiesList.Push(new History(this, firstRemovedCard, firstRemovedCardParent, secondRemovedCard, secondRemovedCardParent));
    }
    #endregion history
}

[Serializable]
public class Rows
{
    public Transform[] row;

    public int Length()
    {
        return row.Length;
    }
}
