using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
Script description:
This script is used to control the player behaviour
attach this to the player instance

set the player layer
*/
public class PlayerController : MonoBehaviour, ISaveable
{
    [Header("Value Section")]
    public float runSpeed;

    public float MouseRunSpeed;

    private float speed;

    public float climbSpeed;

    public float JumpForce;

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
    private float m_MovementSmoothing = .05f; // How much to smooth out the movement

    private bool m_Grounded; // Whether or not the player is grounded.

    private Rigidbody2D m_Rigidbody2D;

    private Vector3 m_Velocity = Vector3.zero;

    [Header("Position Section")]
    // the detection point of player
    public Transform detectionPoint;

    // the detection layer name
    public LayerMask detectionLayer;

    // the detected item
    public GameObject item;

    private Vector2 targetPosition;

    public float groundCheckDistance = 0.1f;

    private bool isMoving = false;

    private Vector3 targetPos;

    [Header("Events")]
    [Space]
    public UnityEvent OnLandEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    public BoolEvent OnCrouchEvent;

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

    public Sprite CharacterDialogHappy;

    public Sprite CharacterDialogNormal;

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
    private UIManager uiManagerComp;

    [Header("Click Indicator")]
    [Tooltip("点击移动时在目标位置生成的箭头预制体（需要带 SpriteRenderer）。")]
    public GameObject clickIndicatorPrefab;

    [Tooltip("箭头上下浮动幅度。")]
    public float indicatorFloatHeight = 0.3f;

    [Tooltip("箭头浮动速度。")]
    public float indicatorFloatSpeed = 2f;

    [Tooltip("角色抵达后箭头渐隐时长（秒）。")]
    public float indicatorFadeDuration = 1f;

    private GameObject activeIndicator;
    private Coroutine indicatorFloatCoroutine;
    private Coroutine indicatorFadeCoroutine;

    [Header("Save section")]
    [SerializeField] private int characterIndex;

    [Header("Follower section")]
    public List<GameObject> followers; // List of other characters

    public float followDistance = 2.0f; // Distance to maintain while following

    public float followSpeed = 5.0f; // Speed at which followers move

    private System.Random rd = new System.Random();

    private static bool InputState = true;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();

        if (DataManager.Instance != null)
            DataManager.Instance.Register(this);
    }

    public void Start()
    {
        // assign the instance when game starts
        rb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
        UIManager = GameObject.FindWithTag("UIManager");
        uiManagerComp = UIManager?.GetComponent<UIManager>();

        //ignore collision on other characters
        Physics2D.IgnoreLayerCollision(PlayerLayerIndex1, PlayerLayerIndex2);

        // Ensure no residual velocity from physics initialization causes unintended drift
        // before the first FixedUpdate runs.
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public void Update()
    {
        if (InputState && this.gameObject == UIManager.GetComponent<UIManager>().CurrentCharacter)
        {
            // Detect  input
            horizontal = InputHelper.GetAxisRaw("Horizontal");
            vertical = InputHelper.GetAxisRaw("Vertical");
            speed = Mathf.Abs(horizontal);

            // If there is input, cancel mouse movement
            if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
            {
                if (!uiManagerComp.panelOpened)
                {
                    isMoving = false; // Cancel mouse click movement
                    playerAnimator.SetBool("Can walk", true);
                }
            }

            // Mouse click movement
            if (
                InputHelper.GetMouseButtonDown(0) &&
                !UIManager.GetComponent<UIManager>().panelOpened
            )
            {
                //if it is on UI button, return
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                //translate screen position to game scene position
                Vector3 mouseScreenPos = InputHelper.mousePosition;
                mouseScreenPos.z =
                    Mathf
                        .Abs(Camera.main.transform.position.z -
                        transform.position.z -
                        5.0f);
                Vector3 worldPos =
                    Camera.main.ScreenToWorldPoint(mouseScreenPos);

                targetPos =
                    new Vector3(worldPos.x, worldPos.y, transform.position.z);
                isMoving = true;

                // ── 生成点击指示箭头 ──
                SpawnClickIndicator(targetPos);

                //check if it is clicked on an interactive UI
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.up);

                //if the UI is not null and it is an interactive item, interact
                if (
                    hit.collider != null &&
                    hit.collider.gameObject != this.gameObject
                )
                {
                    Debug.Log("Clicked on: " + hit.collider.name);

                    ItemBehaviour ib =
                        hit.collider.gameObject.GetComponent<ItemBehaviour>();

                    if (
                        ib != null &&
                        hit
                            .collider
                            .gameObject
                            .GetComponent<ItemBehaviour>()
                            .interactionUI
                            .enabled
                    )
                    {
                        ib.itemBehaviour();
                        rb.linearVelocity = Vector2.zero;
                        isMoving = false;
                        DestroyClickIndicator();

                        // Ensure idle animation
                        playerAnimator.SetFloat("speed", 0f);
                        playerAnimator.SetBool("Can walk", false);
                        return;
                    }
                }
            }

            if (InputHelper.GetKeyDown(KeyCode.Q) || InputHelper.GetMouseButtonDown(1))
            {
                attack();
            }

            // Player interactions
            interaction();
        }
    }

    void FixedUpdate()
    {
        // Handle keyboard movement
        if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
        {
            Move(horizontal * runSpeed * Time.fixedDeltaTime, false, isJumping);
            isJumping = false;

            // Flip character based on keyboard input
            if (horizontal != 0)
            {
                Flip(-horizontal);
            }

            if (!UIManager.GetComponent<UIManager>().panelOpened)
            {
                // Set running animation
                playerAnimator.SetFloat("speed", Mathf.Abs(horizontal));
            }
        }
        else if (isMoving)
        {
            playerAnimator.SetBool("Can walk", true);

            // Mouse click movement
            Vector3 direction = (targetPos - transform.position).normalized;
            rb.linearVelocity =
                new Vector2(direction.x * MouseRunSpeed, rb.linearVelocity.y);

            // Flip character based on direction
            // Flip character based on movement direction
            if (direction.x > 0)
            {
                transform.localScale =
                    new Vector3(-Mathf.Abs(transform.localScale.x),
                        transform.localScale.y,
                        transform.localScale.z);
            }
            else if (direction.x < 0)
            {
                transform.localScale =
                    new Vector3(Mathf.Abs(transform.localScale.x),
                        transform.localScale.y,
                        transform.localScale.z);
            }

            // Set running animation
            playerAnimator
                .SetFloat("speed", Mathf.Abs(direction.x * MouseRunSpeed));

            //Determine how close to stop when reach target(accuracy)
            if (Mathf.Abs(transform.position.x - targetPos.x) < 1f)
            {
                rb.linearVelocity = Vector2.zero;
                isMoving = false;

                // ── 开始渐隐指示箭头 ──
                FadeOutClickIndicator();

                // Ensure idle animation
                playerAnimator.SetFloat("speed", 0f);
                playerAnimator.SetBool("Can walk", false);
            }
        }
        else
        {
            // No input → idle
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            // Only set idle animation for CurrentCharacter;
            // followers' animation is driven by UpdateFollowers()
            if (this.gameObject == UIManager.GetComponent<UIManager>().CurrentCharacter)
                playerAnimator.SetFloat("speed", 0f);
        }

        // Check climbing and other mechanics
        climb();

        UpdateFollowers();
    }

    public void Move(float move, bool crouch, bool jump)
    {
        // Only control the player if grounded or airControl is turned on
        // Move the character by finding the target velocity
        Vector3 targetVelocity =
            new Vector2(move * 10f, m_Rigidbody2D.linearVelocity.y);

        // And then smoothing it out and applying it to the character
        m_Rigidbody2D.linearVelocity =
            Vector3
                .SmoothDamp(m_Rigidbody2D.linearVelocity,
                targetVelocity,
                ref m_Velocity,
                m_MovementSmoothing);

        Flip(move);

        // If the player should jump...
        if (m_Grounded && jump)
        {
            // Add a vertical force to the player.
            m_Grounded = false;
            m_Rigidbody2D.AddForce(new Vector2(0f, JumpForce));
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
            rb.linearVelocity = launchDirection * launchForce;
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
            if (InputHelper.GetKeyDown(KeyCode.E))
            {
                item.GetComponent<ItemBehaviour>().itemBehaviour();
            }

            //if key W is pressed
            if (InputHelper.GetKeyDown(KeyCode.W) && item.CompareTag("door")) goUp(item);

            //if key S is pressed
            if (InputHelper.GetKeyDown(KeyCode.S) && item.CompareTag("door")) goDown(item);
            if (item.CompareTag("ladder") && Mathf.Abs(vertical) > 0f)
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

        UIManager.GetComponent<UIManager>().PlayPortalAnimation();
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
                diffx < 20 &&
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

        if (UIManager.GetComponent<UIManager>().isSingle)
        {
            // Move only the current character
            GameObject CurrentCharacter =
                UIManager.GetComponent<UIManager>().CurrentCharacter;
            CurrentCharacter.transform.position =
                closestDoorUp.transform.position;
        }
        else
        {
            if (closestDoorUp != targetItem)
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
                    float offsetZ = 0.01f; // Adjust the spacing between followers
                    int index = 1; // Track follower index

                    foreach (GameObject follower in playerController.followers)
                    {
                        if (follower != null)
                        {
                            Vector3 newPosition =
                                closestDoorUp.transform.position;
                            newPosition.z += index * offsetZ; // Spread followers along X-axis
                            follower.transform.position = newPosition;
                            index++; // Increment index for the next follower
                        }
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
        UIManager.GetComponent<UIManager>().PlayPortalAnimation();
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
                diffx < 20 &&
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
            if (closestDoorDown != targetItem)
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
                    float offsetZ = 0.01f; // Adjust the spacing between followers
                    int index = 1; // Track follower index

                    foreach (GameObject follower in playerController.followers)
                    {
                        if (follower != null)
                        {
                            Vector3 newPosition =
                                closestDoorDown.transform.position;
                            newPosition.z += index * offsetZ; // Spread followers along X-axis
                            follower.transform.position = newPosition;
                            index++; // Increment index for the next follower
                        }
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
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, vertical * climbSpeed);
            GetComponent<Collider2D>().enabled = false;
        }
        else
        {
            rb.gravityScale = 10f;
            GetComponent<Collider2D>().enabled = true;
        }
    }

    public void Add(Item item)
    {
        Item tmpItem = item;
        Items.Add(tmpItem);
    }

    public void Remove(Item item)
    {
        Items.Remove(item);
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

    private void Flip(float moveX)
    {
        if (moveX > 0)
        {
            transform.localScale =
                new Vector3(Mathf.Abs(transform.localScale.x),
                    transform.localScale.y,
                    transform.localScale.z);
        }
        else if (moveX < 0)
        {
            transform.localScale =
                new Vector3(-Mathf.Abs(transform.localScale.x),
                    transform.localScale.y,
                    transform.localScale.z);
        }
    }

    public void FreezeCharacter()
    {
        Rigidbody2D m_Rigidbody;
        m_Rigidbody = gameObject.GetComponent<Rigidbody2D>();
        m_Rigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
        this.gameObject.GetComponent<Animator>().SetBool("Can walk", false);
        this.gameObject.GetComponent<Animator>().SetFloat("speed", 0);
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
        if (!InputState) return; // Skip during plot/dialogue when input is disabled
        // Only the current character pulls followers; followers must not pull back
        if (this.gameObject != UIManager.GetComponent<UIManager>().CurrentCharacter) return;
        if (!UIManager.GetComponent<UIManager>().isSingle)
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
                    Mathf
                        .Abs(transform.position.x -
                        follower.transform.position.x);

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
                        FlipFollower(follower);
                    }
                    else if (
                        targetX < follower.transform.position.x &&
                        follower.transform.localScale.x < 0
                    )
                    {
                        FlipFollower(follower);
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
    }

    private void FlipFollower(GameObject follower)
    {
        Vector3 scale = follower.transform.localScale;
        scale.x *= -1; // Flip the x-axis
        follower.transform.localScale = scale;
    }

    public void TakeDamage()
    {
        health -= rd.Next(10, 50);
        UIManager.GetComponent<UIManager>().UIupdate();
    }

    public void disableInput()
    {
        InputState = false;
    }

    public void enableInput()
    {
        InputState = true;
    }

    private void OnDestroy()
    {
        DestroyClickIndicator();
        if (DataManager.Instance != null)
            DataManager.Instance.Unregister(this);
    }

    public void Save(GameData data)
    {
        if (data?.characters == null) return;

        int index = Mathf.Clamp(characterIndex, 0, data.characters.Length - 1);

        CharacterSaveData charData = data.characters[index];
        Transform t = transform;
        charData.position = new float[] { t.position.x, t.position.y, t.position.z };
        charData.health = health;
        charData.starvation = Starvation;
        charData.fatigue = Fatigue;

        // 序列化角色背包物品名称
        if (Items != null && Items.Count > 0)
        {
            charData.inventoryItems = new string[Items.Count];
            for (int i = 0; i < Items.Count; i++)
            {
                charData.inventoryItems[i] = Items[i]?.ItemName ?? string.Empty;
            }
        }
        else
        {
            charData.inventoryItems = new string[0];
        }

        // struct 是值类型，需要写回数组
        data.characters[index] = charData;
    }

    public void Load(GameData data)
    {
        if (data?.characters == null) return;

        int index = Mathf.Clamp(characterIndex, 0, data.characters.Length - 1);
        CharacterSaveData charData = data.characters[index];
        if (charData.position == null || charData.position.Length < 3) return;

        // 恢复位置
        transform.position = new Vector3(
            charData.position[0],
            charData.position[1],
            charData.position[2]);

        // 恢复属性
        health = charData.health;
        Starvation = charData.starvation;
        Fatigue = charData.fatigue;

        // 恢复角色背包物品
        Items.Clear();
        if (charData.inventoryItems != null && DataManager.Instance?.ItemDatabase != null)
        {
            foreach (string itemName in charData.inventoryItems)
            {
                if (string.IsNullOrEmpty(itemName)) continue;
                Item item = DataManager.Instance.ItemDatabase.GetItemByName(itemName);
                if (item != null)
                {
                    Items.Add(item);
                }
            }
        }
    }

    // ═══════════════════════════════════════════
    // 点击指示箭头
    // ═══════════════════════════════════════════

    private void SpawnClickIndicator(Vector3 position)
    {
        if (clickIndicatorPrefab == null) return;

        DestroyClickIndicator();

        // 指示器始终与角色保持同平面
        position.y = transform.position.y;

        activeIndicator = Instantiate(clickIndicatorPrefab, position, Quaternion.identity);
        indicatorFloatCoroutine = StartCoroutine(FloatIndicator());
    }

    private void DestroyClickIndicator()
    {
        if (indicatorFloatCoroutine != null)
        {
            StopCoroutine(indicatorFloatCoroutine);
            indicatorFloatCoroutine = null;
        }
        if (indicatorFadeCoroutine != null)
        {
            StopCoroutine(indicatorFadeCoroutine);
            indicatorFadeCoroutine = null;
        }
        if (activeIndicator != null)
        {
            Destroy(activeIndicator);
            activeIndicator = null;
        }
    }

    private void FadeOutClickIndicator()
    {
        if (activeIndicator == null) return;

        // 停止浮动
        if (indicatorFloatCoroutine != null)
        {
            StopCoroutine(indicatorFloatCoroutine);
            indicatorFloatCoroutine = null;
        }
        // 停止旧的渐隐（防止重复）
        if (indicatorFadeCoroutine != null)
        {
            StopCoroutine(indicatorFadeCoroutine);
            indicatorFadeCoroutine = null;
        }

        indicatorFadeCoroutine = StartCoroutine(FadeOutIndicatorRoutine());
    }

    private IEnumerator FloatIndicator()
    {
        if (activeIndicator == null) yield break;

        float baseY = activeIndicator.transform.position.y;
        float elapsed = 0f;

        while (activeIndicator != null)
        {
            float offset = Mathf.Sin(elapsed * indicatorFloatSpeed) * indicatorFloatHeight;
            Vector3 pos = activeIndicator.transform.position;
            pos.y = baseY + offset;
            activeIndicator.transform.position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeOutIndicatorRoutine()
    {
        var sr = activeIndicator?.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float elapsed = 0f;
        Color color = sr.color;
        float startAlpha = color.a;

        while (elapsed < indicatorFadeDuration && activeIndicator != null && sr != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / indicatorFadeDuration;
            color.a = Mathf.Lerp(startAlpha, 0f, t);
            sr.color = color;
            yield return null;
        }

        DestroyClickIndicator();
    }
}
