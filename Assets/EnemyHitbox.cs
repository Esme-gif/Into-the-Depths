using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    EnemyRat enemyRatParent;

    // Start is called before the first frame update
    void Start()
    {
        enemyRatParent = GetComponentInParent<EnemyRat>();
    }

    //TODO: refactor into an event?
    private void OnCollisionEnter2D(Collision2D collision)
    {
        enemyRatParent.CollisionMovementDetection();
    }
}
