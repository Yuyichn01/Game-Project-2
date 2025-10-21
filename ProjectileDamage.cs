using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    public int damage = 10; // Damage dealt to the enemy

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the projectile hit an enemy
        if (collision.CompareTag("Enemy"))
        {
            // Access the enemy's health component and apply damage
            EnemyAI enemyHealth = collision.GetComponent<EnemyAI>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage (damage);
                Debug.Log("Enemy is being hit!");
            }

            // Destroy the projectile after it hits
            Destroy (gameObject);
        }
    }
}
