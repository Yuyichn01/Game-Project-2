using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // the player gameobject to assign
    public GameObject player;

    // the speed of ememy
    public float speed;

    // the distance between enemy and player
    private float distance;

    // the detect distance between ememy and player
    public float EnemyRange;

    void Update()
    {
        distance =
            Vector2.Distance(transform.position, player.transform.position);
        Vector2 direction = player.transform.position - transform.position;
        changeDirection(direction.x);
        if (distance < EnemyRange)
        {
            transform.position =
                Vector2
                    .MoveTowards(this.transform.position,
                    player.transform.position,
                    speed * Time.deltaTime);
        }
    }

    // change the face direction of player
    void changeDirection(float direction)
    {
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
}
