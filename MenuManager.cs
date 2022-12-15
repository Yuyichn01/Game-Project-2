using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    // button variables
    public Button startButton;

    public int SceneTransitionIndex1;

    public Button settingButton;

    public int SceneTransitionIndex2;

    public Button exitButton;

    // the background image
    public Image background;

    void Start()
    {
        // when button is clicked, do action
        Button btn1 = startButton.GetComponent<Button>();
        btn1.onClick.AddListener (startGame);

        Button btn2 = settingButton.GetComponent<Button>();
        btn2.onClick.AddListener (goToSetting);

        Button btn3 = exitButton.GetComponent<Button>();
        btn3.onClick.AddListener (exitGame);
    }

    void Update()
    {
        // always make sure the background is larger than canvas
        RectTransform RTCanvas = GetComponent<RectTransform>();
        background.rectTransform.sizeDelta =
            new Vector2(RTCanvas.rect.width * 2, RTCanvas.rect.height * 2);
    }

    void startGame()
    {
        Debug.Log("the game is started");
        SceneManager.LoadScene (SceneTransitionIndex1);
    }

    void goToSetting()
    {
        SceneManager.LoadScene (SceneTransitionIndex2);
    }

    void exitGame()
    {
        Application.Quit();
    }
}
