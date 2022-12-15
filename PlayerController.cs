using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // set the speed of player
    public float speed = 1f;

    // animator instance
    private Animator playerAnimator;

    // the radius of detection
    public float detectionRadius = 0.2f;

    // the detection point of player
    public Transform detectionPoint;

    // the detection layer name
    public LayerMask detectionLayer;

    // the detected item
    GameObject item;

    // the collider of the detected item
    private Collider2D itemCollider;

    // the inventory open status
    private bool isOpen;

    // rigidbody instance
    private Rigidbody2D rb;

    void Awake()
    {
        // assign the instance when game starts
        rb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        //control the interaction of player
        interaction();

        //control the player movement
        Move(Input.GetAxisRaw("Horizontal"));
    }

    void FixedUpdate()
    {
        //trigger the player animation
        playerAnimator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
    }

    // move the player
    void Move(float direction)
    {
        // the velovity of player
        rb.velocity = new Vector2(direction * speed, rb.velocity.y);

        // switch the position of player
        if (direction > 0)
        {
            if (transform.localScale.x < 0)
                transform.localScale =
                    new Vector3(transform.localScale.x * -1,
                        transform.localScale.y,
                        transform.localScale.z);
        }
        if (direction < 0)
        {
            if (transform.localScale.x > 0)
                transform.localScale =
                    new Vector3(transform.localScale.x * -1,
                        transform.localScale.y,
                        transform.localScale.z);
        }
    }

    // manage the interaction of items
    void interaction()
    {
        itemCollider =
            Physics2D
                .OverlapCircle(detectionPoint.position,
                detectionRadius,
                detectionLayer);

        if (itemCollider != null)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                item = itemCollider.gameObject;
                item.GetComponent<ItemBehaviour>().reaction();
            }
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            playerAnimator.SetBool("Can walk", isOpen);
            if (isOpen)
            {
                GetComponent<Rigidbody2D>().constraints =
                    RigidbodyConstraints2D.None;
                GetComponent<Rigidbody2D>().constraints =
                    RigidbodyConstraints2D.FreezeRotation;
            }
            else
            {
                GetComponent<Rigidbody2D>().constraints =
                    RigidbodyConstraints2D.FreezeAll;
            }
            isOpen = !isOpen;
            FindObjectOfType<InventorySystem>().InventoryUI.SetActive(isOpen);
        }
    }
}
