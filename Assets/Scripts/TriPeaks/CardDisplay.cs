using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour, IPointerDownHandler
{
    public TriPeaksController triPeaksController;

    public Sprite frontImage;
    public Sprite backImage;
    [SerializeField]private Image thisImage;
    public int cardNumber;
    public int index;

    public void OnPointerDown(PointerEventData eventData)
    {
        triPeaksController.HandleClickCard(this);
        //Debug.Log("name: "+this.name+"\nindex: "+index);
    }

    public void SetImage(bool frontOrBack)
    {
        thisImage.sprite = frontOrBack ? frontImage : backImage;
    }
}
