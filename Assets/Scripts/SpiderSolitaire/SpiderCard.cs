using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpiderCard : MonoBehaviour
{
    public static Vector3 CARD_PLACEMENT_DIFFERENCE = new Vector3(0, -30);

    [Header("The card suit and number. 0:S, 1:H, 2:C, 3:D")]
    [SerializeField] int suit; //0-3
    [SerializeField] int number; //0-12

    [Header("Displaying Side Variables")]
    [SerializeField] Image currentImage;
    [SerializeField] bool isCardFaceUp;

    [Header("Moving variables")]
    [SerializeField] bool isMoving;

    [SerializeField] Transform parentDuringMove;
    [SerializeField] Transform parentBeforeMove;
    [SerializeField] Vector3 localpositionBeforeMove;

    [SerializeField] float delay;

    // Update is called once per frame
    void Update()
    {
        if(delay > 0)
        {
            delay--;
            if (Input.GetMouseButtonUp(0))
            {
                isMoving = false;
            }
        }
        else if (
            isMoving &&
            !SettingsManager.instance.IsSettingsOpen() &&
            SettingsManager.instance.IsGameActive()
        )
        {
            transform.position = Input.mousePosition;

            if (Input.GetMouseButtonUp(0))
            {
                isMoving = false;

                if (!SpiderController.instance.TryToPlace(this))
                {
                    ReturnToPreviousPosition();
                }
            }
        }
    }

    public void ReturnToPreviousPosition()
    {
        //return to previous position
        transform.SetParent(parentBeforeMove);
        transform.localPosition = localpositionBeforeMove;
    }

    public void TryToMove()
    {
        if (CanCardMove())
        {
            delay = 15;
            isMoving = true;

            parentBeforeMove = transform.parent;
            localpositionBeforeMove = transform.localPosition;
            transform.SetParent(parentDuringMove);
        }
    }

    /**
     * If all of the children of this card are sequential, then return true
     * 
     * else, return false
     */
    public bool CanCardMove()
    {
        if (transform.childCount > 1) throw new System.Exception(this.name + " has too many children. Child count: " + transform.childCount);

        if (transform.childCount > 0)
        {
            //look for card in children
            SpiderCard spiderCardChild = transform.GetChild(0).GetComponent<SpiderCard>();

            //check recursively
            return IsNextCard(spiderCardChild)?spiderCardChild.CanCardMove():false;
        }
        else
        {
            //return true
            return true;
        }
    }

    /**
     * Places this card on parent at position
     */
    public void PlaceCard(Transform parent, Vector3 localPosition, bool checkRun, bool undoTwice)
    {
        if (parent == null) throw new System.Exception("PlaceCard: " + this.name + " parent cannot be null.");

        if (parent.childCount > 0) return;

        Transform historyChild = transform, historyParent = transform.parent;
        Vector3 historyPosition = transform.localPosition;
        bool isFlipParent = false;

        if (parentBeforeMove != null)
        {
            historyPosition = localpositionBeforeMove;
            historyParent = parentBeforeMove;
            SpiderCard currentParentCard = parentBeforeMove.GetComponent<SpiderCard>();
            if (currentParentCard != null && !currentParentCard.isCardFaceUp)
            {
                currentParentCard.SetIsCardFaceUp(true);
                isFlipParent = true;
            }
        }

        isMoving = false;

        transform.SetParent(parent);
        transform.localPosition = localPosition;

        SpiderController.instance.AddHistory(historyChild, historyParent, historyPosition, isFlipParent, undoTwice);

        if (checkRun)
        {
            SpiderController.instance.CheckForRunKToA(transform);
        }
    }

    /**
     * If spiderCard is this cards next card, return true.
     * 
     * else, return false.
     */
    public bool IsNextCard(SpiderCard spiderCard)
    {
        if (number == 0 || suit != spiderCard.GetSuit()) return false;

        return (number - 1) == spiderCard.GetNumber();
    }

    /**
     * return this.suit
     */
    public int GetSuit()
    {
        return suit;
    }

    /**
     * returns this.number
     */
    public int GetNumber()
    {
        return number;
    }

    /**
     * sets this card's values if valid
     * 
     * else, throws error
     */
    public void SetCard(int suit, int number, bool isCardFaceUp)
    {
        if (!VerifySuit(suit)) throw new System.Exception("Suit: " + suit + " is invalid. Must be from 0-3.");
        if (!VerifyNumber(number)) throw new System.Exception("Number: " + number + " is invalid. Must be from 0-3.");

        this.suit = suit;
        this.number = number;

        SetIsCardFaceUp(isCardFaceUp);
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

    public void SetParentBeforeMove(Transform parentBeforeMove)
    {
        this.parentBeforeMove = parentBeforeMove;
        localpositionBeforeMove = transform.localPosition;
    }

    public void SetIsCardFaceUp(bool isCardFaceUp)
    {
        this.isCardFaceUp = isCardFaceUp;

        currentImage.sprite = isCardFaceUp ? SettingsManager.instance.GetCardFront(suit, number) : SettingsManager.instance.GetCardBack();
    }

    public bool GetIsCardFaceUp()
    {
        return isCardFaceUp;
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
