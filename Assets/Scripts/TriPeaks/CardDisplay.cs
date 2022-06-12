using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour, IPointerDownHandler
{
    Sprite frontImage;
    Sprite backImage;
    [SerializeField]private Image thisImage;
    public int cardNumber;
    public int index;

    public void SetCard(int suit, int number)
    {
        backImage = SettingsManager.instance.GetCardBack();
        frontImage = SettingsManager.instance.GetCardFront(suit, number);
        cardNumber = number;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TriPeaksController.instance.HandleClickCard(this);
        //Debug.Log("name: "+this.name+"\nindex: "+index);
    }

    public void SetImage(bool frontOrBack)
    {
        thisImage.sprite = frontOrBack ? frontImage : backImage;
    }
}
