/************************************************************************
 * Written by Nicholas Mirchandani on 6/5/2021                          *
 *                                                                      *
 * The purpose of EnemySlasher.cs is to implement the slasher enemy in  *
 * its entirety.  This includes AI (using FSM), animations, and         *
 * colliders.                                                           *
 *                                                                      *
 ************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EnemyBully : Enemy {
    public float enemySpeed;
    public float lerpCoefficient = 0.1f;
    public bool drawGizmos = true;
    [Range(0f, 3f)] public float jitterStrength = 1f;
    [Range(1f, 10f)] public float jitterSpeed = 3f;
    private Vector2 jitter;
    private float noise;

    [Header("Spotting Player")]
    public float patrolSpeed;
    public float patrolRadius = 5;
    private Vector2 initialPos;
    private Vector2 nextPos;
    public float viewDistance;
    [Header("Preparing To Attack")]
    public float minReadyToAttackTime = 2;
    public float maxReadyToAttackTime = 4;
    private bool isPreparingToAttack;
    [Header("Moving Around Player")]
    public float enemyCircleDistance = 2;
    public float enemyCircleTolerance = 0.25f;
    private bool flipDirection;
    [Header("Attack")]
    public float strikeTime;
    public float chargeTime;
    public float nearAttackRange = 1;
    public float farAttackRange = 2;
    public float attackForce;
    private bool isAttacking;
    private Vector2 attackDirection;

    private Vector2 currentDir;

    private float currentSpeed;
    private float r;
    private float angle;

    [SerializeField] AnimationClip bullyStrikeAnim;
    [SerializeField] AnimationClip bullyChargeAnim;

    private List<GameObject> hitGOs = new List<GameObject>(); //a list of game objects the enemy has hit in one strike. used to check for double hits. 

    //Ensuring that enums convert cleanly to uint as expected
    enum BullyStates : uint {
        IDLE,                   // 0
        MOVE_AROUND_PLAYER,     // 1
        IN_EITHER_RANGE,        // 2
        READY_TO_ATTACK,        // 3
        STRIKE_ATTACK,          // 4
        CHARGE_ATTACK,          // 5
        STAGGER,                // 6
        NUM_STATES              // 7
    }

    enum BullyActions : uint {
        SPOTS_PLAYER,           // 0
        NEAR_RANGE,             // 1
        FAR_RANGE,              // 2
        READY_TO_ATTACK,        // 3
        ATTACK_OVER,            // 4
        STAGGER,                // 5
        EXIT_STAGGER,           // 6
        NUM_ACTIONS             // 7
    }

    // Start is called before the first frame update
    void Start() {

        //Initialize FSM with proper initial state and transitions
        enemyBrain = new FSM((uint)BullyStates.IDLE);
        enemyBrain.addTransition((uint)BullyStates.IDLE,               (uint)BullyStates.MOVE_AROUND_PLAYER, (uint)BullyActions.SPOTS_PLAYER);
        enemyBrain.addTransition((uint)BullyStates.MOVE_AROUND_PLAYER, (uint)BullyStates.READY_TO_ATTACK,    (uint)BullyActions.READY_TO_ATTACK);
        enemyBrain.addTransition((uint)BullyStates.READY_TO_ATTACK,    (uint)BullyStates.STRIKE_ATTACK,      (uint)BullyActions.NEAR_RANGE);
        enemyBrain.addTransition((uint)BullyStates.READY_TO_ATTACK,    (uint)BullyStates.CHARGE_ATTACK,      (uint)BullyActions.FAR_RANGE);
        enemyBrain.addTransition((uint)BullyStates.CHARGE_ATTACK,      (uint)BullyStates.MOVE_AROUND_PLAYER, (uint)BullyActions.ATTACK_OVER);
        enemyBrain.addTransition((uint)BullyStates.STRIKE_ATTACK,      (uint)BullyStates.MOVE_AROUND_PLAYER, (uint)BullyActions.ATTACK_OVER);
        enemyBrain.addTransition((uint)BullyStates.STAGGER,            (uint)BullyStates.MOVE_AROUND_PLAYER, (uint)BullyActions.EXIT_STAGGER);
        enemyBrain.addTransition((uint)BullyStates.STAGGER,            (uint)BullyActions.STAGGER);

        isAttacking = false;
        isPreparingToAttack = false;

        nextPos = transform.position;
        initialPos = transform.position;
        currentSpeed = 0;

        InitializeEnemy();

        animator.SetBool("Moving", true); // will need to change, right now is a placeholder as there is no time when the enemy isn't moving

        strikeTime = bullyStrikeAnim.length;
        chargeTime = bullyChargeAnim.length;
    }

    // Update is called once per frame
    void Update() {
        //Calculate desired movement
        switch ((BullyStates)enemyBrain.currentState) {
            case BullyStates.IDLE:
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
            case BullyStates.MOVE_AROUND_PLAYER: // TODO: MOVE_AROUND_PLAYER
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
            case BullyStates.READY_TO_ATTACK:
                // Apply transition dependent on if player is close or far
                if (Vector2.Distance(player.transform.position, transform.position) < nearAttackRange) {
                    enemyBrain.applyTransition((uint)BullyActions.NEAR_RANGE);
                } else {
                    enemyBrain.applyTransition((uint)BullyActions.FAR_RANGE);
                }
                break;
            case BullyStates.STRIKE_ATTACK:
                // TODO: Attack is a "long jump/long/slash"?  Animation will need a function to call that propels enemy forward in the direction of the player
                // (Can and should go a little past) when wind up is done
                if (!isAttacking) {
                    StartCoroutine(Strike());
                }
                break;
            case BullyStates.CHARGE_ATTACK:
                if (!isAttacking) {
                    StartCoroutine(Charge());
                }
                break;
            case BullyStates.STAGGER:
                currentSpeed = 0;
                break;
        }

        // Enemies check for certain transitions not every frame for efficiency, and checks are offset based on enemyID so different enemy checks are at different frames.
        if (Time.frameCount % framesBetweenAIChecks == enemyID % framesBetweenAIChecks) {
            //Separate case statement for potentially intensive state transition checks
            switch ((BullyStates)enemyBrain.currentState) {
                case BullyStates.IDLE:
                    //SPOTS_PLAYER code if in idle state: If player is within range, raycast to check if you see them
                    if (Vector2.Distance(player.transform.position, transform.position) <= viewDistance) {
                        RaycastHit2D hit = Physics2D.Raycast(transform.position, player.transform.position - transform.position, Vector2.Distance(transform.position, player.transform.position), layerMask);
                        if (hit && hit.collider.tag.Equals("playerHitbox")) {
                            enemyBrain.applyTransition((uint)BullyActions.SPOTS_PLAYER);
                            animator.SetBool("Moving", true);
                            animator.SetBool("Idling", false);
                        }
                    }
                    break;
                default:
                    break;
            }

        }
        if (!isAttacking) {
            rb2d.velocity = currentDir * currentSpeed;
            animator.SetFloat("FaceX", currentDir.normalized.x);
        }
    }

    private IEnumerator Strike() {
        //NOTE: Simple implementation assuming attackTime is something we know.  May be complexified later, but is abstracted for that reason
        isAttacking = true;
        attackDirection = currentDir.normalized;
        animator.SetFloat("FaceX", attackDirection.x);
        animator.SetTrigger("Attack"); // TODO: Change this to be custom based on new animator
        rb2d.velocity = Vector2.zero;
        yield return new WaitForSeconds(strikeTime);

        enemyBrain.applyTransition((uint)BullyActions.ATTACK_OVER);

        isAttacking = false;
    }

    private IEnumerator Charge() {
        //NOTE: Simple implementation assuming attackTime is something we know.  May be complexified later, but is abstracted for that reason
        isAttacking = true;
        attackDirection = currentDir.normalized;
        animator.SetFloat("FaceX", attackDirection.x);
        animator.SetTrigger("Attack"); // TODO: Change this to be custom based on new animator
        rb2d.velocity = Vector2.zero;
        yield return new WaitForSeconds(chargeTime);
        enemyBrain.applyTransition((uint)BullyActions.ATTACK_OVER);
        isAttacking = false;
    }

    private IEnumerator PrepareToAttack() {
        isPreparingToAttack = true;
        float waitTime = Random.Range(minReadyToAttackTime, maxReadyToAttackTime);
        yield return new WaitForSeconds(waitTime);
        enemyBrain.applyTransition((uint)BullyActions.READY_TO_ATTACK);
        isPreparingToAttack = false;
    }

    // called by the attack animation when wind up is done
    public void AddAttackForce(AnimationEvent evt) {
        //need to check the weight, otherwise mecanim calls all animation events on all anims in the blend tree at once. 
        if (evt.animatorClipInfo.weight > 0.5) {
            rb2d.AddForce((player.transform.position - transform.position).normalized * attackForce);
            animator.SetFloat("FaceX", (player.transform.position - transform.position).normalized.x);
        }
    }

    private void OnDrawGizmos() {
        //Drawing Gizmos like radius for debug purposes in editor.  Nothing here will be drawn in build :)
        if (!drawGizmos) {
            return;
        }
        //Since lots of things aren't initialized until the editor's started, need a conditional branch based on whether or not Start has been called (aka whether or not you're editing in the editor)
        if (hasStarted) {
            switch ((BullyStates)enemyBrain.currentState) {
                case BullyStates.IDLE:
                    Handles.color = new Color(0, 1f, 0f, 1);
                    Handles.DrawWireDisc(initialPos, Vector3.forward, patrolRadius);
                    Debug.DrawLine(transform.position, nextPos, Color.green);
                    Handles.color = new Color(0f, 1f, 0f, 0.25f);
                    Handles.DrawSolidDisc(nextPos, Vector3.forward, 0.25f);
                    Handles.color = new Color(1f, 1f, 0f, 0.25f);
                    Handles.DrawSolidDisc(transform.position, Vector3.forward, viewDistance);
                    break;
                case BullyStates.MOVE_AROUND_PLAYER:
                    Handles.color = new Color(1f, 0f, 1f, 1f);
                    Handles.DrawWireDisc(player.transform.position, Vector3.forward, enemyCircleDistance + enemyCircleTolerance);
                    Handles.color = new Color(1f, 0f, 1f, 1f);
                    Handles.DrawWireDisc(player.transform.position, Vector3.forward, enemyCircleDistance - enemyCircleTolerance);
                    Debug.DrawLine(transform.position, nextPos, Color.cyan);
                    Handles.color = new Color(0f, 1f, 1f, 0.25f);
                    Handles.DrawSolidDisc(nextPos, Vector3.forward, 0.25f);
                    Handles.color = new Color(1f, 0f, 0f, 0.25f);
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

            //Attack Draws: Attack Range
            Handles.color = new Color(1f, 0f, 0f, 0.25f);
            Handles.DrawSolidDisc(transform.position, Vector3.forward, nearAttackRange);
            Handles.DrawSolidDisc(transform.position, Vector3.forward, farAttackRange);
        }
    }

    //Simple Debug Colllision Code:  If IDLE and collide with something, change waypoing.  If MovingAround player and collider with something, change direction
    public override void CollisionMovementDetection() //Feel free to rename this lmao
    {
        //called by child enemyHitbox object in OnCollisionEnter
        //just. exactly what was in Nick's original OnCollisionEnter2D
        //refactor into an event? 
        switch ((BullyStates)enemyBrain.currentState) {
            case BullyStates.IDLE:
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
        }

    }

    // need to figure something out with colliders to refactor with Nick 
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.tag == "playerHitbox") {
            if (!hitGOs.Contains(collision.gameObject)) {
                Debug.Log("Hulk hit the player!");
                //at the moment, the only gameobject the enemy should ever hit will be the player, however
                //i want this to be expandable in case we have a "two player" system in the final boss battle or there are other edge cases
                hitGOs.Add(collision.gameObject);
                player.GetComponent<PlayerScript>().ChangePlayerHealth(-attackDamage, "hit");
                //play player taken damage animation? 
            }
        }
    }

    //called by attack animation event 
    public void RemoveFromHitGOs() {
        hitGOs.Clear();
    }

    protected override IEnumerator Stagger() {
        enemyBrain.applyTransition((uint)BullyActions.STAGGER);
        yield return new WaitForSeconds(staggerTime);
        enemyBrain.applyTransition((uint)BullyActions.EXIT_STAGGER);
        Debug.Log("Stagger Over!");
    }

}