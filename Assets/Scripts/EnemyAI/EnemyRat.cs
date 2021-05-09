﻿/************************************************************************
 * Written by Nicholas Mirchandani on 4/25/2021                         *
 *                                                                      *
 * The purpose of EnemyRat.cs is to implement the rat enemy in its      *
 * entirety.  This includes AI (using FSM), animations, and colliders.  *
 *                                                                      *
 ************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRat : Enemy {
    public int framesBetweenAIChecks = 3;
    public float enemySpeed;
    [Header("Spotting Player")]
    public float viewDistance;
    [Header("Moving Around Player")]
    public float enemyCircleDistance = 2;
    public float enemyCircleTolerance = 0.25f;
    public float targetLerpCoefficient = 0.1f;
    public float minReadyToAttackTime = 2;
    public float maxReadyToAttackTime = 4;
    [Header("Attack")]
    public float attackTime = 3;

    //NOTE: FSM is just public for debug
    public FSM ratBrain;
    static int curID = 0;
    private int enemyID;
    private GameObject player;
    private Vector3 target;
    private Rigidbody2D rb2d;
    private int layerMask;

    private bool isAttacking;
    private bool isPreparingToAttack;

    //Ensuring that enums convert cleanly to uint as expected
    enum RatStates : uint {
        IDLE,                   // 0
        MOVE_AROUND_PLAYER,     // 1
        MOVE_TOWARDS_PLAYER,    // 2
        ATTACK_PLAYER,          // 3
        MOVE_PAST_PLAYER,       // 4
        NUM_STATES              // 5
    }

    enum RatActions : uint {
        SPOTS_PLAYER,           // 0
        READY_TO_ATTACK,        // 1
        IN_ATTACK_RANGE,        // 2
        ATTACK_OVER,            // 3
        STOP_MOVE_PAST,         // 4
        NUM_ACTIONS             // 5
    }

    // Start is called before the first frame update
    void Start() {
        //Initialize FSM with proper initial state and transitions
        ratBrain = new FSM((uint) RatStates.IDLE);
        ratBrain.addTransition((uint) RatStates.IDLE,                (uint) RatStates.MOVE_AROUND_PLAYER,  (uint) RatActions.SPOTS_PLAYER);
        ratBrain.addTransition((uint) RatStates.MOVE_AROUND_PLAYER,  (uint) RatStates.MOVE_TOWARDS_PLAYER, (uint) RatActions.READY_TO_ATTACK);
        ratBrain.addTransition((uint) RatStates.MOVE_TOWARDS_PLAYER, (uint) RatStates.ATTACK_PLAYER,       (uint) RatActions.IN_ATTACK_RANGE);
        ratBrain.addTransition((uint) RatStates.ATTACK_PLAYER,       (uint) RatStates.MOVE_PAST_PLAYER,    (uint) RatActions.ATTACK_OVER);

        //Set Enemy ID
        enemyID = curID;
        curID += 1;

        rb2d = GetComponent<Rigidbody2D>();
        layerMask = LayerMask.GetMask("Hitbox", "Map");
        isAttacking = false;

        //TODO: Instead of doing this do something with ReferenceManager/PlayerScript as a singleton.
        player = GameObject.FindGameObjectWithTag("Player"); //find player game object
    }

    // Update is called once per frame
    void Update() {
        // Enemies check for transitions not every frame for efficiency, and checks are offset based on enemyID so different enemy checks are at different frames.
        // May end up removing this functionality depending on if it causes implementation difficulties.
        if(Time.frameCount % framesBetweenAIChecks == enemyID % framesBetweenAIChecks) {
            //Separate case statement for potentially intensive state transition checks
            switch((RatStates)ratBrain.currentState) {
                case RatStates.IDLE:
                    //SPOTS_PLAYER code if in idle state
                    //If player is within range, raycast to check
                    if(Vector2.Distance(player.transform.position, transform.position) <= viewDistance) {
                        //Raycast
                        RaycastHit2D hit = Physics2D.Raycast(transform.position, player.transform.position - transform.position, Vector2.Distance(transform.position, player.transform.position), layerMask);
                        if (hit) {
                            Debug.Log("HIT: " + hit.collider.tag);
                        }
                        if (hit && hit.collider.tag.Equals("playerHitbox")) {
                            //The player is visible
                            ratBrain.applyTransition((uint) RatActions.SPOTS_PLAYER);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        //Debug Draws; Green -> Player.  Red -> Perpendicular to player (for horizontal jitter + moving in a circle)
        Debug.DrawLine(transform.position, player.transform.position, Color.green);
        Debug.DrawRay(transform.position, Vector2.Perpendicular(player.transform.position - transform.position), Color.red);

        switch ((RatStates)ratBrain.currentState) {
            case RatStates.IDLE:
                // TODO: Moves randomly around/within a confined area

                // TODO: When player enters vision, "Spots Player"
                break;
            case RatStates.MOVE_AROUND_PLAYER:
                // TODO: Moves around, rather quickly, in a wide range, generally towards the player and then continuing past the player if not ready to attack.
                // Even when moving past player, tries to keep out of player's attack range

                // TODO: Add Jitter and Lerp
                Vector3 newTarget = Vector2.Perpendicular(player.transform.position - transform.position).normalized;

                if(Vector2.Distance(player.transform.position, transform.position) > enemyCircleDistance + enemyCircleTolerance) {
                    newTarget += (player.transform.position - transform.position).normalized;
                } else if (Vector2.Distance(player.transform.position, transform.position) < enemyCircleDistance - enemyCircleTolerance) {
                    newTarget -= (player.transform.position - transform.position).normalized;
                }

                target = Vector2.Lerp(target, newTarget, targetLerpCoefficient);

                Debug.DrawRay(transform.position, target, Color.magenta);

                rb2d.MovePosition(transform.position + target * enemySpeed * Time.deltaTime);

                if (!isPreparingToAttack) {
                    StartCoroutine(PrepareToAttack());
                }

                // TODO: Starts a timer with a random amount of seconds [2,4] seconds, then is "Ready to Attack"
                break;
            case RatStates.MOVE_TOWARDS_PLAYER:
                // TODO: Move towards player, but still include a little bit of randomness/jitter perpendicular to the player's location to keep things interesting

                // TODO: Add Jitter and Lerp
                target = (player.transform.position - transform.position).normalized;
                rb2d.MovePosition(transform.position + target * enemySpeed * Time.deltaTime);

                // TODO: When in range of attack, "Attack Player"
                break;
            case RatStates.ATTACK_PLAYER:
                // TODO: Attack is a "long jump/long/slash"?  Animation will need a function to call that propels enemy forward in the direction of the player
                // (Can and should go a little past) when wind up is done
                if(!isAttacking) {
                    StartCoroutine(AttackPlayer());
                }
                // TODO: When attack is over, "Attack Over"
                break;
            case RatStates.MOVE_PAST_PLAYER:
                // TODO: Completes momentum of attack and then continues forward a random amount within a range

                // TODO: When random amount within the range has been reached, "Stop Move Past"
                break;
        }
    }

    private IEnumerator AttackPlayer() {
        //NOTE: Simple implementation assuming attackTime is something we know.  May be complexified later, but is abstracted for that reason
        isAttacking = true;
        yield return new WaitForSeconds(attackTime);
        ratBrain.applyTransition((uint)RatActions.ATTACK_OVER);
        isAttacking = false;
    }

    private IEnumerator PrepareToAttack() {
        isPreparingToAttack = true;
        float waitTime = Random.Range(minReadyToAttackTime, maxReadyToAttackTime);
        Debug.Log("Prepare to attack START!");
        yield return new WaitForSeconds(waitTime);
        Debug.Log("Ready to attack");
        ratBrain.applyTransition((uint)RatActions.READY_TO_ATTACK);
        isPreparingToAttack = false;
    }
}
