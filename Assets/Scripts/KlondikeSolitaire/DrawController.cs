using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawController : MonoBehaviour
{
    public static DrawController instance;

    //the image of the drawn deck, replace when drawn deck is empty
    private Image drawDeckImage;

    private float nextClick = 0;
    private float clickDelay = 0.25f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this; // In first scene, make us the singleton.
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(instance.gameObject); // On reload, singleton already set, so destroy duplicate.
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        drawDeckImage = GameObject.Find("DrawDeck").GetComponent<Image>();
    }

    /**
     * Grabs the last child of 'deck' and adds it to the last child of 'drawnCardHolder'
     */
    public void DrawOne(Transform drawChild)
    {
        SettingsManager.instance.PlayDealSound();
        Transform parentTransform = KlondikeSolitaire.instance.GetLastChild(KlondikeSolitaire.instance.drawnCardHolder).transform;

        drawChild.SetParent(parentTransform);

        FlippableCard flippableCards = drawChild.GetComponent<FlippableCard>();
        flippableCards.Flip();
        flippableCards.isDraw = true;
        flippableCards.MoveTo(Vector3.zero);
    }

    private void DrawThree(Transform drawChild)
    {
        if(KlondikeSolitaire.instance.drawnCardHolder.transform.childCount>0) Reset(KlondikeSolitaire.instance.drawnCardHolder.transform.GetChild(0));

        //get the parent we are moving to.
        Transform parentTransform = KlondikeSolitaire.instance.GetLastChild(KlondikeSolitaire.instance.drawnCardHolder).transform;

        KlondikeSolitaire.instance.AddHistory(drawChild.gameObject, drawChild.transform.parent.gameObject, Vector3.zero, true, false);
        //set the parent
        drawChild.SetParent(parentTransform);

        int count = 0;
        while (true)
        {
            SettingsManager.instance.PlayDealSound();

            //get the flippable card
            FlippableCard flippableCards = drawChild.GetComponent<FlippableCard>();
            flippableCards.Flip();
            flippableCards.isDraw = true;

            Vector3 pos = Vector3.zero;

            int drawnCardHolderChildCount = CountChildren(KlondikeSolitaire.instance.drawnCardHolder.transform);
            int drawChildChildCount = CountChildren(drawChild.transform);

            if (drawChildChildCount < 2 && !((drawChildChildCount==1 && drawnCardHolderChildCount==2) || (drawChildChildCount == 0 && drawnCardHolderChildCount == 1)))
            {
                pos = new Vector3(KlondikeSolitaire.instance.CARD_PLACEMENT_DIFFERENCE, 0);
            }

            flippableCards.MoveTo(pos, (Time.time+(count*0.1f)));

            count++;

            if (drawChild.childCount < 1) break;
            drawChild = drawChild.GetChild(0);

            KlondikeSolitaire.instance.AddHistory(drawChild.gameObject, drawChild.transform.parent.gameObject, Vector3.zero, true, false, true);
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
        if (KlondikeSolitaire.instance.drawnCardHolder.transform.childCount > 0)
        {
            //set the current card outline to clear
            GameObject currentLastChild = KlondikeSolitaire.instance.GetLastChild(KlondikeSolitaire.instance.drawnCardHolder);

            //make sure that this card is no longer selected
            if (KlondikeSolitaire.instance.lastClickedFlippableCard!=null && KlondikeSolitaire.instance.lastClickedFlippableCard.gameObject.Equals(currentLastChild))
            {
                KlondikeSolitaire.instance.NullifyLastClicked();
            }

            Outline outline = currentLastChild.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = Color.black;
            }
        }


        //refresh if no cards left
        if (KlondikeSolitaire.instance.deck.transform.childCount <1)
        {
            //starts at 0, triggers to 1, then triggers to 2. 2 > 1, stops
            if (!SettingsManager.instance.IsLimitDeckRefresh() || SettingsManager.instance.CanRefreshDeck(KlondikeSolitaire.instance.deckRefreshText))
            {
                //add the back to this card and set alpha back to normal
                while (KlondikeSolitaire.instance.drawnCardHolder.transform.childCount > 0)
                {
                    GameObject lastChild = KlondikeSolitaire.instance.GetLastChild(KlondikeSolitaire.instance.drawnCardHolder);

                    Transform parentTransform = KlondikeSolitaire.instance.GetLastChild(KlondikeSolitaire.instance.deck).transform;

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

                KlondikeSolitaire.instance.AddHistory();
            }

            //place drawnCardHolder here
            return;
        }

        Transform drawChild = KlondikeSolitaire.instance.GetLastChild(KlondikeSolitaire.instance.deck).transform;

        if (SettingsManager.instance.IsDrawThree())
        {
            //we have a drawChild which is the bottom most child in the deck.
            //we need to see if drawChild has a parent and a grandparent that are not deck.name
            for(int i = 0; i < 2; i++)
            {
                string parentName = drawChild.transform.parent.gameObject.name;
                
                //if the parent is not the deck then child equals parent
                if (!parentName.Equals(KlondikeSolitaire.instance.deck.name)){
                    drawChild = drawChild.parent;
                }
            }

            DrawThree(drawChild);
            //ResetDeckThree();
        }
        else
        {
            KlondikeSolitaire.instance.AddHistory(drawChild.gameObject, drawChild.transform.parent.gameObject, Vector3.zero, true, false);

            DrawOne(drawChild);
        }

        if (KlondikeSolitaire.instance.deck.transform.childCount == 0)
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
        if (KlondikeSolitaire.instance.drawnCardHolder.transform.childCount < 1) return 0;

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
        if (!SettingsManager.instance.IsDrawThree() || KlondikeSolitaire.instance.drawnCardHolder.transform.childCount < 1) return;

        Transform holder = KlondikeSolitaire.instance.drawnCardHolder.transform.GetChild(0);

        int count = Reset(holder);

        //moves the last 2 children CARD_PLACEMENT_DIFFERENCE to the right of each other
        if (count>1)
        {
            //move the last over
            Transform lastChild = KlondikeSolitaire.instance.GetLastChild(holder.gameObject).transform;
            FlippableCard flippable = lastChild.GetComponent<FlippableCard>();

            //move over
            //lastChild.localPosition += new Vector3(KlondikeSolitaire.instance.CARD_PLACEMENT_DIFFERENCE, 0);
            //flippable.MoveTo(new Vector3(KlondikeSolitaire.instance.CARD_PLACEMENT_DIFFERENCE, 0));

            Transform parent = lastChild.parent;

            flippable.MoveTo(new Vector3(KlondikeSolitaire.instance.CARD_PLACEMENT_DIFFERENCE, 0));

            if (count > 2)
            {
                //move the last parent over
                //lastChild.parent.localPosition += new Vector3(KlondikeSolitaire.instance.CARD_PLACEMENT_DIFFERENCE, 0);

                parent.GetComponent<FlippableCard>().MoveTo(new Vector3(KlondikeSolitaire.instance.CARD_PLACEMENT_DIFFERENCE, 0), Time.time+(1 * 0.1f));
            }
        }
    }
}
