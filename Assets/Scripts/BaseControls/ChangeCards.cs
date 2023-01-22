using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChangeCards : MonoBehaviour
{
    public static ChangeCards instance;

    [SerializeField] Transform cardHolderBack;
    [SerializeField] Transform cardHolderFront;
    [SerializeField] GameObject prefab;

    [SerializeField] List<Outline> backOutlines;
    [SerializeField] List<Outline> frontOutlines;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this; // In first scene, make us the singleton.
        }
        else if (instance != this)
        {
            Destroy(instance.gameObject); // On reload, singleton already set, so destroy duplicate.
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //load prefab into cardHolder for each in SM
        int index = 0;

        foreach (Sprite image in SettingsManager.instance.backs)
        {
            GameObject newCard = Instantiate(prefab, cardHolderBack);
            newCard.GetComponent<Image>().sprite = image;
            backOutlines.Add(newCard.GetComponent<Outline>());

            if (SettingsManager.activeBack == index)
            {
                backOutlines[index].effectColor = Color.black;
            }
            else
            {
                backOutlines[index].effectColor = Color.clear;
            }

            newCard.GetComponent<ChangeCardItem>().SetCard(index++, false);
        }

        index = 0;
        foreach (Sprite image in SettingsManager.instance.GetFrontAces())
        {
            GameObject newCard = Instantiate(prefab, cardHolderFront);
            newCard.GetComponent<Image>().sprite = image;
            frontOutlines.Add(newCard.GetComponent<Outline>());

            if (SettingsManager.activeDeck == index)
            {
                frontOutlines[index].effectColor = Color.black;
            }
            else
            {
                frontOutlines[index].effectColor = Color.clear;
            }

            newCard.GetComponent<ChangeCardItem>().SetCard(index++, true);
        }
    }

    public void BackToMain()
    {
        SettingsManager.instance.LoadScene("MainMenuScene");
    }

    public void ClickCard(int index, bool frontOrBack)
    {
        if (frontOrBack)
        {
            SettingsManager.activeDeck = index;

            for(int i = 0; i < frontOutlines.Count; i++)
            {
                if (i == index)
                {
                    frontOutlines[i].effectColor = Color.black;
                    continue;
                }

                frontOutlines[i].effectColor = Color.clear;
            }
        }
        else
        {
            SettingsManager.activeBack = index;

            for (int i = 0; i < backOutlines.Count; i++)
            {
                if (i == index)
                {
                    backOutlines[i].effectColor = Color.black;
                    continue;
                }

                backOutlines[i].effectColor = Color.clear;
            }
        }
    }
}
