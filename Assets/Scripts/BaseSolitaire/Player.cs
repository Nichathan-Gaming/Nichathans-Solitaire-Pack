using System.Collections.Generic;

[System.Serializable]
public class Player
{
    public Deck main, winnings, active, hand;

    /**
     * @details
     * A typical player is a member of the game, they are created with their main deck and all
     *      others are empty.
     *
     * @effects
     * Initialize the Player
     *
     * @param main
     */
    public Player(Deck main)
    {
        this.main = main;
        winnings = new Deck();
        active = new Deck();
        hand = new Deck();
    }

    /**
     * @details
     *  A typical Player can be initialized with all 4 decks set in case the game is being
     *      continued.
     *
     * @param main
     * @param winnings
     * @param active
     * @param hand
     */
    public Player(Deck main, Deck winnings, Deck active, Deck hand)
    {
        this.main = main;
        this.winnings = winnings;
        this.active = active;
        this.hand = hand;
    }

    public Deck getMain()
    {
        return main;
    }

    public Deck getWinnings()
    {
        return winnings;
    }

    public Deck getActive()
    {
        return active;
    }

    public Deck getHand()
    {
        return hand;
    }

    /**
     * @effects
     *  adds 1 card from main to hand
     *  The card removed from main is at position 0, the position added to hand is hand.size
     *
     * @return true if successful
     */
    public bool drawDeckToHand()
    {
        return drawDeck(hand);
    }

    /**
     * @effects
     *  adds all cards from hand to active
     *
     * @return true if successful, false if not
     */
    public bool playHand()
    {
        if (!hand.isEmpty())
        {
            active.addAll(hand.clear());

            return true;
        }

        return false;
    }

    public bool drawDeckToActive()
    {
        return drawDeck(active);
    }

    public bool moveActiveToWinning()
    {
        if (!active.isEmpty())
        {
            winnings.addAll(active.clear());

            return true;
        }

        return false;
    }

    public List<Card> clearActive()
    {
        return active.clear();
    }

    public bool moveWinningsToDeck()
    {
        if (main.isEmpty() && !winnings.isEmpty())
        {
            main.addAll(winnings.clear());

            return true;
        }

        return false;
    }

    public int[] getActiveScores()
    {
        return active.getScores();
    }
    public int[] getHandScores()
    {
        return hand.getScores();
    }

    public int countWinnings()
    {
        return winnings.size();
    }

    public void addToWinnings(params List<Card>[] decks)
    {
        if (decks == null)
        {
            return;
        }

        foreach (List<Card> d in decks)
        {
            if (d != null)
            {
                winnings.addAll(d);
            }
        }
    }

    public void emptyDeckToWinnings()
    {
        addToWinnings(main.clear());
    }

    private bool drawDeck(Deck addToDeck)
    {
        if (!main.isEmpty())
        {
            addToDeck.add(main.draw());
            return true;
        }

        return false;
    }

    public bool hasLost(int gameType)
    {
        if (gameType == 0)
        {
            return eliminationLost();
        }
        else
        {
            return suddenDeathLost();
        }
    }

    /**
     * true if all 4 decks are empty
     *
     * @return
     */
    private bool eliminationLost()
    {
        return main.isEmpty() && winnings.isEmpty() && active.isEmpty() && hand.isEmpty();
    }

    private bool suddenDeathLost()
    {
        return main.isEmpty();
    }

    public string toString()
    {
        return "Player{" +
                "main=" + main.toString() +
                ", winnings=" + winnings.toString() +
                ", active=" + active.toString() +
                ", hand=" + hand.toString() +
                '}';
    }
}
