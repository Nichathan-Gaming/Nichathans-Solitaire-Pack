using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/**
 * Typical Game:
 * A Typical BlackWar game is played with 2-4 players and is played with a single deck of 52 playing cards (Suits are irrelevant here).
 * 
 * Setup:
 * To begin a game of BlackWar, the deck(s) is(are) evenly split between all players. Any cards that cannot be evenly distributed should be 
 * discarded and not used for the remainder of the game.
 * 
 * A Typical Turn:
 * During a typical turn in BlackWar, all players must draw 2 cards. (A single card may be drawn if playing a No Refresh Game and there is only 1 
 * card left in the players deck.) Once every player draws their cards, they may look at the cards of every other player. Then, each player may 
 * decide to continue drawing cards until they decide to stop or go over a hand count of 21.
 * 
 * Deciding the winner of a turn:
 * Once every player has decided to stop drawing cards, whoever has a hand count under 22 and closest to 21 is the winner. At this point, if all
 * players are over 21 cards then the winner will be whoever is closest to 21. If there are multiple winners then a single card is drawn and the 
 * winner is whoever is closest to 21. This last step can be repeated until a winner is found. In the unlikely chance that the players are playing
 * a No Refresh Game and this causes them to tie but run out of cards, then every card not in the winnings or play decks are discarded, the game is
 * stopped and the decks are counted to determine a winner.
 * 
 * The game has two play modes, one is very quick and the other becomes a battle of attrition that may take from half an hour to several days.
 * 
 * No Refresh Game:
 * The quick play mode is also known as a No Refresh Game in which the game is played until at least 1 player has used up their entire deck of 
 * cards. At this point, all players must merge their winnings deck, play deck, and hand. Then these cards are counted and whoever has the most is 
 * the winner. It is possible to have multiple winners or no winners. There are multiple winners if multiple players have the highest amount of 
 * cards and there is at least one winner. If all players have the same number of cards then there are no winners. In the case of a tie, a single 
 * tie breaking hand may be dealt to the tied winners and the winner of that hand is pronounced as the winner.
 * 
 * Attrition Game:
 * The extended play mode is also known as an Attrition Game in which the game is played until all of the cards are owned by a single player.
 * Once a players play deck runs out of cards, they flip over their winnings deck and it becomes their new play deck. This can be done during
 * the players current turn or at the end of the turn. Players who run out of cards are eliminated and may not play again until a player owns every 
 * card or every other player has forfeit.
 * 
 * Single Person Game:
 * The game is playable with a single person but then it is indistinguishable from single person BlackJack without the dealer. 
 * The goal is to get as close to 21 as possible before giving up and seeing how far you can push yourself.
 * 
 * Large Party Game:
 * BlackWar can become a bit hectic with large groups of players but that can be part of the fun. The rules for the game do not change but it is
 * recommended that a new deck is added for every 4 players (For the mathematical inclined, it looks more like [Number of players mod 4])
 */
public class BlackWarController : MonoBehaviour
{
    [Header("display player info here")]
    [SerializeField] Text[] playerInfoText;

    [Header("The total number of players")]
    [SerializeField] int numberOfPlayers=4;

    [Header("Turn these off or on for the players")]
    [SerializeField] GameObject[] playerDisplayArea;

    [Header("Place Cards Here")]
    [SerializeField] Transform[] playerHandArea;
    [SerializeField] Transform[] playerDeckArea;
    [SerializeField] Transform[] playerWinningsArea;

    [Header("The BlackWar card here")]
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Transform instantiationLocation;

    [Header("GameControlButtons")]
    [SerializeField] GameObject startButton;
    [SerializeField] GameObject turnControls;
    [SerializeField] Text holdOrEndTurnText;

    [Header("0: No Refresh Game, 1: Attrition Game")]
    [SerializeField] int gameType;
    [SerializeField] bool hasAIMoved;

    [Header("The current turn")]
    [SerializeField] int turn = 0;

    [Header("Our stopwatch object used to track the time")]
    [SerializeField] StopWatch stopWatch;

    [Header("The text to show players their scores")]
    [SerializeField] Text timerText;
    [SerializeField] Text turnText;

    // Start is called before the first frame update
    void Start()
    {
        int cardCount = (int)(52 * Mathf.Ceil(numberOfPlayers / 4f));//get from Anh's laptop

        CreateCards(cardCount);
        UpdatePlayerDisplayArea();
        startButton.SetActive(true);

        SettingsManager.RESET = ResetGame;

        stopWatch.StartStopWatch(SetTime);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
     * Generates 52*(playerCount/4) cards
     * I.E. 52 for every 4 players
     * 
     * then shuffles them and returns them as a Queue
     */
    void CreateCards(int cardCount)
    {
        int suit=0;
        for (int i = 0; i < cardCount; i++)
        {
            int number = i % 13;

            GameObject newCard = Instantiate(cardPrefab, instantiationLocation);

            newCard.name = suit + "i_" + number + "j";

            newCard.GetComponent<BlackWarCard>().SetCard(suit, number);

            if (number == 12)
            {
                suit++;
                if (suit > 3)
                {
                    suit = 0;
                }
            }
        }

        AssignCards();
    }

    /**
     * Deletes extra cards
     * 
     * Moves cards from instantiation location
     */
    void AssignCards()
    {
        int playerToAssignTo = 0;
        while (instantiationLocation.childCount > 0)
        {
            instantiationLocation
                .GetChild(Random.Range(0, instantiationLocation.childCount))
                .SetParent(playerDeckArea[playerToAssignTo]);

            playerToAssignTo++;
            if (playerToAssignTo > numberOfPlayers - 1) playerToAssignTo = 0;
        }
    }

    /**
     * For each player in number of players, count the number of cards in winningsArea and deckArea
     * 
     * tally the card score 
     * 
     * if a score is over 21 and others are under 21, do not show the number over 21
     */
    void UpdatePlayerDisplayArea()
    {
        for(int i = 0; i < numberOfPlayers; i++)
        {
            int deckCount = playerDeckArea[i].childCount,
                winningsCount = playerWinningsArea[i].childCount;

            int[] handCount = CalculateHand(i);

            string infoText = "Deck: "+deckCount+
                " Winnings: "+winningsCount+" \nHandScore: ";

            bool shownOnce = false;
            foreach(int count in handCount)
            {
                if (shownOnce) infoText += "/";
                infoText += count;
                shownOnce = true;
            }

            if (handCount.Length < 1) infoText += "0";

            playerInfoText[i].text = infoText;
        }
    }

    /**
     * Counts all of the cards in the players hand.
     */
    int[] CalculateHand(int playersHandIndex)
    {
        List<int> handNumbers = new List<int>();
        handNumbers.Add(0);

        foreach (BlackWarCard card in playerHandArea[playersHandIndex].GetComponentsInChildren<BlackWarCard>())
        {
            int value = card.GetCardValue();

            if (value>1)
            {
                for(int i = 0; i < handNumbers.Count; i++)
                {
                    handNumbers[i] += value;
                }
            }
            else
            {
                List<int> tempList = new List<int>();
                for(int i=0;i<handNumbers.Count;i++)
                {
                    tempList.Add(handNumbers[i]+1);
                    handNumbers[i] += 11;
                }

                handNumbers.AddRange(tempList);
            }
        }

        int[] tempHand = handNumbers.ToArray();
        for (int i = 0; i < tempHand.Length; i++)
        {
            for(int j = i+1; j < tempHand.Length; j++)
            {
                if (tempHand[i] == tempHand[j])
                {
                    handNumbers.Remove(tempHand[i]);
                    break;
                }
            }
        }

        return handNumbers.ToArray();
    }

    /**
     * if playerDeckArea has cards,
     * Moves a card from playerDeckArea to playerHandArea
     * 
     * then, UpdatePlayerDisplayArea
     */
    public void DrawCard(int player)
    {
        DrawCardWithoutUpdate(player);

        UpdatePlayerDisplayArea();

        hasAIMoved = false;
        holdOrEndTurnText.text = "Hold";
    }

    /**
     * if playerDeckArea has cards,
     * Moves a card from playerDeckArea to playerHandArea
     */
    public void DrawCardWithoutUpdate(int player)
    {
        if (playerDeckArea[player].childCount > 0)
        {
            playerDeckArea[player].GetChild(0).SetParent(playerHandArea[player]);
        }
    }

    /**
     * Each Player draws 2 cards
     */
    public void StartTurn()
    {
        turn++;
        turnText.text = "" + turn;
        foreach (Transform t in playerHandArea)
        {
            if (t.childCount > 0) return;
        }

        for(int i = 0; i < numberOfPlayers; i++)
        {
            DrawCardWithoutUpdate(i);
            DrawCardWithoutUpdate(i);
        }
        UpdatePlayerDisplayArea();

        startButton.SetActive(false);
        turnControls.SetActive(true);
    }
   
    /**
     * stops players turn and allows computers to move
     * 
     * computers draw or hold based on the process
     */
    public void Hold()
    {
        if (hasAIMoved)
        {
            turnControls.SetActive(false);
            CheckForTurnWinner();
        }
        else
        {
            holdOrEndTurnText.text = "End Turn";
            ProcessAITurns(1);
            if(GetHighestNumberUnder22(CalculateHand(0)) > 21)
            {
                turnControls.SetActive(false);
                CheckForTurnWinner();
            }
        }
        hasAIMoved = !hasAIMoved;
    }

    void CheckForTurnWinner()
    {
        //get the number closest to 21 for all players and build an array
        int[] highestNumber = new int[] 
        {
            GetHighestNumberUnder22(CalculateHand(0)),
            GetHighestNumberUnder22(CalculateHand(1)),
            GetHighestNumberUnder22(CalculateHand(2)),
            GetHighestNumberUnder22(CalculateHand(3))
        };

        List<int> winners = new List<int>();
        winners.Add(0);

        //find the highest number here
        for (int i = 1; i < numberOfPlayers; i++)
        {
            //if the two numbers are the same, we may have duplicate winners
            if (highestNumber[winners[0]] == highestNumber[i])
            {
                winners.Add(i);
                continue;
            }

            //highestNumber is a loser, see if it's less of a loser than current
            if (highestNumber[winners[0]] > 21)
            {
                //highestNumber[i] is closer to 21 than winning player, we have a new winner
                if (highestNumber[i] < highestNumber[winners[0]])
                {
                    winners.Clear();
                    winners.Add(i);
                }
            }
            else
            {
                //see if this is under 22 and higher than winner
                if (highestNumber[i] < 22 && highestNumber[i] > highestNumber[winners[0]])
                {
                    winners.Clear();
                    winners.Add(i);
                }
            }
        }

        if (winners.Count>1)
        {
            //force all winners to draw 1 until a winner is found
            foreach(int winner in winners)
            {
                DrawCardWithoutUpdate(winner);

                UpdatePlayerDisplayArea();
            }

            CheckForTurnWinner();
            return;
        }
        else
        {
            //move all cards to winners winningsArea
            foreach(Transform t in playerHandArea)
            {
                while (t.childCount > 0)
                {
                    t.GetChild(0).SetParent(playerWinningsArea[winners[0]]);
                }
            }

            UpdatePlayerDisplayArea();

            if (IsSectionOver())
            {
                //player lost
                if (HasPlayerLost(0))
                {
                    SettingsManager.instance.SetVerification("Game Lost.\nContinue?", "Continue", "Quit", ResetGame, SettingsManager.instance.VerifyQuitGame);
                }
                //player hasn't completely lost yet
                else
                {
                    //0: No Refresh Game, 1: Attrition Game
                    if (gameType == 0)
                    {
                        //find winning player
                        int winner = NoRefreshWinningPlayer()+1;

                        SettingsManager.instance.SetVerification("Player "+winner+" has won.\nContinue?", "Continue", "Quit", ResetGame, SettingsManager.instance.VerifyQuitGame);
                    }
                    else
                    {
                        //see if there is an absolute winner
                        int winner = AttritionWinner();
                        if (winner > -1)
                        {
                            SettingsManager.instance.SetVerification("Player " + winner + " has won.\nContinue?", "Continue", "Quit", ResetGame, SettingsManager.instance.VerifyQuitGame);
                        }
                        //else, reset winnings
                        else
                        {
                            ResetWinnings();
                        }
                    }
                }
            }
            else
            {
                startButton.SetActive(true);
                SettingsManager.instance.SetNotification("Player " + (winners[0] + 1) + " has won the hand.");
            }
        }

        /**
         * if a player has no deck then section is over
         */
        bool IsSectionOver()
        {
            foreach (Transform t in playerDeckArea)
            {
                if (t.childCount < 1)
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * Tells you which player has the highest winnings
         * 
         * will go with lowest player in case of a tie
         */
        int NoRefreshWinningPlayer()
        {
            int winner = 0;

            for(int i = 1; i < numberOfPlayers; i++)
            {
                if((playerWinningsArea[i].childCount + playerDeckArea[i].childCount) > (playerWinningsArea[winner].childCount + playerDeckArea[winner].childCount))
                {
                    winner = i;
                }
            }

            return winner;
        }

        /**
         * if the player has no more cards then true
         */
        bool HasPlayerLost(int i)
        {
            return playerDeckArea[i].childCount < 1 && playerHandArea[i].childCount < 1 && playerWinningsArea[i].childCount < 1;
        }

        int AttritionWinner()
        {
            int winner = -1;

            for(int i = 0; i < numberOfPlayers; i++)
            {
                if (!HasPlayerLost(i))
                {
                    if (winner > -1) return -1;
                    winner = i;
                }
            }

            return winner;
        }

        void ResetWinnings()
        {
            for(int i = 0; i < numberOfPlayers; i++)
            {
                while (playerWinningsArea[i].childCount > 0)
                {
                    playerWinningsArea[i].GetChild(0).SetParent(playerDeckArea[i]);
                }
            }
        }
    }

    /**
     * Opens the settings
     */
    public void OpenSettings()
    {
        SettingsManager.instance.ToggleSettings();
    }

    /**
     * returns either the highest number under 22 or the lowest number over 21
     */
    int GetHighestNumberUnder22(int[] handScore)
    {
        int closestNumberOver21 = 9999;
        int closestNumberUnder21 = 0;

        foreach (int i in handScore)
        {
            //stop if 21 found
            if (i == 21) return 21;

            //under 22, see if over closestNumberUnder21
            if (i < 22)
            {
                if (i > closestNumberUnder21) closestNumberUnder21 = i;
            }
            //over 21, see if under closestNumberOver21
            else
            {
                if (i < closestNumberOver21) closestNumberOver21 = i;
            }
        }

        if (closestNumberUnder21 > 0) return closestNumberUnder21;
        return closestNumberOver21;
    }

    /**
     * Determines whether or not the AI should draw a card or not
     */
    bool ShouldAIMOve(int highestHandTotalUnder21, int playerHighestHandTotalUnder21)
    {
        if (highestHandTotalUnder21 > 16) return false;

        if (highestHandTotalUnder21 < 12) return true;

        if (playerHighestHandTotalUnder21 > 15) return false;

        return highestHandTotalUnder21 < playerHighestHandTotalUnder21;
    }

    /**
     * AI - 17 or higher, hold
     * 11 or lower, draw
     * 
     * 12-16 : 
     *      if user has 16 or higher, hold
     *      else, if more than user, hold
     *              else draw
     */
    private void ProcessAITurns(int turnStart)
    {
        int playerHighestHand = GetHighestNumberUnder22(CalculateHand(0));
        for (int i = turnStart; i < numberOfPlayers; i++)
        {
            int highestHand = GetHighestNumberUnder22(CalculateHand(i));
            if (ShouldAIMOve(highestHand, playerHighestHand))
            {
                DrawCardWithoutUpdate(i);

                //recursively draw until done
                ProcessAITurns(i);
                return;
            }
        }

        UpdatePlayerDisplayArea();

        //Turn on player turn area
        turnControls.SetActive(true);
    }
}
