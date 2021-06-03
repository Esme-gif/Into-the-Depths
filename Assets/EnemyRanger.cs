/************************************************************************
 * Written by Nicholas Mirchandani on 5/29/2021                         *
 *                                                                      *
 * The purpose of EnemyRanger.cs is to implement the ranger enemy in    *
 * its entirety.  This includes AI (using FSM), animations, and         *
 * colliders.                                                           *
 *                                                                      *
 ************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EnemyRanger : Enemy {
    public float enemySpeed;
    public float lerpCoefficient = 0.1f;
    public bool drawGizmos = true;
    [Range(0f, 3f)] public float jitterStrength = 1f;
    [Range(1f, 10f)] public float jitterSpeed = 3f;
    private Vector2 jitter;
    private float noise;



    [Header("Stats")]
    public float health;
    public float defense;
    public float attackDamage;

    [Header("Spotting Player")]
    public float patrolSpeed;
    public float patrolRadius = 5;
    private Vector2 initialPos;
    private Vector2 nextPos;
    public float viewDistance;
    [Header("Moving Around Player")]
    public float enemyCircleDistance = 2;
    public float enemyCircleTolerance = 0.25f;
    public float minReadyToAttackTime = 2;
    public float maxReadyToAttackTime = 4;
    private bool isPreparingToAttack;
    private bool flipDirection;
    [Header("Attack")]
    public float attackTime;
    public float attackRange = 1;
    public float projDamage;
    public Projectile _projectile;
    private bool isAttacking;
    private Vector2 attackDirection;
    [Header("Burst Attack")]
    public float closeRange;
    public float burstAttackTime;
    public float burstAttackRange;
    public float attackForce;

    private Vector2 currentDir;

    private float currentSpeed;
    private float r;
    private float angle;

    [SerializeField] AnimationClip rangerAttackAnim;

    //Ensuring that enums convert cleanly to uint as expected
    enum RangerStates : uint {
        IDLE,                   // 0
        MOVE_AROUND_PLAYER,     // 1
        MOVE_TOWARDS_PLAYER,    // 2
        ATTACK_PLAYER,          // 3
        NEAR_PLAYER,            // 4    
        NUM_STATES              // 5
    }

    enum RangerActions : uint {
        SPOTS_PLAYER,           // 0
        READY_TO_ATTACK,        // 1
        IN_ATTACK_RANGE,        // 2
        ATTACK_OVER,            // 3
        PLAYER_CLOSE,           // 4
        NUM_ACTIONS             // 5
    }

    // Start is called before the first frame update
    void Start() {
        //Initialize FSM with proper initial state and transitions
        enemyBrain = new FSM((uint) RangerStates.IDLE);
        enemyBrain.addTransition((uint) RangerStates.IDLE,                (uint) RangerStates.MOVE_AROUND_PLAYER,  (uint) RangerActions.SPOTS_PLAYER);
        enemyBrain.addTransition((uint) RangerStates.MOVE_AROUND_PLAYER,  (uint) RangerStates.MOVE_TOWARDS_PLAYER, (uint) RangerActions.READY_TO_ATTACK);
        enemyBrain.addTransition((uint) RangerStates.MOVE_TOWARDS_PLAYER, (uint) RangerStates.ATTACK_PLAYER,       (uint) RangerActions.IN_ATTACK_RANGE);
        enemyBrain.addTransition((uint) RangerStates.ATTACK_PLAYER,       (uint) RangerStates.MOVE_AROUND_PLAYER,  (uint) RangerActions.ATTACK_OVER);

        //Add transitions from every state to NEAR_PLAYER on PLAYER_CLOSE
        enemyBrain.addTransition((uint) RangerStates.IDLE,                (uint)RangerStates.NEAR_PLAYER,          (uint)RangerActions.PLAYER_CLOSE);
        enemyBrain.addTransition((uint) RangerStates.MOVE_AROUND_PLAYER,  (uint)RangerStates.NEAR_PLAYER,          (uint)RangerActions.PLAYER_CLOSE);
        enemyBrain.addTransition((uint) RangerStates.MOVE_TOWARDS_PLAYER, (uint)RangerStates.NEAR_PLAYER,          (uint)RangerActions.PLAYER_CLOSE);
        enemyBrain.addTransition((uint) RangerStates.ATTACK_PLAYER,       (uint)RangerStates.NEAR_PLAYER,          (uint)RangerActions.PLAYER_CLOSE);

        enemyBrain.addTransition((uint)RangerStates.NEAR_PLAYER,          (uint)RangerStates.MOVE_AROUND_PLAYER,   (uint)RangerActions.ATTACK_OVER);

        isAttacking = false;
        isPreparingToAttack = false;

        nextPos = transform.position;
        initialPos = transform.position;
        currentSpeed = 0;
        flipDirection = false;

        InitializeEnemy();

        animator.SetBool("Moving", true); // will need to change, right now is a placeholder as there is no time when the enemy isn't moving

        attackTime = rangerAttackAnim.length;
    }

    // Update is called once per frame
    void Update() {

        //Calculate desired movement
        switch ((RangerStates) enemyBrain.currentState) {
            case RangerStates.IDLE:
                // Moves randomly around/within a confined area
                if (Vector2.Distance(transform.position, nextPos) <= 0.5) {
                    //Generates a random point within the circle via polar coordinates
                    r = Random.Range(0, patrolRadius);
                    angle = Random.Range((float)0, 2) * Mathf.PI;
                    //Shoot out a raycast to find any walls in the given direction, and scale down r accordingly to prevent any collisions
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), patrolRadius * 1.2f, wallMask);
                    if (hit) {
                        r = r * (hit.distance / (patrolRadius * 1.2f));
                    }
                    nextPos = initialPos + new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
                }
                currentDir = Vector2.Lerp(currentDir, (nextPos - (Vector2)transform.position).normalized, lerpCoefficient);
                currentSpeed = patrolSpeed;
                break;
            case RangerStates.MOVE_AROUND_PLAYER:
                // Even when moving past player, tries to keep out of player's attack range
                animator.SetBool("Moving", true);
                animator.SetBool("Idling", false);

                if (isPreparingToAttack) {
                    nextPos = (Vector2)player.transform.position + new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
                    if (Vector2.Distance(transform.position, nextPos) <= 0.5) {
                        r = Random.Range(enemyCircleDistance - enemyCircleTolerance, enemyCircleDistance + enemyCircleTolerance);
                        angle = Random.Range(0f, .25f) * Mathf.PI * (flipDirection ? -1 : 1);
                        angle += Mathf.Atan2((transform.position - player.transform.position).y, (transform.position - player.transform.position).x);
                    }
                }

                // TODO: Better Jitter
                noise = Mathf.PerlinNoise(Time.time % 1, enemyID * 100);
                jitter = noise * jitterStrength * Vector2.Perpendicular(nextPos - (Vector2)transform.position).normalized;


                currentDir = Vector2.Lerp(currentDir, ((nextPos - (Vector2)transform.position).normalized + jitter).normalized, lerpCoefficient);

                // Starts a timer with a random amount of seconds [2,4] seconds, then is "Ready to Attack"
                if (!isPreparingToAttack) {
                    StartCoroutine(PrepareToAttack());
                    r = Random.Range(enemyCircleDistance - enemyCircleTolerance, enemyCircleDistance + enemyCircleTolerance);
                    angle = Random.Range((float)0f, .25f) * Mathf.PI * (flipDirection ? -1 : 1);
                    angle += Mathf.Atan2((transform.position - player.transform.position).y, (transform.position - player.transform.position).x);
                }

                currentSpeed = enemySpeed;
                break;
            case RangerStates.MOVE_TOWARDS_PLAYER:
                // Move towards player, but still include a little bit of randomness/jitter perpendicular to the player's location to keep things interesting
                // TODO: Better Jitter
                animator.SetBool("Moving", true);
                animator.SetBool("Idling", false);
                noise = Mathf.PerlinNoise(Time.time % 1, enemyID * 100);
                jitter = noise * jitterStrength * Vector2.Perpendicular(player.transform.position - transform.position).normalized;
                currentDir = Vector2.Lerp(currentDir, ((Vector2)(player.transform.position - transform.position).normalized + jitter).normalized, lerpCoefficient);

                currentSpeed = enemySpeed;
                // When in range of attack, "Attack Player"
                if (Vector2.Distance(player.transform.position, transform.position) < attackRange) {
                    enemyBrain.applyTransition((uint)RangerActions.IN_ATTACK_RANGE);
                }
                break;
            case RangerStates.ATTACK_PLAYER:
                // TODO: Attack is a "long jump/long/slash"?  Animation will need a function to call that propels enemy forward in the direction of the player
                // (Can and should go a little past) when wind up is done
                if (!isAttacking) {
                    StartCoroutine(AttackPlayer());
                }
                break;
            case RangerStates.NEAR_PLAYER:
                // TODO: Near_Player, burst attack to push player away :)
                if(!isAttacking) {
                    StopAllCoroutines(); //Need to stop coroutines of ongoing timers, so you don't get burst attacked instant shot (Problem since being near interrupts anything else)
                    StartCoroutine(BurstAttack());
                }
                break;
        }

        // Enemies check for certain transitions not every frame for efficiency, and checks are offset based on enemyID so different enemy checks are at different frames.
        if (Time.frameCount % framesBetweenAIChecks == enemyID % framesBetweenAIChecks) {
            //Separate case statement for potentially intensive state transition checks
            //SPOTS_PLAYER code if in idle state, but always raycast to check if player is near
            if (Vector2.Distance(player.transform.position, transform.position) <= viewDistance) {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, player.transform.position - transform.position, Vector2.Distance(transform.position, player.transform.position), layerMask);
                if (hit && hit.collider.tag.Equals("playerHitbox")) {
                    if(Vector2.Distance(player.transform.position, transform.position) < closeRange) {
                        enemyBrain.applyTransition((uint)RangerActions.PLAYER_CLOSE);
                    } else if(enemyBrain.currentState == (uint)RangerStates.IDLE) {
                        enemyBrain.applyTransition((uint)RangerActions.SPOTS_PLAYER);
                    }
                    animator.SetBool("Moving", true);
                    animator.SetBool("Idling", false);
                }
            }
        }

        if (!isAttacking) {
            rb2d.velocity = currentDir * currentSpeed;
            animator.SetFloat("FaceX", currentDir.normalized.x);
        }
    }

    private IEnumerator AttackPlayer() {
        //NOTE: Simple implementation assuming attackTime is something we know.  May be complexified later, but is abstracted for that reason
        isAttacking = true;
        attackDirection = currentDir.normalized;
        animator.SetFloat("FaceX", attackDirection.x);
        animator.SetTrigger("Attack");

        // TODO: Ranged Attack Projectile (Also move this to animation event instead of here)
        Vector3 playerDir = player.transform.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(forward: Vector3.forward,
        upwards: playerDir);
        Projectile newProj = Instantiate(_projectile, transform.position, targetRotation);

        newProj.direction = playerDir;
        newProj.targetTag = "playerHitbox";
        newProj.speed = 0.01f;
        newProj.damage = projDamage;

        Debug.Log("Ranged Attack!");

        rb2d.velocity = Vector2.zero;
        yield return new WaitForSeconds(attackTime);
        enemyBrain.applyTransition((uint)RangerActions.ATTACK_OVER);
        isAttacking = false;
    }

    private IEnumerator PrepareToAttack() {
        isPreparingToAttack = true;
        float waitTime = Random.Range(minReadyToAttackTime, maxReadyToAttackTime);
        yield return new WaitForSeconds(waitTime);
        enemyBrain.applyTransition((uint)RangerActions.READY_TO_ATTACK);
        isPreparingToAttack = false;
    }

    private IEnumerator BurstAttack() {
        isAttacking = true;
        Debug.Log("Burst attack!");
        rb2d.velocity = Vector2.zero;

        // Burst Attack Pushback (Also move this to animation event instead of here)
        yield return new WaitForSeconds(burstAttackTime);
        if(Vector2.Distance(player.transform.position, transform.position) <= burstAttackRange) {
            player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            player.GetComponent<Rigidbody2D>().AddForce((player.transform.position - transform.position).normalized * attackForce);
        }
        enemyBrain.applyTransition((uint)RangerActions.ATTACK_OVER);
        isAttacking = false;
    }

    private void OnDrawGizmos() {
        //Drawing Gizmos like radius for debug purposes in editor.  Nothing here will be drawn in build :)
        if (!drawGizmos) {
            return;
        }
        //Since lots of things aren't initialized until the editor's started, need a conditional branch based on whether or not Start has been called (aka whether or not you're editing in the editor)
        if (hasStarted) {
            switch ((RangerStates)enemyBrain.currentState) {
                case RangerStates.IDLE:
                    Handles.color = new Color(0, 1f, 0f, 1);
                    Handles.DrawWireDisc(initialPos, Vector3.forward, patrolRadius);
                    Debug.DrawLine(transform.position, nextPos, Color.green);
                    Handles.color = new Color(0f, 1f, 0f, 0.25f);
                    Handles.DrawSolidDisc(nextPos, Vector3.forward, 0.25f);
                    Handles.color = new Color(1f, 1f, 0f, 0.25f);
                    Handles.DrawSolidDisc(transform.position, Vector3.forward, viewDistance);
                    Handles.color = new Color(1f, 0f, 0f, 0.25f);
                    Handles.DrawSolidDisc(transform.position, Vector3.forward, closeRange);
                    break;
                case RangerStates.MOVE_AROUND_PLAYER:
                    Handles.color = new Color(1f, 0f, 1f, 1f);
                    Handles.DrawWireDisc(player.transform.position, Vector3.forward, enemyCircleDistance + enemyCircleTolerance);
                    Handles.color = new Color(1f, 0f, 1f, 1f);
                    Handles.DrawWireDisc(player.transform.position, Vector3.forward, enemyCircleDistance - enemyCircleTolerance);
                    Debug.DrawLine(transform.position, nextPos, Color.cyan);
                    Handles.color = new Color(0f, 1f, 1f, 0.25f);
                    Handles.DrawSolidDisc(nextPos, Vector3.forward, 0.25f);
                    Handles.color = new Color(1f, 0f, 0f, 0.25f);
                    Handles.DrawSolidDisc(transform.position, Vector3.forward, closeRange);
                    break;
                case RangerStates.MOVE_TOWARDS_PLAYER:
                    Debug.DrawLine(transform.position, player.transform.position, Color.red);
                    Debug.DrawRay(transform.position, currentDir, Color.red);
                    Handles.color = new Color(1f, 0f, 0f, 0.25f);
                    Handles.DrawSolidDisc(transform.position, Vector3.forward, attackRange);
                    break;
                default:
                    Debug.DrawRay(transform.position, currentDir, Color.red);
                    break;
            }
        } else {
            //Idle Draws: Patrol Radius and View Distance
            Handles.color = new Color(0, 1f, 0f, 1);
            Handles.DrawWireDisc(transform.position, Vector3.forward, patrolRadius);
            Handles.color = new Color(1f, 1f, 0f, 0.25f);
            Handles.DrawSolidDisc(transform.position, Vector3.forward, viewDistance);

            //Circling Player Draws: Min/Max tolerance circles
            Handles.color = new Color(1f, 0f, 1f, 1f);
            Handles.DrawWireDisc(transform.position, Vector3.forward, enemyCircleDistance + enemyCircleTolerance);
            Handles.color = new Color(1f, 0f, 1f, 1f);
            Handles.DrawWireDisc(transform.position, Vector3.forward, enemyCircleDistance - enemyCircleTolerance);

            //Attack Draws: Attack Range + Close Range
            Handles.color = new Color(1f, 0f, 0f, 0.25f);
            Handles.DrawSolidDisc(transform.position, Vector3.forward, attackRange);
            Handles.DrawSolidDisc(transform.position, Vector3.forward, closeRange);

        }
    }

    //Simple Debug Colllision Code:  If IDLE and collide with something, change waypoing.  If MovingAround player and collider with something, change direction
    public override void CollisionMovementDetection() //Feel free to rename this lmao
    {
        //called by child enemyHitbox object in OnCollisionEnter
        //just. exactly what was in Nick's original OnCollisionEnter2D
        //refactor into an event? 
        switch ((RangerStates)enemyBrain.currentState) {
            case RangerStates.IDLE:
                //Generates a random point within the circle via polar coordinates
                r = Random.Range(0, patrolRadius);
                angle = Random.Range((float)0, 2) * Mathf.PI;
                //Shoot out a raycast to find any walls in the given direction, and scale down r accordingly to prevent any collisions
                RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), patrolRadius * 1.2f, wallMask);
                if (hit) {
                    r = r * (hit.distance / (patrolRadius * 1.2f));
                }
                nextPos = initialPos + new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
                break;
            case RangerStates.MOVE_AROUND_PLAYER:
                flipDirection = !flipDirection;
                r = Random.Range(enemyCircleDistance - enemyCircleTolerance, enemyCircleDistance + enemyCircleTolerance);
                angle = Random.Range((float)0f, .25f) * Mathf.PI * (flipDirection ? -1 : 1);
                angle += Mathf.Atan2((transform.position - player.transform.position).y, (transform.position - player.transform.position).x);
                break;
        }

    }

    public void TakeDamage(float amount) {
        health -= (amount - defense);
        if (health <= 0) {
            Destroy(gameObject);
        }
    }
}