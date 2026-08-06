using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerGuide : MonoBehaviour
{
    public TextMeshProUGUI uiText;

    // 使用本地化 key 而非硬编码文字
    [Tooltip("key 列表: guide_move, guide_interact, guide_jump")]
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
            (InputHelper.GetKeyDown(KeyCode.A) || InputHelper.GetKeyDown(KeyCode.D))
        )
        {
            isADKeyPressed = true;
            currentInstruction = instructions[1];
            DisplayInstruction();
        } // Check for player input (E key)
        else if (isADKeyPressed && !isEKeyPressed && InputHelper.GetKeyDown(KeyCode.E)
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
            InputHelper.GetKeyDown(KeyCode.W) ||
            InputHelper.GetKeyDown(KeyCode.S) ||
            InputHelper.GetKeyDown(KeyCode.UpArrow) ||
            InputHelper.GetKeyDown(KeyCode.DownArrow)
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
            string localizedText = LocalizationManager.Get(currentInstruction);
            fadeCoroutine =
                StartCoroutine(FadeTextToFullAlpha(1f, localizedText));
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
