using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
this script manages the whether, including the sky colour and nvironmental sound
*/
public class WhetherManager : MonoBehaviour
{
    public Image bgImage;

    [SerializeField, Header("渐变方式")]
    private GradientType gradientType = GradientType.Vertical;

    [SerializeField, Header("开始颜色")]
    private Color startColor = Color.white;

    [SerializeField, Header("结束颜色")]
    private Color endColor = Color.black;

    [SerializeField, Header("渐变曲线(0~1)")]
    private AnimationCurve
        gradientCurve =
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    public enum GradientType
    {
        None,
        Vertical,
        Horizontal,
        Diagonal
    }

    private void Awake()
    {
        bgImage = GetComponent<Image>();
    }

    private void Start()
    {
        SetGradientColor();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            gradientType = GradientType.None;
            SetGradientColor();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            gradientType = GradientType.Vertical;
            SetGradientColor();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            gradientType = GradientType.Horizontal;
            SetGradientColor();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            gradientType = GradientType.Diagonal;
            SetGradientColor();
        }
    }

    private void SetGradientColor()
    {
        //创建Texture2D
        Vector2Int imageSize = new Vector2Int(Screen.width, Screen.height);
        Texture2D texture2D = new Texture2D(imageSize.x, imageSize.y);

        //遍历像素点
        switch (gradientType)
        {
            case GradientType.Vertical:
                for (int y = 0; y < imageSize.y; y++)
                {
                    Color pixelColor = GetColorByCurve(1.0f * y / imageSize.y);
                    for (int x = 0; x < imageSize.x; x++)
                    {
                        texture2D.SetPixel (x, y, pixelColor);
                    }
                }
                break;
            case GradientType.Horizontal:
                for (int x = 0; x < imageSize.x; x++)
                {
                    Color pixelColor = GetColorByCurve(1.0f * x / imageSize.x);
                    for (int y = 0; y < imageSize.y; y++)
                    {
                        texture2D.SetPixel (x, y, pixelColor);
                    }
                }
                break;
            case GradientType.Diagonal:
                for (int x = 0; x < imageSize.x; x++)
                {
                    for (int y = 0; y < imageSize.y; y++)
                    {
                        Color pixelColor =
                            GetColorByCurve(0.5f * x / imageSize.x +
                            0.5f * y / imageSize.y);
                        texture2D.SetPixel (x, y, pixelColor);
                    }
                }
                break;
            default:
                for (int x = 0; x < imageSize.x; x++)
                {
                    for (int y = 0; y < imageSize.y; y++)
                    {
                        texture2D.SetPixel (x, y, startColor);
                    }
                }
                break;
        }
        texture2D.Apply();

        //创建Sprite
        Sprite sprite =
            Sprite
                .Create(texture2D,
                new Rect(0, 0, imageSize.x, imageSize.y),
                new Vector2(0.5f, 0.5f));
        sprite.name = "GradientBg";
        if (bgImage != null) bgImage.sprite = sprite;
    }

    private Color GetColorByCurve(float ratio)
    {
        float curveValue = gradientCurve.Evaluate(ratio);
        curveValue = Mathf.Clamp01(curveValue);
        return Color.Lerp(startColor, endColor, curveValue);
    }
}
