/************************************************************************
 * Written by Nicholas Mirchandani on 4/25/2021                         *
 *                                                                      *
 * The purpose of EnemyRat.cs is to implement the rat enemy in its      *
 * entirety.  This includes AI (using FSM), animations, and colliders.  *
 *                                                                      *
 ************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EnemyRat : Enemy {
    public int framesBetweenAIChecks = 3;
    public float enemySpeed;
    [Header("Spotting Player")]
    public float patrolSpeed;
    public float patrolRadius = 5;
    private Vector2 initialPos;
    private Vector2 nextPos;
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
    private Vector3 currentDir;
    private Rigidbody2D rb2d;
    private int layerMask;

    private bool isAttacking;
    private bool isPreparingToAttack;
    private bool hasStarted = false; //Useful for drawGizmos

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
        nextPos = transform.position;
        initialPos = transform.position;

        //TODO: Instead of doing this do something with ReferenceManager/PlayerScript as a singleton.
        player = GameObject.FindGameObjectWithTag("Player"); //find player game object

        hasStarted = true;
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
        //Debug.DrawLine(transform.position, player.transform.position, Color.green);
        //Debug.DrawRay(transform.position, Vector2.Perpendicular(player.transform.position - transform.position), Color.red);

        switch ((RatStates)ratBrain.currentState) {
            case RatStates.IDLE:
                // TODO: Moves randomly around/within a confined area
                if(Vector2.Distance(transform.position, nextPos) <= 0.05) {
                    //Generates a random point within the circle via polar coordinates
                    float r = Random.Range(0, patrolRadius);
                    float angle = Random.Range((float) 0, 2) * Mathf.PI;
                    nextPos = initialPos + new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
                }

                //TODO: If Collision, Change Destination

                currentDir = (nextPos - (Vector2)transform.position).normalized;
                rb2d.MovePosition(transform.position + currentDir * patrolSpeed * Time.deltaTime);
                break;
            case RatStates.MOVE_AROUND_PLAYER:
                // TODO: Moves around, rather quickly, in a wide range, generally towards the player and then continuing past the player if not ready to attack.
                // Even when moving past player, tries to keep out of player's attack range

                // TODO: Add Jitter and Lerp
                Vector3 newDir = Vector2.Perpendicular(player.transform.position - transform.position).normalized;

                if(Vector2.Distance(player.transform.position, transform.position) > enemyCircleDistance + enemyCircleTolerance) {
                    newDir += (player.transform.position - transform.position).normalized;
                } else if (Vector2.Distance(player.transform.position, transform.position) < enemyCircleDistance - enemyCircleTolerance) {
                    newDir -= (player.transform.position - transform.position).normalized;
                }

                currentDir = Vector2.Lerp(currentDir, newDir, targetLerpCoefficient);

                Debug.DrawRay(transform.position, currentDir, Color.magenta);

                rb2d.MovePosition(transform.position + currentDir * enemySpeed * Time.deltaTime);

                if (!isPreparingToAttack) {
                    StartCoroutine(PrepareToAttack());
                }

                // TODO: Starts a timer with a random amount of seconds [2,4] seconds, then is "Ready to Attack"
                break;
            case RatStates.MOVE_TOWARDS_PLAYER:
                // TODO: Move towards player, but still include a little bit of randomness/jitter perpendicular to the player's location to keep things interesting

                // TODO: Add Jitter and Lerp
                currentDir = (player.transform.position - transform.position).normalized;
                rb2d.MovePosition(transform.position + currentDir * enemySpeed * Time.deltaTime);

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

    private void OnDrawGizmos() {
        //Drawing Gizmos like radius for debug purposes in editor.  Nothing here will be drawn in build :)

        //Since lots of things aren't initialized until the editor's started, need a conditional branch based on whether or not Start has been called (aka whether or not you're editing in the editor)
        if (hasStarted) {
            switch ((RatStates) ratBrain.currentState) {
                case RatStates.IDLE:
                    Handles.color = new Color(0, 1f, 0f, 1);
                    Handles.DrawWireDisc(initialPos, Vector3.forward, patrolRadius);
                    Debug.DrawLine(transform.position, nextPos, Color.green);
                    Handles.color = new Color(0f, 1f, 0f, 0.25f);
                    Handles.DrawSolidDisc(nextPos, Vector3.forward, 0.25f);
                    Handles.color = new Color(1f, 1f, 0f, 0.25f);
                    Handles.DrawSolidDisc(transform.position, Vector3.forward, viewDistance);
                    break;
                case RatStates.MOVE_TOWARDS_PLAYER:
                    Debug.DrawLine(transform.position, player.transform.position, Color.red);
                    break;
                default:
                    Debug.DrawRay(transform.position, currentDir, Color.red);
                    break;
            }
        } else {
            Handles.color = new Color(0, 1f, 0f, 1);
            Handles.DrawWireDisc(transform.position, Vector3.forward, patrolRadius);
            Handles.color = new Color(1f, 1f, 0f, 0.25f);
            Handles.DrawSolidDisc(transform.position, Vector3.forward, viewDistance);
        }
    }
}
