using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//look for the quiz tutorial code to make a creatable object
public class FreeCellCard : MonoBehaviour
{
    [SerializeField] int suit;
    [SerializeField] int number;

    [SerializeField] Image cardImage;

    [SerializeField] bool isClicked;

    [SerializeField] Transform playArea;
    [SerializeField] Transform parentOnMove;
    [SerializeField] Vector3 transformOnMove;

    private void Update()
    {
        if (SettingsManager.instance.IsGameActive() && isClicked && !SettingsManager.instance.IsSettingsOpen())
        {
            transform.position = Input.mousePosition;

            if (Input.GetMouseButtonUp(0) && FreeCellManager.isMoving)
            {
                isClicked = false;
                FreeCellManager.isMoving = false;

                if (!FreeCellManager.instance.TryToPlace(this))
                {
                    transform.SetParent(parentOnMove);
                    transform.localPosition = transformOnMove;
                }
            }
        }
    }

    /**
     * uses position as opposed to localPosition
     */
    public void MoveThis(Vector3 moveToTransform, bool isLocal)
    {
        if (isLocal)
        {
            transform.localPosition = moveToTransform;
        }
        else
        {
            transform.position = moveToTransform;
        }
    }

    public void CardClicked()
    {
        //see if we can move this card
        if (!CanCardMove() || isClicked || FreeCellManager.isMoving)
        {
            FreeCellManager.isMoving = false;
            return;
        }

        FreeCellManager.isMoving = true;

        parentOnMove = transform.parent;
        transformOnMove = transform.localPosition;

        transform.SetParent(playArea);

        isClicked = true;
    }

    public void SetIsClicked(bool isClicked)
    {
        this.isClicked = isClicked;
    }

    /**
     * If all cards are alternating colors and sequential numbers from K to A then return true
     * 
     * else false
     */
    private bool CanCardMove()
    {
        if (transform.childCount > 1) throw new System.Exception("Card: " + this.name + " has too many children. ChildCount: " + transform.childCount);

        if (transform.childCount < 1) return true;

        FreeCellCard childCard = transform.GetChild(0).GetComponent<FreeCellCard>();

        if (childCard == null) throw new System.Exception("First child of " + this.name + " is not a FreeCellCard. name: "+transform.GetChild(0).name);

        return IsNextCardKtoA(childCard)?childCard.CanCardMove():false;
    }

    public Transform GetParentOnMove()
    {
        return parentOnMove;
    }

    public Vector3 GetTransformOnMove()
    {
        return transformOnMove;
    }

    public void SetParentOnMove(Transform parentOnMove)
    {
        this.parentOnMove = parentOnMove;
    }

    public void SetTransformOnMove(Vector3 transformOnMove)
    {
        this.transformOnMove = transformOnMove;
    }

    public void SetCard(int suitNumb, int number, Transform playArea)
    {
        if (!VerifySuit(suitNumb)) throw new System.Exception("Type: " + suitNumb + " is invalid.");
        if (!VerifyNumber(number)) throw new System.Exception("Number: " + number + " is invalid.");
        if (playArea == null) throw new System.Exception("PlayArea cannot be null.");

        suit = suitNumb;
        this.number = number;
        this.playArea = playArea;

        cardImage.sprite = SettingsManager.instance.GetCardFront(suitNumb, number);
    }

    public bool IsNextCardKtoA(FreeCellCard placingCard)
    {
        if (number == 0 || placingCard == null) return false;

        return IsOppositeSuit(placingCard.GetSuit()) && ((number - 1) == placingCard.GetNumber());
    }

    public bool IsNextCardAtoK(FreeCellCard placingCard)
    {
        if (number == 12 || placingCard == null) return false;

        return placingCard.GetSuit().Equals(suit) && ((number + 1) == placingCard.GetNumber());
    }

    public int GetSuit()
    {
        return suit;
    }

    public int GetNumber()
    {
        return number;
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

    private bool IsOppositeSuit(int suit)
    {
        switch (this.suit)
        {
            case 0:
            case 1:
                return suit == 2 || suit == 3;
            case 2:
            case 3:
                return suit == 0 || suit == 1;
            default:
                return false;
        }
    }

    private bool VerifySuit(int suit)
    {
        return suit > -1 && suit < 4;
    }

    private bool VerifyNumber(int number)
    {
        return number < 13 && number > -1;
    }

    override public string ToString()
    {
        return "s:" + suit + " n:" + number;
    }

    public bool Equals(Object o)
    {
        if (o == null) return false;

        FreeCellCard card = (FreeCellCard) o;
        if (card == null) return false;

        return (card.suit==suit) && (card.number==number);
    }
}
