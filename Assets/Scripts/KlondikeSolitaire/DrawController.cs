using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawController : MonoBehaviour
{
    public KlondikeSolitaire klondikeSolitaire;

    //the image of the drawn deck, replace when drawn deck is empty
    private Image drawDeckImage;

    private float nextClick = 0;
    private float clickDelay = 0.25f;

    // Start is called before the first frame update
    void Start()
    {
        drawDeckImage = GameObject.Find("DrawDeck").GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    /**
     * Grabs the last child of 'deck' and adds it to the last child of 'drawnCardHolder'
     */
    public void DrawOne(Transform drawChild)
    {
        klondikeSolitaire.PlayDealSound();
        Transform parentTransform = klondikeSolitaire.GetLastChild(klondikeSolitaire.drawnCardHolder).transform;

        drawChild.SetParent(parentTransform);

        FlippableCard flippableCards = drawChild.GetComponent<FlippableCard>();
        flippableCards.Flip();
        flippableCards.isDraw = true;
        flippableCards.MoveTo(Vector3.zero);
    }

    private void DrawThree(Transform drawChild)
    {
        if(klondikeSolitaire.drawnCardHolder.transform.childCount>0)Reset(klondikeSolitaire.drawnCardHolder.transform.GetChild(0));

        //get the parent we are moving to.
        Transform parentTransform = klondikeSolitaire.GetLastChild(klondikeSolitaire.drawnCardHolder).transform;

        klondikeSolitaire.AddHistory(drawChild.gameObject, drawChild.transform.parent.gameObject, Vector3.zero, true, false);
        //set the parent
        drawChild.SetParent(parentTransform);

        int count = 0;
        while (true)
        {
            klondikeSolitaire.PlayDealSound();

            //get the flippable card
            FlippableCard flippableCards = drawChild.GetComponent<FlippableCard>();
            flippableCards.Flip();
            flippableCards.isDraw = true;

            Vector3 pos = Vector3.zero;

            int drawnCardHolderChildCount = CountChildren(klondikeSolitaire.drawnCardHolder.transform);
            int drawChildChildCount = CountChildren(drawChild.transform);

            if (drawChildChildCount < 2 && !((drawChildChildCount==1 && drawnCardHolderChildCount==2) || (drawChildChildCount == 0 && drawnCardHolderChildCount == 1)))
            {
                pos = new Vector3(klondikeSolitaire.CARD_PLACEMENT_DIFFERENCE, 0);
            }

            flippableCards.MoveTo(pos, (Time.time+(count*0.1f)));

            count++;

            if (drawChild.childCount < 1) break;
            drawChild = drawChild.GetChild(0);

            klondikeSolitaire.AddHistory(drawChild.gameObject, drawChild.transform.parent.gameObject, Vector3.zero, true, false, true);
        }
    }

    private int CountChildren(Transform parent)
    {
        if (parent.childCount < 1) return 0;

        int count = 1;

        Transform currentChild = parent.GetChild(0);

        while (currentChild.childCount > 0)
        {
            currentChild = currentChild.GetChild(0);
            count++;
        }

        return count;
    }

    public void Draw()
    {
        //see if we can draw
        if(Time.time > nextClick)
        {
            nextClick = Time.time + clickDelay;
        }
        else
        {
            return;
        }


        //makes sure that we remove the outline
        if (klondikeSolitaire.drawnCardHolder.transform.childCount > 0)
        {
            //set the current card outline to clear
            GameObject currentLastChild = klondikeSolitaire.GetLastChild(klondikeSolitaire.drawnCardHolder);

            //make sure that this card is no longer selected
            if (klondikeSolitaire.lastClickedFlippableCard!=null && klondikeSolitaire.lastClickedFlippableCard.gameObject.Equals(currentLastChild))
            {
                klondikeSolitaire.NullifyLastClicked();
            }

            Outline outline = currentLastChild.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = Color.clear;
            }
        }


        //refresh if no cards left
        if (klondikeSolitaire.deck.transform.childCount <1)
        {
            //starts at 0, triggers to 1, then triggers to 2. 2 > 1, stops
            if (!(SettingsManager.instance.IsLimitDeckRefresh() && SettingsManager.instance.CanRefreshDeck(klondikeSolitaire.deckRefreshText)))
            {
                //add the back to this card and set alpha back to normal
                while (klondikeSolitaire.drawnCardHolder.transform.childCount > 0)
                {
                    GameObject lastChild = klondikeSolitaire.GetLastChild(klondikeSolitaire.drawnCardHolder);

                    Transform parentTransform = klondikeSolitaire.GetLastChild(klondikeSolitaire.deck).transform;

                    lastChild.transform.SetParent(parentTransform);
                    lastChild.transform.localPosition = Vector3.zero;
                    FlippableCard flippable = lastChild.GetComponent<FlippableCard>();
                    if (flippable != null)
                    {
                        flippable.Flip();
                        flippable.isDraw = true;
                    }
                    else
                    {
                        Debug.Log("lastChild in drawController does not have flippable card : " + lastChild.name);
                    }

                    //flippable.MoveTo(Vector3.zero);
                }

                klondikeSolitaire.AddHistory();
            }

            //place drawnCardHolder here
            return;
        }

        Transform drawChild = klondikeSolitaire.GetLastChild(klondikeSolitaire.deck).transform;

        if (SettingsManager.instance.IsDrawThree())
        {
            //we have a drawChild which is the bottom most child in the deck.
            //we need to see if drawChild has a parent and a grandparent that are not deck.name
            for(int i = 0; i < 2; i++)
            {
                string parentName = drawChild.transform.parent.gameObject.name;
                
                //if the parent is not the deck then child equals parent
                if (!parentName.Equals(klondikeSolitaire.deck.name)){
                    drawChild = drawChild.parent;
                }
            }

            DrawThree(drawChild);
            //ResetDeckThree();
        }
        else
        {
            klondikeSolitaire.AddHistory(drawChild.gameObject, drawChild.transform.parent.gameObject, Vector3.zero, true, false);

            DrawOne(drawChild);
        }

        if (klondikeSolitaire.deck.transform.childCount == 0)
        {
            //change background and set alpha
            Color c = drawDeckImage.color;
            c.a = 0.5f;
            drawDeckImage.color = c;
        }
    }

    /**
     * moves all cards to drawDeckLocation
     * 
     * loop through the children one at a time, set their local position to 0 and then do it for thier children
     */
    private int Reset(Transform first)
    {
        //no cards to move, leave
        if (klondikeSolitaire.drawnCardHolder.transform.childCount < 1) return 0;

        int count = 1;
        Transform current = first;
        
        while (true)
        {
            //set current transform to 0
            current.localPosition = Vector3.zero;
            //get the FlippableCard of current
            //current.GetComponent<FlippableCard>().MoveTo(Vector3.zero);

            if (current.childCount < 1) break;
            count++;

            current = current.GetChild(0);
        }

        return count;
    }

    /**
     * If draw three, always try to have the bottom 2 children pushed over
     * 
     * call on card moved out of deck and undo
     */
    public void ResetDeckThree()
    {
        if (!SettingsManager.instance.IsDrawThree() || klondikeSolitaire.drawnCardHolder.transform.childCount < 1) return;

        Transform holder = klondikeSolitaire.drawnCardHolder.transform.GetChild(0);

        int count = Reset(holder);

        //moves the last 2 children CARD_PLACEMENT_DIFFERENCE to the right of each other
        if (count>1)
        {
            //move the last over
            Transform lastChild = klondikeSolitaire.GetLastChild(holder.gameObject).transform;
            FlippableCard flippable = lastChild.GetComponent<FlippableCard>();

            //move over
            //lastChild.localPosition += new Vector3(klondikeSolitaire.CARD_PLACEMENT_DIFFERENCE, 0);
            //flippable.MoveTo(new Vector3(klondikeSolitaire.CARD_PLACEMENT_DIFFERENCE, 0));

            Transform parent = lastChild.parent;

            flippable.MoveTo(new Vector3(klondikeSolitaire.CARD_PLACEMENT_DIFFERENCE, 0));

            if (count > 2)
            {
                //move the last parent over
                //lastChild.parent.localPosition += new Vector3(klondikeSolitaire.CARD_PLACEMENT_DIFFERENCE, 0);

                parent.GetComponent<FlippableCard>().MoveTo(new Vector3(klondikeSolitaire.CARD_PLACEMENT_DIFFERENCE, 0), Time.time+(1 * 0.1f));
            }
        }
    }
}
