using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

public enum SUIT{Spades='S', Clubs='C', Hearts='H', Diamonds='D' };
public enum NUMBER { ACE, TWO, THREE, FOUR, FIVE, SIX, SEVEN, EIGHT, NINE, TEN, JACK, QUEEN, KING};

public class FlippableCard : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public KlondikeSolitaire klondikeSolitaire;

    public SUIT suit=SUIT.Spades;
    public NUMBER number=NUMBER.ACE;

    public bool isInAce=false;

    public bool isDraw = true;

    public bool isBack=true;
    Sprite backImage, frontImage;
    Image image;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    public Vector2 returnPos;
    public bool returnToPrevPos;

    public Transform baseLocations, tempParent;

    private CardMover cardMover;

    private void Start()
    {
        baseLocations = GameObject.Find("BaseLocations").transform;

        image = GetComponent<Image>();
        backImage = Resources.Load<Sprite>("Images/SolitaireCards/CardBack");
        frontImage = Resources.Load<Sprite>("Images/SolitaireCards/" + suit + "/" + GetNumberAsChar() + (char)suit);

        image.sprite = isBack ? backImage : frontImage;

        klondikeSolitaire = FindObjectOfType<KlondikeSolitaire>();

        canvas = GameObject.Find("GameViewMainCanvas").GetComponent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        CardMover cm = gameObject.GetComponent<CardMover>();
        if (cm == null)
        {
            cardMover = gameObject.AddComponent<CardMover>();
        }
        else
        {
            cardMover = cm;
        }
    }

    public void MoveTo(Vector3 vector3)
    {
        if (cardMover == null)
        {
            CardMover cm = gameObject.GetComponent<CardMover>();
            if (cm == null)
            {
                cardMover = gameObject.AddComponent<CardMover>();
            }
            else
            {
                cardMover = cm;
            }
        }

        if (klondikeSolitaire == null)
        {
            klondikeSolitaire = FindObjectOfType<KlondikeSolitaire>();
        }

        if (baseLocations == null)
        {
            baseLocations = GameObject.Find("BaseLocations").transform;
        }

        cardMover.MoveTo(vector3);
    }

    public void MoveTo(Vector3 vector3, float startTime)
    {
        if (cardMover == null)
        {
            CardMover cm = gameObject.GetComponent<CardMover>();
            if (cm == null)
            {
                cardMover = gameObject.AddComponent<CardMover>();
            }
            else
            {
                cardMover = cm;
            }
        }

        if (klondikeSolitaire == null)
        {
            klondikeSolitaire = FindObjectOfType<KlondikeSolitaire>();
        }

        if (baseLocations == null)
        {
            baseLocations = GameObject.Find("BaseLocations").transform;
        }

        cardMover.MoveTo(vector3, startTime);
    }

    public void Flip()
    {
        image.sprite = isBack ? frontImage : backImage;
        isBack = !isBack;
        klondikeSolitaire.PlayFlipSound();
    }

    public char GetNumberAsChar()
    {
        switch (number)
        {
            case NUMBER.ACE:
                return 'A';
            case NUMBER.TWO:
                return '2';
            case NUMBER.THREE:
                return '3';
            case NUMBER.FOUR:
                return '4';
            case NUMBER.FIVE:
                return '5';
            case NUMBER.SIX:
                return '6';
            case NUMBER.SEVEN:
                return '7';
            case NUMBER.EIGHT:
                return '8';
            case NUMBER.NINE:
                return '9';
            case NUMBER.TEN:
                return 'X';
            case NUMBER.JACK:
                return 'J';
            case NUMBER.QUEEN:
                return 'Q';
            case NUMBER.KING:
                return 'K';
        }

        throw new System.Exception("Invalid enum NUMBER found. number: "+number);
    }

    /**
     * This Card is clicked
     */
    public void OnPointerDown(PointerEventData eventData)
    {
        if((isBack || !SettingsManager.instance.IsGameActive() || (isDraw && transform.childCount>0) || (klondikeSolitaire.lastClickedFlippableCard!=null && transform.childCount>0)) || !SettingsManager.instance.CanMoveCard()) return; 

        klondikeSolitaire.PlayClickSound();

        returnPos = transform.localPosition;

        if (klondikeSolitaire.lastClickedFlippableCard != this)
        {
            //set as clicked
            if (klondikeSolitaire.lastClickedFlippableCard == null)
            {
                if (isDraw && transform.childCount > 0) return;

                //save this card
                klondikeSolitaire.lastClickedFlippableCard = this;
                tempParent = this.transform.parent;

                //get the outline and turn up the alpha
                ToggleColumnOutlineAlpha(this.transform, true);
            }
            else
            {
                //see if it is the next or previous card
                TryToPlace(klondikeSolitaire.lastClickedFlippableCard);

                //set back to null
                klondikeSolitaire.NullifyLastClicked();
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if ((!isBack && SettingsManager.instance.IsGameActive() && !(isDraw && transform.childCount > 0)) && SettingsManager.instance.CanMoveCard())
        {
            //does not block click
            returnToPrevPos = true;

            canvasGroup.blocksRaycasts = false;

            //set parent to baseLocations
            tempParent = transform.parent;

            transform.SetParent(baseLocations.transform.parent);

            klondikeSolitaire.draggingCard = this;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if ((!isBack && SettingsManager.instance.IsGameActive() && !(isDraw && transform.childCount > 0))&& SettingsManager.instance.CanMoveCard())
        {
            //get the rectTransform of this object
            RectTransform rectTransform = (RectTransform)transform;

            //get the max/min coords for this card
            float maxX=transform.position.x + rectTransform.rect.width,
                minX=transform.position.x - rectTransform.rect.width,
                maxY = transform.position.y + rectTransform.rect.height,
                minY = transform.position.y - rectTransform.rect.height
            ;

            //find all columns that match this
            foreach(GameObject location in klondikeSolitaire.sevenLocations.Concat(klondikeSolitaire.fourLocations))
            {
                //if we found a match, check y position of bottom element
                if (location.transform.position.x>minX && location.transform.position.x < maxX)
                {
                    //get the bottom child
                    GameObject locBottomChild = klondikeSolitaire.GetLastChild(location);

                    //check if y pos match
                    if(locBottomChild.transform.position.y>minY && locBottomChild.transform.position.y < maxY)
                    {
                        //check if FlippableCard or KlondikeDrop
                        FlippableCard touchedCard = locBottomChild.GetComponent<FlippableCard>();
                        if (touchedCard != null)
                        {
                            //try to place on this card
                            canvasGroup.blocksRaycasts = true;
                            if (touchedCard.TryToPlace(this)) return;
                        }

                        //was not flippable, check klondikeDrop
                        KlondikeDrop klondikeDrop = locBottomChild.GetComponent<KlondikeDrop>();
                        if (klondikeDrop != null)
                        {
                            //try to place on this card
                            canvasGroup.blocksRaycasts = true;
                            if (klondikeDrop.TryToPlace(this)) return;
                        }
                    }
                }
            }

            if (returnToPrevPos)
            {
                transform.SetParent(tempParent.transform);
                cardMover.MoveTo(returnPos);
            }
        }

        canvasGroup.blocksRaycasts = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if ((!isBack && SettingsManager.instance.IsGameActive() && !(isDraw && transform.childCount>0)) && SettingsManager.instance.CanMoveCard())
        {
            transform.position += (Vector3) eventData.delta / canvas.scaleFactor;
        }
    }

    /**
     * try to place flippableCards on this
     * 
     * deselect the lastClickedFlippableCard
     */
    public bool TryToPlace(FlippableCard flippableCards)
    {
        klondikeSolitaire.NullifyLastClicked();
        klondikeSolitaire.LabelAllCards();

        if (flippableCards == null || (flippableCards.isDraw && flippableCards.transform.childCount>0) || isDraw || transform.childCount>0) return false;

        if(flippableCards.isDraw) klondikeSolitaire.drawController.ResetDeckThree();

        //get the outline and turn up the alpha
        ToggleColumnOutlineAlpha(flippableCards.transform, false);

        bool flipParent = false;

        Transform lastParent= flippableCards.tempParent;
        Vector3 lastLocalPosition = flippableCards.returnPos;

        //if this card is in ace then we need to find the next card
        if (isInAce)
        {
            //see if the card can be a child of this
            if (!IsNextCard(flippableCards.number) || suit != flippableCards.suit || flippableCards.transform.childCount > 0)
            {
                return false;
            }
            else
            {
                flippableCards.isInAce = true;
                klondikeSolitaire.PlayMovedToAce();
            }
        }
        else
        {
            //see if the card can be a child of this
            if (!IsPrevCard(flippableCards.number) || !HasOppositeCard(flippableCards))
            {

                return false;
            }
            else
            {
                flippableCards.isInAce = false;
            }
        }

        //get parent. If parent can be flipped, flip
        FlippableCard parentsFlippableCard = flippableCards.tempParent?.GetComponent<FlippableCard>();
        if (parentsFlippableCard != null && parentsFlippableCard.isBack)
        {
            parentsFlippableCard.Flip();
            flipParent = true;
        }

        flippableCards.transform.SetParent(transform);

        Vector3 pos = Vector3.zero; //flippableCards.transform.parent.localPosition;

        if (!isInAce)
        {
            pos.y -= klondikeSolitaire.CARD_PLACEMENT_DIFFERENCE;
        }

        //flippableCards.transform.position = pos;

        if (flippableCards != null)
        {
            flippableCards.MoveTo(pos);
            flippableCards.returnToPrevPos = false;
        }

        if (flippableCards.isDraw)
        {
            flippableCards.isDraw = false;

            klondikeSolitaire.drawController.ResetDeckThree();
        }

        klondikeSolitaire.CheckVictory();
        klondikeSolitaire.AddHistory(flippableCards.gameObject, lastParent.gameObject, lastLocalPosition, false, flipParent);
        return true;
    }

    private bool IsPrevCard(NUMBER number)
    {
        switch (this.number)
        {
            case NUMBER.ACE:
                return false;
            case NUMBER.TWO:
                if (number == NUMBER.ACE)
                {
                    return true;
                }
                return false;
            case NUMBER.THREE:
                if (number == NUMBER.TWO)
                {
                    return true;
                }
                return false;
            case NUMBER.FOUR:
                if (number == NUMBER.THREE)
                {
                    return true;
                }
                return false;
            case NUMBER.FIVE:
                if (number == NUMBER.FOUR)
                {
                    return true;
                }
                return false;
            case NUMBER.SIX:
                if (number == NUMBER.FIVE)
                {
                    return true;
                }
                return false;
            case NUMBER.SEVEN:
                if (number == NUMBER.SIX)
                {
                    return true;
                }
                return false;
            case NUMBER.EIGHT:
                if (number == NUMBER.SEVEN)
                {
                    return true;
                }
                return false;
            case NUMBER.NINE:
                if (number == NUMBER.EIGHT)
                {
                    return true;
                }
                return false;
            case NUMBER.TEN:
                if (number == NUMBER.NINE)
                {
                    return true;
                }
                return false;
            case NUMBER.JACK:
                if (number == NUMBER.TEN)
                {
                    return true;
                }
                return false;
            case NUMBER.QUEEN:
                if (number == NUMBER.JACK)
                {
                    return true;
                }
                return false;
            case NUMBER.KING:
                if (number == NUMBER.QUEEN)
                {
                    return true;
                }
                return false;
        }

        Debug.LogError("NUMBER not found: " + this.number);
        return false;
    }

    private bool IsNextCard(NUMBER number)
    {
        switch (this.number)
        {
            case NUMBER.ACE:
                if (number == NUMBER.TWO)
                {
                    return true;
                }
                return false;
            case NUMBER.TWO:
                if (number == NUMBER.THREE)
                {
                    return true;
                }
                return false;
            case NUMBER.THREE:
                if (number == NUMBER.FOUR)
                {
                    return true;
                }
                return false;
            case NUMBER.FOUR:
                if (number == NUMBER.FIVE)
                {
                    return true;
                }
                return false;
            case NUMBER.FIVE:
                if (number == NUMBER.SIX)
                {
                    return true;
                }
                return false;
            case NUMBER.SIX:
                if (number == NUMBER.SEVEN)
                {
                    return true;
                }
                return false;
            case NUMBER.SEVEN:
                if (number == NUMBER.EIGHT)
                {
                    return true;
                }
                return false;
            case NUMBER.EIGHT:
                if (number == NUMBER.NINE)
                {
                    return true;
                }
                return false;
            case NUMBER.NINE:
                if (number == NUMBER.TEN)
                {
                    return true;
                }
                return false;
            case NUMBER.TEN:
                if (number == NUMBER.JACK)
                {
                    return true;
                }
                return false;
            case NUMBER.JACK:
                if (number == NUMBER.QUEEN)
                {
                    return true;
                }
                return false;
            case NUMBER.QUEEN:
                if (number == NUMBER.KING)
                {
                    return true;
                }
                return false;
            case NUMBER.KING:
                return false;
        }

        Debug.LogError("NUMBER not found: " + this.number);
        return false;
    }

    public NUMBER GetNextCard()
    {
        switch (number)
        {
            case NUMBER.ACE:
                return NUMBER.TWO;
            case NUMBER.TWO:
                return NUMBER.THREE;
            case NUMBER.THREE:
                return NUMBER.FOUR;
            case NUMBER.FOUR:
                return NUMBER.FIVE;
            case NUMBER.FIVE:
                return NUMBER.SIX;
            case NUMBER.SIX:
                return NUMBER.SEVEN;
            case NUMBER.SEVEN:
                return NUMBER.EIGHT;
            case NUMBER.EIGHT:
                return NUMBER.NINE;
            case NUMBER.NINE:
                return NUMBER.TEN;
            case NUMBER.TEN:
                return NUMBER.JACK;
            case NUMBER.JACK:
                return NUMBER.QUEEN;
            case NUMBER.QUEEN:
                return NUMBER.KING;
            case NUMBER.KING:
                return NUMBER.ACE;
        }

        Debug.LogError("NUMBER not found: " + this.number);
        return NUMBER.ACE;
    }

    public bool HasOppositeCard(FlippableCard parent)
    {
        switch (parent.suit)
        {
            case SUIT.Clubs:
            case SUIT.Spades:
                if (suit==SUIT.Diamonds|| suit == SUIT.Hearts)
                {
                    return true;
                }
                break;
            case SUIT.Diamonds:
            case SUIT.Hearts:
                if (suit == SUIT.Clubs|| suit == SUIT.Spades)
                {
                    return true;
                }
                break;
        }


        return false;
    }

    public void ToggleColumnOutlineAlpha(Transform startingObject, bool onOff)
    {
        Transform parent = startingObject;
        while(parent!=null)
        {
            Outline outline = parent.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = onOff?new Color(0, 0, 255, 127):Color.black;
            }

            parent = parent.childCount > 0 ? parent.GetChild(0) : null;
        }
    }

    public override string ToString()
    {
        return ("S" + suit + " N" + number);
    }
}
