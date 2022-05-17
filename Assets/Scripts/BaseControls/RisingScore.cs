using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RisingScore : MonoBehaviour
{
    public Text scoreText;

    public CanvasGroup canvasGroup;

    public bool isActive;

    const float RISING_SPEED = 5,
        FADE_SPEED=0.01f;

    // Update is called once per frame
    void Update()
    {
        if (isActive)
        {
            //move up
            transform.position += new Vector3(0, RISING_SPEED);

            //fade
            canvasGroup.alpha -= FADE_SPEED;

            if(canvasGroup.alpha < FADE_SPEED)
            {
                isActive = false;
                canvasGroup.alpha = 0;
                //Destroy(this.gameObject);
            }
        }
    }

    /**
     * Sets the text, use override method to show different symbol(make later if needed)
     */
    public void StartRising(int score)
    {
        canvasGroup.alpha = 1;

        float halfWidth = GetComponent<RectTransform>().rect.width / 2;

        float vectX = Random.Range(halfWidth, Screen.width - halfWidth);

        transform.position = new Vector3(vectX, Screen.height/2);

        if (score < 0)
        {
            scoreText.text = "" + score;
        }
        else
        {
            scoreText.text = "+" + score;
        }
        isActive = true;
    }
}
