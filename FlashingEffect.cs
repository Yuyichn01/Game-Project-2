using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
Author: Yi Yu

Date:2024/4/27

Description:
The script that creates flashing effect of images such as logo

*/
public class FlashingEffect : MonoBehaviour
{
    //image to fade in and out
    public Image image;

    public bool Reverse = false;

    public bool PlayOnce = false;

    private float FadingTime = 1;

    public float FadingPeriod;

    public bool loop = true;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        if (PlayOnce == true)
        {
            image.canvasRenderer.SetAlpha(1.0f);
            yield return new WaitForSeconds(FadingPeriod);
            FadeOut();
            yield return new WaitForSeconds(FadingPeriod);
            image.canvasRenderer.SetAlpha(0.0f);
            this.gameObject.SetActive(false);
        }
        else
        {
            do
            {
                if (Reverse == true)
                {
                    image.canvasRenderer.SetAlpha(0.0f);
                    yield return new WaitForSeconds(FadingPeriod);
                    FadeIn();
                    yield return new WaitForSeconds(FadingPeriod);
                    FadeOut();
                    yield return new WaitForSeconds(FadingPeriod);
                    image.canvasRenderer.SetAlpha(1.0f);
                }
                else
                {
                    image.canvasRenderer.SetAlpha(1.0f);
                    yield return new WaitForSeconds(FadingPeriod);
                    FadeOut();
                    yield return new WaitForSeconds(FadingPeriod);
                    FadeIn();
                    yield return new WaitForSeconds(FadingPeriod);
                    image.canvasRenderer.SetAlpha(0.0f);
                }
            }
            while (loop == true);
        }
    }

    // Update is called once per frame
    void FadeIn()
    {
        image.CrossFadeAlpha(1.0f, FadingTime, false);
    }

    //The FadeOut function
    void FadeOut()
    {
        image.CrossFadeAlpha(0.0f, FadingTime, false);
    }
}
