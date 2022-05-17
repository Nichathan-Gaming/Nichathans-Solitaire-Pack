using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropCatcher : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;

        if (dropped != null)
        {
            dropped.GetComponent<RectTransform>().anchoredPosition = GetComponent<RectTransform>().anchoredPosition;

            DragAndDrop dragAndDrop = dropped.GetComponent<DragAndDrop>();
            if (dragAndDrop != null)
            {
                dragAndDrop.returnToPrevPos = false;
            }
        }
    }
}
