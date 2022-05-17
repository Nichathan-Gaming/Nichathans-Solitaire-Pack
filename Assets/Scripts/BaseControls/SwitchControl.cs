using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SwitchControl : MonoBehaviour, IPointerDownHandler
{
    [Header("Controlling booleans")]
    public bool active=true; 
    public bool requiresVerification =false;
    public bool isNormalSwitch = true;

    [Header("What is this switch called?")]
    public string title="";

    [Header("Only used for verification pop up")]
    public string verificationMessage = "";
    public string verificationLeftButtonText = "Verify";
    public string verificationRightButtonText = "Cancel";

    [Header("Do not assign in inspector")]
    //the prefab that we use with the verification pop up
    public GameObject popUpPrefab;

    #region hidden from inspector
    //The actions, hidden from inspector
    public UnityAction<bool> switchControlFunction;
    public UnityAction switchControlToggle;

    public UnityAction leftButtonSelected = () => { };
    public UnityAction rightButtonSelected = ()=> { };

    public GameObject gameViewMainCanvas;

    private Text titleText;

    private Image dotImage;

    private Transform dotTransform;

    private int difference = 50;

    private Color red=new Color(255, 0, 0, 255), green=new Color(0, 255, 0, 255);

    private VerificationPopUp verificationPop;
    #endregion hidden from inspector

    private void Awake()
    {
        if(gameViewMainCanvas==null) gameViewMainCanvas = GameObject.Find("GameViewMainCanvas");

        if (isNormalSwitch)
        {
            dotTransform = transform.GetChild(0);
            dotImage = dotTransform.GetComponent<Image>();

            titleText = transform.GetChild(1).GetComponent<Text>();
            titleText.text = title;

            AssignDot();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (verificationPop == null)
        {
            verificationPop = gameObject.AddComponent<VerificationPopUp>();
        }

        if (requiresVerification)
        {
            verificationPop.CreatePopUp(
                gameViewMainCanvas,
                popUpPrefab,
                verificationMessage,
                verificationLeftButtonText,
                verificationRightButtonText,
                true,
                leftButtonSelected,
                rightButtonSelected
            );
        }
        else if(isNormalSwitch)
        {//if is normal switch and requires verification then this must be ran in the accept button
            active = !active;
            AssignDot();

            switchControlFunction?.Invoke(active);
            switchControlToggle?.Invoke();
        }
    }

    public void SetTitle(string title)
    {
        this.title = title;
        if (titleText != null)
        {
            titleText.text = title;
        }
    }

    public void AssignDot()
    {
        Vector3 position = new Vector3(((active?-1:1) * difference), 0);
        dotTransform.localPosition = position;
        dotImage.color = active ? green : red;
    }
}
