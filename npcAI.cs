using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class npcAI : MonoBehaviour
{
    [Header("Enemy Settings")]
    public float speed = 2.0f;

    public int maxHealth = 100;

    private int currentHealth;

    [Header("NPC UI Section")]
    private GameObject targetPlayer; // Stores the nearest player

    [Header("Player Detection")]
    public float playerSearchInterval = 2.0f; // Reduce frequent player search

    private float lastPlayerSearchTime = 0f;

    void Start()
    {
        currentHealth = maxHealth;
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
        Debug.Log("NPC attacks!");
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
        Debug.Log(" died");
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
