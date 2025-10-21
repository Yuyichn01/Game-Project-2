using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerGuide : MonoBehaviour
{
    public TextMeshProUGUI uiText;

    public List<string> instructions;

    private string currentInstruction;

    private bool isADKeyPressed = false;

    private bool isEKeyPressed = false;

    private bool isUpDownKeyPressed = false;

    private Coroutine fadeCoroutine;

    void Start()
    {
        // Initial instruction
        currentInstruction = instructions[0];
        DisplayInstruction();
    }

    void Update()
    {
        // Check for player input (A or D keys)
        if (
            !isADKeyPressed &&
            (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
        )
        {
            isADKeyPressed = true;
            currentInstruction = instructions[1];
            DisplayInstruction();
        } // Check for player input (E key)
        else if (isADKeyPressed && !isEKeyPressed && Input.GetKeyDown(KeyCode.E)
        )
        {
            isEKeyPressed = true;
            currentInstruction = instructions[2];
            DisplayInstruction();
        }
        else if (
            isADKeyPressed &&
            isEKeyPressed &&
            !isUpDownKeyPressed &&
            Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.DownArrow)
        )
        {
            ClearInstruction();
        }
    }

    void DisplayInstruction()
    {
        if (uiText != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine (fadeCoroutine);
            }
            fadeCoroutine =
                StartCoroutine(FadeTextToFullAlpha(1f, currentInstruction));
        }
        else
        {
            Debug.LogError("UI Text component is not assigned!");
        }
    }

    void ClearInstruction()
    {
        if (uiText != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine (fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeTextToClear(1f));
        }
        else
        {
            Debug.LogError("UI Text component is not assigned!");
        }
    }

    IEnumerator FadeTextToFullAlpha(float duration, string newText)
    {
        uiText.text = newText;
        uiText.color =
            new Color(uiText.color.r, uiText.color.g, uiText.color.b, 0);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            uiText.color =
                new Color(uiText.color.r,
                    uiText.color.g,
                    uiText.color.b,
                    alpha);
            yield return null;
        }
        uiText.color =
            new Color(uiText.color.r, uiText.color.g, uiText.color.b, 1);
    }

    IEnumerator FadeTextToClear(float duration)
    {
        float elapsed = 0f;
        Color initialColor = uiText.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1 - (elapsed / duration));
            uiText.color =
                new Color(initialColor.r,
                    initialColor.g,
                    initialColor.b,
                    alpha);
            yield return null;
        }
        uiText.color =
            new Color(initialColor.r, initialColor.g, initialColor.b, 0);
        uiText.text = "";
    }
}
