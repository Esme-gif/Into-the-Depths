using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    Enemy parentEnemy;

    // Start is called before the first frame update
    void Start()
    {
        parentEnemy = GetComponentInParent<Enemy>();
    }

    //TODO: refactor into an event?
    private void OnCollisionEnter2D(Collision2D collision)
    {
        parentEnemy.CollisionMovementDetection();
    }
}
