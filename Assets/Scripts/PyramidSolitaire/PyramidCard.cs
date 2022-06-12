using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PyramidCard : MonoBehaviour
{
    public int suit;
    public int cardNumber;

    public PyramidCard(int suit, int cardNumber)
    {
        this.suit = suit;
        this.cardNumber = cardNumber;
    }

    public int getSuit()
    {
        return suit;
    }

    public PyramidCard setSuit(int suit)
    {
        this.suit = suit;
        return this;
    }

    public int getCardNumber()
    {
        return cardNumber;
    }

    public PyramidCard setCardNumber(int cardNumber)
    {
        this.cardNumber = cardNumber;
        return this;
    }
}
