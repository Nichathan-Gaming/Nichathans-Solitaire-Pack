using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Credits : MonoBehaviour
{
    public void Back()
    {
        SettingsManager.instance.LoadScene("MainMenuScene");
    }

    public void OpenItch()
    {
        Application.OpenURL("https://nichathan.itch.io/");
    }

    public void OpenGitHub()
    {
        Application.OpenURL("https://github.com/Nichathan-Gaming/Nichathans-Solitaire-Pack");
    }

    public void OpenGooglePlay()
    {
        Application.OpenURL("https://play.google.com/store/apps/dev?id=5505294983591200024");
    }

    public void OpenGmail()
    {
        Application.OpenURL("mailto:JNichols@NichathanGaming.com");
    }
}
