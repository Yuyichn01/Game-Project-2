using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FlashingEffect : MonoBehaviour
{
    //image to fade in and out
    public Image image;

    public bool Reverse;

    public float FadingTime;

    public float FadingPeriod;

    private bool loop = true;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        while (loop == true)
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
