using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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

    [SerializeField]
    private float m_JumpForce = 400f; // Amount of force added when the player jumps.

    [Range(0, 1)]
    [SerializeField]
    private float m_CrouchSpeed = .36f; // Amount of maxSpeed applied to crouching movement. 1 = 100%

    [Range(0, .3f)]
    [SerializeField]
    private float m_MovementSmoothing = .05f; // How much to smooth out the movement

    [SerializeField]
    private bool m_AirControl = false; // Whether or not a player can steer while jumping;

    [SerializeField]
    private LayerMask m_WhatIsGround; // A mask determining what is ground to the character

    [SerializeField]
    private Transform m_GroundCheck; // A position marking where to check if the player is grounded.

    [SerializeField]
    private Transform m_CeilingCheck; // A position marking where to check for ceilings

    [SerializeField]
    private Collider2D m_CrouchDisableCollider; // A collider that will be disabled when crouching

    const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded

    private bool m_Grounded; // Whether or not the player is grounded.

    const float k_CeilingRadius = .2f; // Radius of the overlap circle to determine if the player can stand up

    private Rigidbody2D m_Rigidbody2D;

    private Vector3 m_Velocity = Vector3.zero;

    [Header("Position Section")]
    // the detection point of player
    public Transform detectionPoint;

    // the detection layer name
    public LayerMask detectionLayer;

    // the detected item
    public GameObject item;

    [Header("Events")]
    [Space]
    public UnityEvent OnLandEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    public BoolEvent OnCrouchEvent;

    private bool m_wasCrouching = false;

    [Header("State Section")]
    // the collider of the detected item
    public Collider2D itemCollider;

    // rigidbody instance
    private Rigidbody2D rb;

    // the player climbing state
    private bool isClimbing;

    bool isJumping;

    public Animator animator;

    [Header("Character UI Section")]
    public Sprite CharacterMiniSprite;

    public Sprite CharacterDialogSprite;

    [Header("Item Section")]
    public List<Item> Items = new List<Item>();

    [Header("Status Section")]
    // set the player's health
    public float health = 0;

    public float Fatigue = 0;

    public float Starvation = 0;

    private float timer;

    // set the player's hunger
    public static int hunger = 0;

    [Header("Combat section")]
    public Transform attackPoint;

    public float attackRange = 0.5f;

    public LayerMask enemyLayers;

    public int attackDamage = 40;

    public bool closeCombat = true;

    public GameObject projectilePrefab; // The projectile prefab (arrow, bullet, or bomb)

    public float launchForce = 20f; // Base launch force

    public float launchAngle = 45f; // Adjustable angle for trajectory (in degrees)

    [Header("Manager section")]
    private GameObject UIManager;

    [Header("Follower section")]
    public List<GameObject> followers; // List of other characters

    public float followDistance = 2.0f; // Distance to maintain while following

    public float followSpeed = 5.0f; // Speed at which followers move

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();

        if (OnLandEvent == null) OnLandEvent = new UnityEvent();

        if (OnCrouchEvent == null) OnCrouchEvent = new BoolEvent();
    }

    public void Start()
    {
        // assign the instance when game starts
        rb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
        UIManager = GameObject.FindWithTag("UIManager");

        //ignore collision on other characters
        Physics2D.IgnoreLayerCollision (PlayerLayerIndex1, PlayerLayerIndex2);

        foreach (GameObject follower in followers)
        {
            if (follower != null)
            {
                bool isFollowerFacingRight =
                    follower.transform.localScale.x > 0;
            }
        }
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
        Move(horizontal * Time.fixedDeltaTime, false, isJumping);
        isJumping = false;

        // activate climb method
        climb();
        walk();

        bool wasGrounded = m_Grounded;
        m_Grounded = false;

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.
        Collider2D[] colliders =
            Physics2D
                .OverlapCircleAll(m_GroundCheck.position,
                k_GroundedRadius,
                m_WhatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                m_Grounded = true;
                if (!wasGrounded) OnLandEvent.Invoke();
            }
        }

        UpdateFollowers();
    }

    public void Move(float move, bool crouch, bool jump)
    {
        // If crouching, check to see if the character can stand up
        if (!crouch)
        {
            // If the character has a ceiling preventing them from standing up, keep them crouching
            if (
                Physics2D
                    .OverlapCircle(m_CeilingCheck.position,
                    k_CeilingRadius,
                    m_WhatIsGround)
            )
            {
                crouch = true;
            }
        }

        // Only control the player if grounded or airControl is turned on
        if (m_Grounded || m_AirControl)
        {
            // If crouching
            if (crouch)
            {
                if (!m_wasCrouching)
                {
                    m_wasCrouching = true;
                    OnCrouchEvent.Invoke(true);
                }

                // Reduce the speed by the crouchSpeed multiplier
                move *= m_CrouchSpeed;

                // Disable one of the colliders when crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = false;
            }
            else
            {
                // Enable the collider when not crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = true;

                if (m_wasCrouching)
                {
                    m_wasCrouching = false;
                    OnCrouchEvent.Invoke(false);
                }
            }

            // Move the character by finding the target velocity
            Vector3 targetVelocity =
                new Vector2(move * 10f, m_Rigidbody2D.velocity.y);

            // And then smoothing it out and applying it to the character
            m_Rigidbody2D.velocity =
                Vector3
                    .SmoothDamp(m_Rigidbody2D.velocity,
                    targetVelocity,
                    ref m_Velocity,
                    m_MovementSmoothing);

            // If the input is moving the player right and the player is facing left...
            if (move > 0 && transform.localScale.x > 0)
            {
                Flip(); // Flip to face right
            }
            else if (move < 0 && transform.localScale.x < 0)
            {
                Flip(); // Flip to face left
            }
        }

        // If the player should jump...
        if (m_Grounded && jump)
        {
            // Add a vertical force to the player.
            m_Grounded = false;
            m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
        }
    }

    public void attack()
    {
        //check current weapon
        //update attack values
        //if it is close combat
        if (closeCombat)
        {
            Debug.Log("Attacking enemy, close combat");
            animator.SetTrigger("Attack");

            Collider2D[] hitEnemies =
                Physics2D
                    .OverlapCircleAll(attackPoint.position,
                    attackRange,
                    enemyLayers);

            foreach (Collider2D enemy in hitEnemies)
            {
                Debug.Log("hit" + enemy.name);
                enemy.GetComponent<EnemyAI>().TakeDamage(attackDamage);
            }
        }
        else
        {
            Debug.Log("Attacking enemy, long range combat");
            ShootProjectile();
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || attackPoint == null)
        {
            Debug.LogError("ProjectilePrefab or attackPoint is not assigned!");
            return;
        }

        // Instantiate the projectile
        GameObject projectile =
            Instantiate(projectilePrefab,
            attackPoint.position,
            attackPoint.rotation);

        // Determine the launch direction based on the character's facing direction
        Vector2 launchDirection =
            transform.localScale.x < 0 ? Vector2.right : Vector2.left;

        // Apply force to the projectile
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = launchDirection * launchForce;
        }
        else
        {
            Debug
                .LogError("Projectile prefab does not have a Rigidbody2D component!");
        }

        // Add damage script to the projectile if not present
        if (projectile.GetComponent<ProjectileDamage>() == null)
        {
            projectile.AddComponent<ProjectileDamage>();
        }
    }

    private Vector2 CalculateLaunchDirection()
    {
        // Convert angle to radians
        float angleInRadians = launchAngle * Mathf.Deg2Rad;

        // Calculate direction based on angle
        Vector2 direction =
            new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));

        return direction.normalized; // Normalize the direction
    }

    //visually display the attacking circle
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    // manage the interaction of items
    public void interaction()
    {
        // if collision is detected betwen objects and character
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

        if (UIManager.GetComponent<UIManager>().isSingle == true)
        {
            // Move only the current character
            GameObject CurrentCharacter =
                UIManager.GetComponent<UIManager>().CurrentCharacter;
            CurrentCharacter.transform.position =
                closestDoorUp.transform.position;
        }
        else
        {
            // Teleport all characters (current character and followers) to the next door
            GameObject CurrentCharacter =
                UIManager.GetComponent<UIManager>().CurrentCharacter;

            // Move the current character
            CurrentCharacter.transform.position =
                closestDoorUp.transform.position;

            // Move all followers to the door position
            PlayerController playerController =
                CurrentCharacter.GetComponent<PlayerController>();
            if (playerController != null)
            {
                foreach (GameObject follower in playerController.followers)
                {
                    if (follower != null)
                    {
                        follower.transform.position =
                            closestDoorUp.transform.position;
                    }
                }
            }
        }

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

        if (UIManager.GetComponent<UIManager>().isSingle == true)
        {
            // Move only the current character
            GameObject CurrentCharacter =
                UIManager.GetComponent<UIManager>().CurrentCharacter;
            CurrentCharacter.transform.position =
                closestDoorDown.transform.position;
        }
        else
        {
            // Teleport all characters (current character and followers) to the next door
            GameObject CurrentCharacter =
                UIManager.GetComponent<UIManager>().CurrentCharacter;

            // Move the current character
            CurrentCharacter.transform.position =
                closestDoorDown.transform.position;

            // Move all followers to the door position
            PlayerController playerController =
                CurrentCharacter.GetComponent<PlayerController>();
            if (playerController != null)
            {
                foreach (GameObject follower in playerController.followers)
                {
                    if (follower != null)
                    {
                        follower.transform.position =
                            closestDoorDown.transform.position;
                    }
                }
            }
        }

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
        Move(horizontal * Time.fixedDeltaTime, false, false);
    }

    public void Add(Item item)
    {
        Item tmpItem = item;
        Items.Add (tmpItem);
    }

    public void Remove(Item item)
    {
        Items.Remove (item);
    }

    public void Utilize(Item myitem)
    {
        Item tmpItem = myitem;
        switch (tmpItem.ItemName)
        {
            case "material1":
                // code block
                break;
            case "FriedInstantNoodle":
                // add blood
                health = health + 10;

                //if the last pannel opened is inventory, remove from inventory
                if (
                    UIManager.GetComponent<UIManager>().lastOpenedPannel ==
                    "Inventory"
                )
                    Remove(tmpItem);
                else if (
                    UIManager.GetComponent<UIManager>().lastOpenedPannel ==
                    "Cooker" //if it clicked from cooker
                ) item.gameObject.GetComponent<Cooker>().CancelCook(myitem);
                break;
            case "Bow":
                // equip the bow
                closeCombat = false;
                projectilePrefab = tmpItem.projectile;
                break;
            default:
                Debug
                    .Log("Plase define the item name correctly, the item name does not match");
                break;
        }
    }

    private void Flip()
    {
        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1; // Flip the x-axis
        transform.localScale = currentScale;
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

    public void UpdateFollowers()
    {
        for (int i = 0; i < followers.Count; i++)
        {
            GameObject follower = followers[i];
            if (follower == null) continue;

            // Get the Animator component of the follower
            Animator followerAnimator = follower.GetComponent<Animator>();
            if (followerAnimator == null) continue;

            // Calculate a unique follow distance for each follower based on their index
            float uniqueFollowDistance = followDistance + i * 1.5f; // Adjust the multiplier for spacing as needed

            // Calculate the horizontal distance (x-axis only)
            float distanceX =
                Mathf.Abs(transform.position.x - follower.transform.position.x);

            // Follow only if the x-axis distance is greater than the unique follow distance
            if (distanceX > uniqueFollowDistance)
            {
                // Calculate the new x position for the follower
                float targetX =
                    Mathf
                        .MoveTowards(follower.transform.position.x,
                        transform.position.x,
                        followSpeed * Time.deltaTime);

                // Flip the follower to face the movement direction
                if (
                    targetX > follower.transform.position.x &&
                    follower.transform.localScale.x > 0
                )
                {
                    FlipFollower (follower);
                }
                else if (
                    targetX < follower.transform.position.x &&
                    follower.transform.localScale.x < 0
                )
                {
                    FlipFollower (follower);
                }

                // Update the follower's position, changing only the x-axis
                follower.transform.position =
                    new Vector3(targetX,
                        follower.transform.position.y,
                        follower.transform.position.z);

                // Set the speed in the follower's Animator
                followerAnimator.SetFloat("speed", followSpeed);
            }
            else
            {
                // Stop animation when within unique follow distance
                followerAnimator.SetFloat("speed", 0);
            }
        }
    }

    private void FlipFollower(GameObject follower)
    {
        Vector3 scale = follower.transform.localScale;
        scale.x *= -1; // Flip the x-axis
        follower.transform.localScale = scale;
    }
}
