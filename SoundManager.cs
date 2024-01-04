using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Create a new empty object called Sound manager and attach this script to it
*/
public class SoundManager : MonoBehaviour
{
    public GameObject UIManager;

    // Start is called before the first frame update
    void Start()
    {
        int dayIndex = UIManager.GetComponent<UIManager>().DayIndex;
        Debug.Log("The day index retrieved from UImanager is " + dayIndex);
    }
}
