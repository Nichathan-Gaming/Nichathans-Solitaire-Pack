using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/**
 * Creates a popup.
 * 
 * Only 1 should exist at a time.
 */
public class VerificationPopUp : MonoBehaviour
{
    public void CreatePopUp(GameObject parent, GameObject popUpPrefab, string message, string verificationText, string cancellationText, bool hasCancel, UnityAction verifyAction, UnityAction cancelAction)
    {
        //returns BackgroundImage
        Transform createdPopUp = Instantiate(popUpPrefab, parent.transform).transform.GetChild(0);

        createdPopUp.Find("MessagePopUpText").GetComponent<Text>().text = message;

        Transform verificationTransform = createdPopUp.Find("VerificationButton");
        verificationTransform.GetComponent<Button>().onClick.AddListener(()=>
        {
            verifyAction();
            Destroy(createdPopUp.parent.gameObject);

        });
        verificationTransform.Find("VerificationText").GetComponent<Text>().text = verificationText;

        Transform cancelationTransform = createdPopUp.Find("CancelationButton");
        cancelationTransform.GetComponent<Button>().onClick.AddListener(()=> 
        {
            cancelAction();
            Destroy(createdPopUp.parent.gameObject);
        });
        cancelationTransform.Find("CancellationText").GetComponent<Text>().text = cancellationText;

        GameObject cancelButton = createdPopUp.Find("CancelButton").gameObject;
        if (!hasCancel)
        {
            if (cancelButton != null)
            {
                cancelButton.SetActive(false);
            }
        }
        else
        {
            cancelButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                Destroy(createdPopUp.parent.gameObject);
            });
        }
    }
}
