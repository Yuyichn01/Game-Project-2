using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
Author: Yi Yu
Date: 2024/4/27
Description:
The script controls the movement of an enemy and auto-detects the nearest player.
*/
public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Settings")]
    public float speed = 2.0f;

    public float EnemyRange = 10.0f;

    public int maxHealth = 100;

    private int currentHealth;

    [Header("Enemy UI Section")]
    public Sprite EnemyDialogSprite;

    private GameObject targetPlayer; // Stores the nearest player

    private float attackCooldown = 1.5f;

    private float lastAttackTime;

    private Animator EnemyAnimator;

    private Vector3 lastPosition; // Tracks enemy's last position

    [Header("Player Detection")]
    private float playerSearchInterval = 0.5f; // Reduce frequent player search

    private float lastPlayerSearchTime = 0f;

    private float randomZOffset;

    void Start()
    {
        currentHealth = maxHealth;
        EnemyAnimator = GetComponent<Animator>();
        randomZOffset = Random.Range(-0.5f, 0.5f);
    }

    void Update()
    {
        // Search for players periodically instead of every frame
        if (Time.time >= lastPlayerSearchTime + playerSearchInterval)
        {
            FindNearestPlayer();
            lastPlayerSearchTime = Time.time;
        }

        if (targetPlayer != null)
        {
            Vector3 direction =
                targetPlayer.transform.position - transform.position;
            float distanceSqr = direction.sqrMagnitude; // Use sqrMagnitude for efficiency

            changeDirection(direction.x);

            if (distanceSqr < EnemyRange * EnemyRange)
            {
                // Move towards the player
                EnemyAnimator.SetBool("IsMoving", true);
                Vector3 previousPosition = transform.position;
                Vector3 targetPositionWithOffset =
                    new Vector3(targetPlayer.transform.position.x,
                        targetPlayer.transform.position.y,
                        targetPlayer.transform.position.z + randomZOffset);
                transform.position =
                    Vector3
                        .MoveTowards(transform.position,
                        targetPositionWithOffset,
                        speed * Time.deltaTime);

                // Attack if close enough
                if (
                    distanceSqr < 9.0f &&
                    Time.time > lastAttackTime + attackCooldown // 3.0f squared
                )
                {
                    EnemyAnimator.SetBool("IsMoving", false); // Stop movement before attacking
                    Attack (targetPlayer);
                    lastAttackTime = Time.time;
                }
                EnemyAnimator.SetBool("IsMoving", true);
            }
            else
            {
                EnemyAnimator.SetBool("IsMoving", false);
            }
        }
    }

    void FindNearestPlayer()
    {
        //Find all GameObjects with different tags and merge them into one array
        GameObject[] players1 = GameObject.FindGameObjectsWithTag("Player1");
        GameObject[] players2 = GameObject.FindGameObjectsWithTag("Player2");
        GameObject[] players3 = GameObject.FindGameObjectsWithTag("Player3");

        // Combine arrays into a single array
        GameObject[] players =
            players1.Concat(players2).Concat(players3).ToArray();

        float shortestDistanceSqr = Mathf.Infinity;
        GameObject nearestPlayer = null;

        foreach (GameObject player in players)
        {
            float distanceSqr =
                (player.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < shortestDistanceSqr)
            {
                shortestDistanceSqr = distanceSqr;
                nearestPlayer = player;
            }
        }

        targetPlayer = nearestPlayer;
    }

    void Attack(GameObject player)
    {
        Debug.Log("Enemy attacks!");
        PlayerController playerController =
            player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.TakeDamage();
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy died");
        Destroy (gameObject);
    }

    void changeDirection(float direction)
    {
        if (
            (direction > 0 && transform.localScale.x > 0) ||
            (direction < 0 && transform.localScale.x < 0)
        )
        {
            transform.localScale =
                new Vector3(transform.localScale.x * -1,
                    transform.localScale.y,
                    transform.localScale.z);
        }
    }
}
