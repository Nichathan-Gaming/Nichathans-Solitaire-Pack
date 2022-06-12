using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

//public enum SUIT{Spades='S', Clubs='C', Hearts='H', Diamonds='D' };
//public enum NUMBER { ACE, TWO, THREE, FOUR, FIVE, SIX, SEVEN, EIGHT, NINE, TEN, JACK, QUEEN, KING};

public class FlippableCard : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    //public SUIT suit=SUIT.Spades;
    //public NUMBER number=NUMBER.ACE;
    public int suit;
    public int number;

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
        
        backImage = SettingsManager.instance.GetCardBack();//Resources.Load<Sprite>("Images/SolitaireCards/CardBack");
        frontImage = SettingsManager.instance.GetCardFront(suit, number);//Resources.Load<Sprite>("Images/SolitaireCards/" + suit + "/" + GetNumberAsChar() + (char)suit);

        image.sprite = isBack ? backImage : frontImage;

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
        SettingsManager.instance.PlayFlipSound();
    }

    /**
     * This Card is clicked
     */
    public void OnPointerDown(PointerEventData eventData)
    {
        if((isBack || !SettingsManager.instance.IsGameActive() || (isDraw && transform.childCount>0) || (KlondikeSolitaire.instance.lastClickedFlippableCard!=null && transform.childCount>0)) || !SettingsManager.instance.CanMoveCard()) return;

        SettingsManager.instance.PlayClickSound();

        returnPos = transform.localPosition;

        if (KlondikeSolitaire.instance.lastClickedFlippableCard != this)
        {
            //set as clicked
            if (KlondikeSolitaire.instance.lastClickedFlippableCard == null)
            {
                if (isDraw && transform.childCount > 0) return;

                //save this card
                KlondikeSolitaire.instance.lastClickedFlippableCard = this;
                tempParent = this.transform.parent;

                //get the outline and turn up the alpha
                ToggleColumnOutlineAlpha(this.transform, true);
            }
            else
            {
                //see if it is the next or previous card
                TryToPlace(KlondikeSolitaire.instance.lastClickedFlippableCard);

                //set back to null
                KlondikeSolitaire.instance.NullifyLastClicked();
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

            KlondikeSolitaire.instance.draggingCard = this;
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
            foreach(GameObject location in KlondikeSolitaire.instance.sevenLocations.Concat(KlondikeSolitaire.instance.fourLocations))
            {
                //if we found a match, check y position of bottom element
                if (location.transform.position.x>minX && location.transform.position.x < maxX)
                {
                    //get the bottom child
                    GameObject locBottomChild = KlondikeSolitaire.instance.GetLastChild(location);

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
        KlondikeSolitaire.instance.NullifyLastClicked();
        KlondikeSolitaire.instance.LabelAllCards();

        if (flippableCards == null || (flippableCards.isDraw && flippableCards.transform.childCount>0) || isDraw || transform.childCount>0) return false;

        if(flippableCards.isDraw) DrawController.instance.ResetDeckThree();

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
                SettingsManager.instance.PlayCheersSound();
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
            pos.y -= KlondikeSolitaire.instance.CARD_PLACEMENT_DIFFERENCE;
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

            DrawController.instance.ResetDeckThree();
        }

        KlondikeSolitaire.instance.CheckVictory();
        KlondikeSolitaire.instance.AddHistory(flippableCards.gameObject, lastParent.gameObject, lastLocalPosition, false, flipParent);
        return true;
    }

    private bool IsPrevCard(int number)
    {
        if (this.number == 0) return false;

        return this.number - 1 == number;
    }

    private bool IsNextCard(int number)
    {
        if (this.number > 11) return false;

        return this.number + 1 == number;
    }

    public int GetNextCard()
    {
        if (number > 11) return 0;

        return number + 1;
    }

    public bool HasOppositeCard(FlippableCard parent)
    {
        switch (parent.suit)
        {
            case 0:
            case 1:
                if (suit==2|| suit == 3)
                {
                    return true;
                }
                break;
            case 2:
            case 3:
                if (suit == 0|| suit == 1)
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
