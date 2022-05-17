using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Deck
{
    public List<Card> cards;

    public Deck()
    {
        cards = new List<Card>();
    }

    public Deck(bool populateDeck):this()
    {
        if (populateDeck)
        {
            populate();
        }
    }

    public Deck(Card[] cards):this()
    {
        this.cards.AddRange(cards);
    }

    public Deck(List<Card> cards)
    {
        this.cards = cards;
    }

    public Deck populate()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 13; j++)
            {
                cards.Add(new Card(i, j));
            }
        }

        return this;
    }

    public Deck populate(int populationCount)
    {
        for (int i = 0; i < populationCount; i++)
        {
            populate();
        }

        return this;
    }

    /**
     * Splits the deck in half, clearing this deck.
     */
    public Deck[] split()
    {
        Deck splitA = new Deck();
        Deck splitB = new Deck();

        while (cards.Count > 0)
        {
            splitA.add(drawRandom());
            splitB.add(drawRandom());
        }

        return new Deck[] { splitA, splitB };
    }

    /**
     * splits this into splitCount other decks
     *
     * @param splitCount
     * @return
     */
    public Deck[] split(int splitCount)
    {
        if (splitCount < 1)
        {
            return null;
        }

        List<Deck> splitDecks = new List<Deck>();
        for (int i = 0; i < splitCount; i++)
        {
            splitDecks.Add(new Deck());
        }

        //remove excess cards
        for (int i = 0; i < size() % splitCount; i++)
        {
            drawRandom();
            //            draw();
        }

        while (size() > 0)
        {
            foreach (Deck d in splitDecks)
            {
                if (size() < 1)
                {
                    throw new System.ArithmeticException("Error adding card to decks. Not enough cards.");
                }
                d.add(drawRandom());
            }
        }

        return splitDecks.ToArray();
    }

    public void shuffle()
    {
        List<Card> newCards = new List<Card>();

        while (cards.Count > 0)
        {
            newCards.Add(drawRandom());
        }

        cards = newCards;
    }

    public Card draw()
    {
        if (cards.Count > 0)
        {
            Card c = cards[0];
            cards.RemoveAt(0);
            return c;
        }

        return null;
    }

    public Card drawRandom()
    {
        if (cards.Count > 0)
        {
            //int index = (int)(Math.random() * (cards.size()));
            int index = Random.Range(0, cards.Count);
            Card c = cards[index];
            cards.RemoveAt(index);
            return c;
        }

        return null;
    }

    /**
     * @effects clears this deck
     * 
     * @return this deck
     */
    public List<Card> clear()
    {
        List<Card> sendDeck = new List<Card>(cards);

        cards.Clear();

        return sendDeck;
    }

    public List<Card> getCards()
    {
        return cards;
    }

    public void add(Card card)
    {
        cards.Add(card);
    }

    public void addAll(List<Card> cards)
    {
        this.cards.AddRange(cards);
    }

    public void addAll(Card[] cards)
    {
        this.cards.AddRange(cards);
    }

    public int size()
    {
        return cards == null ? 0 : cards.Count;
    }

    public bool isEmpty()
    {
        return size() < 1;
    }

    public string toString()
    {
        string s= "Deck{" +
                "cards=";

        foreach(Card c in cards){
            s += c.toString() + ", ";
        }

        s += "}";

        return s;
    }

    public string toTestString()
    {
        string s="";

        foreach (Card c in cards)
        {
            s += c.toString() + ", ";
        }

        return s;
    }

    public int[] getScores()
    {
        int[] scores = new int[] { 0 };

        foreach (Card c in cards)
        {
            int value = c.getCardValue();

            //if the value is an Ace, then double every score
            if (value == 1)
            {
                int[] newScores = new int[scores.Length * 2];

                int addedScore = 1;
                int position = 0;
                for (int i = 0; i < newScores.Length; i++)
                {
                    newScores[i] = scores[position] + addedScore;
                    position++;
                    if (position >= scores.Length)
                    {
                        addedScore = 11;
                        position = 0;
                    }
                }

                System.Array.Copy(newScores, scores, newScores.Length);
            }
            else
            {
                for (int i = 0; i < scores.Length; i++)
                {
                    scores[i] += value;
                }
            }
        }

        return scores;
    }

    public void removeLast()
    {
        cards.RemoveAt(cards.Count - 1);
    }
}
