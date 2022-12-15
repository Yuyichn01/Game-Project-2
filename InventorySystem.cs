using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// attach the script to Canvas
public class InventorySystem : MonoBehaviour
{
    // the canvas of the scene
    public GameObject canvas;

    [Header("Inventory Section")]
    // the background image
    public Image background;

    //the Item pannel
    public Image ItemsPannel;

    // the Inventory UI
    public GameObject InventoryUI;

    // the length between pannel and boundary
    public float pannelBoundary;

    // list of items picked up
    [Header("Item Section")]
    public List<GameObject> Items = new List<GameObject>();

    [Header("Player Section")]
    // set the player's health
    public static int health = 0;

    // set the player's hunger
    public static int hunger = 0;

    // Start is called before the first frame update
    void Start()
    {
    }

    void Update()
    {
        Allign();
    }

    // a pick up method to pick up items
    public void addItem(GameObject item)
    {
        Items.Add (item);
    }

    void Allign()
    {
        // initialize the background position
        background.transform.position = new Vector3(0, 0, 0);

        // always make sure the background is larger than canvas
        RectTransform RTCanvas = canvas.GetComponent<RectTransform>();
        background.rectTransform.sizeDelta =
            new Vector2(RTCanvas.rect.width * 2, RTCanvas.rect.height * 2);

        // make sure the Item Pannel is smaller than canvas
        ItemsPannel.rectTransform.offsetMin =
            new Vector2((RTCanvas.rect.width / 2 - pannelBoundary) * -1,
                (RTCanvas.rect.height / 2 - pannelBoundary) * -1);
        ItemsPannel.rectTransform.offsetMax =
            new Vector2(RTCanvas.rect.width / 2 - pannelBoundary,
                RTCanvas.rect.height / 2 - pannelBoundary);
    }
}
