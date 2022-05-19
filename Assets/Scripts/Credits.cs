using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Credits : MonoBehaviour
{
    public void Back()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    public void OpenItch()
    {
        Application.OpenURL("https://nichathan.itch.io/");
    }
}
