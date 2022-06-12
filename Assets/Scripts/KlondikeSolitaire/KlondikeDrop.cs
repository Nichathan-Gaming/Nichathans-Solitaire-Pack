using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KlondikeDrop : MonoBehaviour, IDropHandler, IPointerDownHandler
{
    public bool allowsAceOrKing = false;
    KlondikeSolitaire klondikeSolitaire;

    private void Start()
    {
        klondikeSolitaire = FindObjectOfType<KlondikeSolitaire>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        TryToPlace(eventData.pointerDrag?.GetComponent<FlippableCard>());
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (klondikeSolitaire.lastClickedFlippableCard != null)
        {
            //try to place
            TryToPlace(klondikeSolitaire.lastClickedFlippableCard);
        }
    }

    public bool TryToPlace(FlippableCard flippableCards)
    {
        klondikeSolitaire.NullifyLastClicked();

        if (flippableCards == null || this.transform.childCount>0) return false;

        flippableCards.ToggleColumnOutlineAlpha(flippableCards.transform, false);

        Transform lastParent = flippableCards.tempParent;
        Vector3 lastLocalPosition = flippableCards.returnPos;
        bool flipParent = false;

        //see if dropped is Ace or King
        if (allowsAceOrKing)
        {
            //check if this card is Ace
            if (flippableCards.number != 0)
            {
                return false;
            }
            else
            {
                flippableCards.isInAce = true;
            }
        }
        else
        {
            //check if this card is King
            if (flippableCards.number != 12)
            {
                return false;
            }
            else
            {
                flippableCards.isInAce = false;
            }
        }

        //get parent. If parent can be flipped, flip
        FlippableCard parentsFlippableCard = flippableCards.tempParent.GetComponent<FlippableCard>();
        if (parentsFlippableCard != null && parentsFlippableCard.isBack)
        {
            parentsFlippableCard.Flip();
            flipParent = true;
        }

        //set the card
        if (flippableCards != null)
        {
            flippableCards.returnToPrevPos = false;

            flippableCards.transform.SetParent(transform);
            //flippableCards.transform.position = transform.position;
            flippableCards.MoveTo(Vector3.zero);
        }

        if (flippableCards.isDraw)
        {
            flippableCards.isDraw = false;

            DrawController.instance.ResetDeckThree();
        }

        flippableCards.gameObject.GetComponent<CanvasGroup>().blocksRaycasts = true;

        klondikeSolitaire.AddHistory(flippableCards.gameObject, lastParent.gameObject, lastLocalPosition, false, flipParent);

        return true;
    }
}
