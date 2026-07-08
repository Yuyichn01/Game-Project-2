using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// attach the script to Canvas
public class LogoManager : MonoBehaviour
{
    // the logo image
    public Image logo;

    //the time range for fade in
    public float FadeInTime;

    //the time range for fade out
    public float FadeOutTime;

    //the waiting time in seconds for fade in
    public float WaitingTimeIn;

    //the waiting time in seconds for fade out
    public float WaitingTimeOut;

    //the index of next scene
    public int NextSceneIndex;

    IEnumerator Start()
    {
        //initialize the alpha value to 0
        logo.canvasRenderer.SetAlpha(0.0f);

        //fade in method
        logo.CrossFadeAlpha(1.0f, FadeInTime, false);
        yield return new WaitForSeconds(WaitingTimeIn);

        //fade out method
        logo.CrossFadeAlpha(0.0f, FadeOutTime, false);
        yield return new WaitForSeconds(WaitingTimeOut);

        //Calling the LoadScene method
        SceneManager.LoadScene (NextSceneIndex);
    }
}
