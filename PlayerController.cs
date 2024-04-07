using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
Script description:
This script is used to control the player behaviour
attach this to the player instance

set the player layer
*/
public class PlayerController : MonoBehaviour
{
    [Header("Value Section")]
    public CharacterController2D controller;

    public float runSpeed;

    private float speed;

    public float climbSpeed;

    //the horizontal and vertical of player
    private float horizontal;

    private float vertical;

    // animator instance
    private Animator playerAnimator;

    // the radius of detection
    public float detectionRadius = 0.2f;

    public int PlayerLayerIndex1 = 8;

    public int PlayerLayerIndex2 = 8;

    [Header("Position Section")]
    // the detection point of player
    public Transform detectionPoint;

    // the detection layer name
    public LayerMask detectionLayer;

    // the detected item
    public GameObject item;

    [Header("State Section")]
    // the collider of the detected item
    public Collider2D itemCollider;

    // rigidbody instance
    private Rigidbody2D rb;

    // the player climbing state
    private bool isClimbing;

    bool facingRight = false;

    bool isJumping;

    public Animator animator;

    [Header("Manager section")]
    private GameObject UIManager;

    public void Start()
    {
        // assign the instance when game starts
        rb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
        UIManager = GameObject.FindWithTag("UIManager");

        //ignore collision on other characters
        Physics2D.IgnoreLayerCollision (PlayerLayerIndex1, PlayerLayerIndex2);
    }

    public void Update()
    {
        // always update those variable
        horizontal = Input.GetAxisRaw("Horizontal") * runSpeed;
        vertical = Input.GetAxisRaw("Vertical") * runSpeed;
        speed = Mathf.Abs(horizontal);

        if (Input.GetButtonDown("Jump"))
        {
            isJumping = true;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            attack();
        }

        //control the interaction of player
        interaction();
    }

    void FixedUpdate()
    {
        //Assign speed variable in animator
        playerAnimator.SetFloat("speed", speed);
        controller.Move(horizontal * Time.fixedDeltaTime, false, isJumping);
        isJumping = false;

        // activate climb method
        climb();
        walk();
    }

    public void attack()
    {
        Debug.Log("Attacking enemy");
        animator.SetTrigger("Attack");
    }

    // manage the interaction of items
    public void interaction()
    {
        // if the item collides with player and a key is pressed do the following action
        itemCollider =
            Physics2D
                .OverlapCircle(detectionPoint.position,
                detectionRadius,
                detectionLayer);
        if (itemCollider != null)
        {
            // assign the collided item to a the collided object
            item = itemCollider.gameObject;

            //if key E is pressed
            if (Input.GetKeyDown(KeyCode.E))
            {
                item.GetComponent<ItemBehaviour>().itemBehaviour();
            }

            //if key W is pressed
            if (Input.GetKeyDown(KeyCode.W) && item.tag == "door") goUp(item);

            //if key S is pressed
            if (Input.GetKeyDown(KeyCode.S) && item.tag == "door") goDown(item);
            if (item.tag == "ladder" && Mathf.Abs(vertical) > 0f)
                isClimbing = true;
        }
        else
        {
            isClimbing = false;
        }
    }

    // go up method for door
    public void goUp(GameObject targetItem)
    {
        // variables for goUp and goDown methods
        float distance = Mathf.Infinity;

        float diffy = 0;

        float diffx = 0;

        float currentDistance;

        // All doors in game
        GameObject[] allNextDoor = GameObject.FindGameObjectsWithTag("door");

        // the closest doors in game
        GameObject closestDoorUp = targetItem;

        // filter out the doors in all potential door
        foreach (GameObject door in allNextDoor)
        {
            diffy =
                Mathf
                    .Abs(door.transform.position.y -
                    targetItem.transform.position.y);
            diffx =
                Mathf
                    .Abs(door.transform.position.x -
                    targetItem.transform.position.x);
            currentDistance = diffy;

            // make sure the door position x is alligned
            if (
                diffx >= 0 &&
                diffx < 1 &&
                diffy != 0 &&
                currentDistance < distance
            )
                if (door.transform.position.y > transform.position.y)
                {
                    {
                        closestDoorUp = door;
                        distance = currentDistance;
                    }
                }
        }

        //set the current character position to the door position
        GameObject CurrentCharacter =
            UIManager.GetComponent<UIManager>().CurrentCharacter;
        CurrentCharacter.transform.position = closestDoorUp.transform.position;

        //debug information
        Debug.Log("Going Up");
    }

    // go down method for door
    public void goDown(GameObject targetItem)
    {
        // variables for goUp and goDown methods
        float distance = Mathf.Infinity;

        float diffy = 0;

        float diffx = 0;

        float currentDistance;

        // All doors in game
        GameObject[] allNextDoor = GameObject.FindGameObjectsWithTag("door");

        // the closest doors in game
        GameObject closestDoorDown = targetItem;

        // filter out the doors in all potential door
        foreach (GameObject door in allNextDoor)
        {
            diffy =
                Mathf
                    .Abs(door.transform.position.y -
                    targetItem.transform.position.y);
            diffx =
                Mathf
                    .Abs(door.transform.position.x -
                    targetItem.transform.position.x);
            currentDistance = diffy;

            // make sure the door position x is alligned
            if (
                diffx >= 0 &&
                diffx < 1 &&
                diffy != 0 &&
                currentDistance < distance
            )
                if (door.transform.position.y < transform.position.y)
                {
                    {
                        closestDoorDown = door;
                        distance = currentDistance;
                    }
                }
        }

        //set the current character position to the door position
        GameObject CurrentCharacter =
            UIManager.GetComponent<UIManager>().CurrentCharacter;
        CurrentCharacter.transform.position =
            closestDoorDown.transform.position;

        //debug information
        Debug.Log("Going down");
    }

    //The climb method for player
    public void climb()
    {
        if (isClimbing)
        {
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(rb.velocity.x, vertical * climbSpeed);
            GetComponent<Collider2D>().enabled = false;
        }
        else
        {
            rb.gravityScale = 10f;
            GetComponent<Collider2D>().enabled = true;
        }
    }

    public void walk()
    {
        controller.Move(horizontal * Time.fixedDeltaTime, false, false);

        if (horizontal > 0 && !facingRight)
        {
            flip();
        }
        if (horizontal < 0 && !facingRight)
        {
            flip();
        }
    }

    public void flip()
    {
        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;
        facingRight = !facingRight;
    }

    public void FreezeCharacter()
    {
        Rigidbody2D m_Rigidbody;
        m_Rigidbody = gameObject.GetComponent<Rigidbody2D>();
        m_Rigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
        this.gameObject.GetComponent<Animator>().SetBool("Can walk", false);
        Debug.Log("Character freezed");
    }

    public void UnfreezeCharacter()
    {
        Rigidbody2D m_Rigidbody;
        m_Rigidbody = gameObject.GetComponent<Rigidbody2D>();
        m_Rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        this.gameObject.GetComponent<Animator>().SetBool("Can walk", true);
        Debug.Log("Character unfreezed");
    }
}
