using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlackWarCard : MonoBehaviour
{
    [Header("The card suit and number. 0:S, 1:H, 2:C, 3:D")]
    [SerializeField] int suit; //0-3
    [SerializeField] int number; //0-12

    [Header("Displaying Side Variables")]
    [SerializeField] Image currentImage;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /**
     * sets this card's values if valid
     * 
     * else, throws error
     */
    public void SetCard(int suit, int number)
    {
        if (!VerifySuit(suit)) throw new System.Exception("Suit: " + suit + " is invalid. Must be from 0-3.");
        if (!VerifyNumber(number)) throw new System.Exception("Number: " + number + " is invalid. Must be from 0-3.");

        this.suit = suit;
        this.number = number;

        currentImage.sprite = SettingsManager.instance.GetCardFront(suit, number);
    }

    public int GetCardValue()
    {
        switch (number)
        {
            case 0:
                return 1;
            case 1:
                return 2;
            case 2:
                return 3;
            case 3:
                return 4;
            case 4:
                return 5;
            case 5:
                return 6;
            case 6:
                return 7;
            case 7:
                return 8;
            case 8:
                return 9;
            case 9:
            case 10:
            case 11:
            case 12:
                return 10;
        }

        throw new System.Exception("Number "+number+" is invalid.");
    }

    public char GetSuitChar()
    {
        switch (suit)
        {
            case 0:
                return 'C';
            case 1:
                return 'D';
            case 2:
                return 'H';
            case 3:
                return 'S';
        }

        throw new System.Exception("Suit: (" + suit + ") is invalid.");
    }

    public string GetSuitWord()
    {
        switch (suit)
        {
            case 0:
                return "Clubs";
            case 1:
                return "Diamonds";
            case 2:
                return "Hearts";
            case 3:
                return "Spades";
        }

        throw new System.Exception("Suit: (" + suit + ") is invalid.");
    }

    public char GetNumberAsChar()
    {
        switch (number)
        {
            case 0:
                return 'A';
            case 1:
                return '2';
            case 2:
                return '3';
            case 3:
                return '4';
            case 4:
                return '5';
            case 5:
                return '6';
            case 6:
                return '7';
            case 7:
                return '8';
            case 8:
                return '9';
            case 9:
                return 'X';
            case 10:
                return 'J';
            case 11:
                return 'Q';
            case 12:
                return 'K';
        }

        throw new System.Exception("Number: (" + number + ") is invalid.");
    }

    /**
     * return true if suit is 0, 1, 2, or 3
     * 
     * else false
     */
    private bool VerifySuit(int suit)
    {
        return suit > -1 && suit < 4;
    }

    /**
     * returns true if number is from 0-12 (inclusive)
     * 
     * else false
     */
    private bool VerifyNumber(int number)
    {
        return number > -1 && number < 13;
    }
}
