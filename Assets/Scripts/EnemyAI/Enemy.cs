/************************************************************************
 * Written by Nicholas Mirchandani on 4/25/2021                         *
 *                                                                      *
 * The purpose of Enemy.cs is to provide a parent class for all         *
 * enemies so that in the future, when enemies need to be aggregated,   *
 * they can be stored in a List<Enemy> or similar data structure.       *
 *                                                                      *
 ************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {
    public int framesBetweenAIChecks = 3;
    [SerializeField] protected FSM enemyBrain;
    protected static int curID = 0;
    protected int enemyID;

    protected int layerMask;
    protected int wallMask;
    protected int enemyMask;

    protected Rigidbody2D rb2d;
    protected Animator animator;
    protected GameObject player;

    protected bool hasStarted = false; //Useful for drawGizmos

    [Header("Stats")]
    public float health;
    public float defense;
    public float attackDamage;
    public float staggerTime;

    //A method for initializations that don't need to clutter up the individual enemy implementations
    protected void InitializeEnemy() {
        //Set Enemy ID
        enemyID = curID;
        curID += 1;

        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        layerMask = LayerMask.GetMask("Hitbox", "Map");
        wallMask = LayerMask.GetMask("Map");
        enemyMask = LayerMask.GetMask("Enemies");

        hasStarted = true;

        //TODO: Instead of doing this do something with ReferenceManager/PlayerScript as a singleton.
        player = GameObject.FindGameObjectWithTag("Player"); //find player game object
    }

    public virtual void CollisionMovementDetection() { }

    public void TakeDamage(float amount)
    {
        health -= (amount - defense);
        if(amount > 0)
        {
            StopAllCoroutines(); // Interrupting anything and everything with the stagger call
            StartCoroutine(Stagger());
            animator.SetTrigger("Stagger");
            //needs to stop movement 
        }
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    protected virtual IEnumerator Stagger() {
        Debug.Log("Unimplemented Stagger!");
        yield return null;
    }
}
