using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeCardItem : MonoBehaviour
{
    [SerializeField] int index;
    [SerializeField] bool frontOrBack;

    public void SetCard(int index, bool frontOrBack)
    {
        this.index = index;
        this.frontOrBack = frontOrBack;
    }

    public void OnClick()
    {
        ChangeCards.instance.ClickCard(index, frontOrBack);
    }
}
