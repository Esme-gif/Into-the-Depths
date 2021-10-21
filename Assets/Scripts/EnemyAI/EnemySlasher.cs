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
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemySlasher : Enemy {
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
    [Header("Attack")]
    public float attack1Time;
    public float attack2Time;
    public float attack3Time;
    public float slashTime;
    public float nearAttackRange = 1;
    public float farAttackRange = 2;
    public float attackForce;
    private bool isAttacking;
    private Vector2 attackDirection;

    private Vector2 currentDir;

    private float currentSpeed;
    private float r;
    private float angle;

    [SerializeField] AnimationClip slasherAttack1Anim;
    [SerializeField] AnimationClip slasherAttack2Anim;
    [SerializeField] AnimationClip slasherAttack3Anim;
    [SerializeField] AnimationClip slasherSlashAnim;

    private List<GameObject> hitGOs = new List<GameObject>(); //a list of game objects the enemy has hit in one strike. used to check for double hits. 

    //Ensuring that enums convert cleanly to uint as expected
    enum SlasherStates : uint {
        IDLE,                   // 0
        MOVE_TOWARDS_PLAYER,    // 1
        IN_EITHER_RANGE,        // 2
        READY_TO_ATTACK,        // 3
        ATTACK_1,               // 4
        ATTACK_2,               // 5
        ATTACK_3,               // 6
        SLASH_ATTACK,           // 7
        STAGGER,                // 8
        NUM_STATES              // 9
    }

    enum SlasherActions : uint {
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
        enemyBrain = new FSM((uint)SlasherStates.IDLE);
        enemyBrain.addTransition((uint)SlasherStates.IDLE,                (uint)SlasherStates.MOVE_TOWARDS_PLAYER, (uint)SlasherActions.SPOTS_PLAYER);
        enemyBrain.addTransition((uint)SlasherStates.MOVE_TOWARDS_PLAYER, (uint)SlasherStates.IN_EITHER_RANGE,     (uint)SlasherActions.FAR_RANGE);
        enemyBrain.addTransition((uint)SlasherStates.MOVE_TOWARDS_PLAYER, (uint)SlasherStates.IN_EITHER_RANGE,     (uint)SlasherActions.NEAR_RANGE);
        enemyBrain.addTransition((uint)SlasherStates.IN_EITHER_RANGE,     (uint)SlasherStates.READY_TO_ATTACK,     (uint)SlasherActions.READY_TO_ATTACK);
        enemyBrain.addTransition((uint)SlasherStates.READY_TO_ATTACK,     (uint)SlasherStates.ATTACK_1,            (uint)SlasherActions.NEAR_RANGE);
        enemyBrain.addTransition((uint)SlasherStates.READY_TO_ATTACK,     (uint)SlasherStates.SLASH_ATTACK,        (uint)SlasherActions.FAR_RANGE);
        enemyBrain.addTransition((uint)SlasherStates.SLASH_ATTACK,        (uint)SlasherStates.MOVE_TOWARDS_PLAYER, (uint)SlasherActions.ATTACK_OVER);
        enemyBrain.addTransition((uint)SlasherStates.SLASH_ATTACK,        (uint)SlasherStates.ATTACK_1,            (uint)SlasherActions.NEAR_RANGE);
        enemyBrain.addTransition((uint)SlasherStates.ATTACK_1,            (uint)SlasherStates.MOVE_TOWARDS_PLAYER, (uint)SlasherActions.ATTACK_OVER);
        enemyBrain.addTransition((uint)SlasherStates.ATTACK_1,            (uint)SlasherStates.ATTACK_2,            (uint)SlasherActions.NEAR_RANGE);
        enemyBrain.addTransition((uint)SlasherStates.ATTACK_2,            (uint)SlasherStates.MOVE_TOWARDS_PLAYER, (uint)SlasherActions.ATTACK_OVER);
        enemyBrain.addTransition((uint)SlasherStates.ATTACK_2,            (uint)SlasherStates.ATTACK_3,            (uint)SlasherActions.NEAR_RANGE);
        enemyBrain.addTransition((uint)SlasherStates.ATTACK_3,            (uint)SlasherStates.MOVE_TOWARDS_PLAYER, (uint)SlasherActions.ATTACK_OVER);
        enemyBrain.addTransition((uint)SlasherStates.STAGGER,             (uint)SlasherStates.MOVE_TOWARDS_PLAYER, (uint)SlasherActions.EXIT_STAGGER);
        enemyBrain.addTransition((uint)SlasherStates.STAGGER,             (uint)SlasherActions.STAGGER);

        isAttacking = false;
        isPreparingToAttack = false;

        nextPos = transform.position;
        initialPos = transform.position;
        currentSpeed = 0;

        InitializeEnemy();

        animator.SetBool("Moving", true); // will need to change, right now is a placeholder as there is no time when the enemy isn't moving

        attack1Time = slasherAttack1Anim.length;
        attack2Time = slasherAttack2Anim.length;
        attack3Time = slasherAttack3Anim.length;
        slashTime = slasherSlashAnim.length;
    }

    // Update is called once per frame
    void Update() {
        //Calculate desired movement
        switch ((SlasherStates)enemyBrain.currentState) {
            case SlasherStates.IDLE:
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
            case SlasherStates.MOVE_TOWARDS_PLAYER:
                // Move towards player, but still include a little bit of randomness/jitter perpendicular to the player's location to keep things interesting
                // TODO: Better Jitter
                animator.SetBool("Moving", true);
                animator.SetBool("Idling", false);
                noise = Mathf.PerlinNoise(Time.time % 1, enemyID * 100);
                jitter = noise * jitterStrength * Vector2.Perpendicular(player.transform.position - transform.position).normalized;
                currentDir = Vector2.Lerp(currentDir, ((Vector2)(player.transform.position - transform.position).normalized + jitter).normalized, lerpCoefficient);

                currentSpeed = enemySpeed;
                // When in range of attack, "Attack Player"
                if (Vector2.Distance(player.transform.position, transform.position) < nearAttackRange) {
                    enemyBrain.applyTransition((uint)SlasherActions.NEAR_RANGE);
                } else if (Vector2.Distance(player.transform.position, transform.position) < farAttackRange) {
                    enemyBrain.applyTransition((uint)SlasherActions.FAR_RANGE);
                }
                break;

            case SlasherStates.IN_EITHER_RANGE:
                currentSpeed = 0f;
                if (!isPreparingToAttack) {
                    StartCoroutine(PrepareToAttack());
                }
                break;
            case SlasherStates.READY_TO_ATTACK:
                // Apply transition dependent on if player is close or far
                if (Vector2.Distance(player.transform.position, transform.position) < nearAttackRange) {
                    enemyBrain.applyTransition((uint)SlasherActions.NEAR_RANGE);
                } else {
                    enemyBrain.applyTransition((uint)SlasherActions.FAR_RANGE);
                }
                break;
            case SlasherStates.ATTACK_1:
                // TODO: Attack is a "long jump/long/slash"?  Animation will need a function to call that propels enemy forward in the direction of the player
                // (Can and should go a little past) when wind up is done
                if (!isAttacking) {
                    StartCoroutine(Attack1());
                }
                break;
            case SlasherStates.ATTACK_2:
                if (!isAttacking) {
                    StartCoroutine(Attack2());
                }
                break;
            case SlasherStates.ATTACK_3:
                if (!isAttacking) {
                    StartCoroutine(Attack3());
                }
                break;
            case SlasherStates.SLASH_ATTACK:
                if (!isAttacking) {
                    StartCoroutine(Slash());
                }
                break;
            case SlasherStates.STAGGER:
                currentSpeed = 0;
                break;
                // TODO: Implement other attacks, not just attack_1 lol
        }

        // Enemies check for certain transitions not every frame for efficiency, and checks are offset based on enemyID so different enemy checks are at different frames.
        if (Time.frameCount % framesBetweenAIChecks == enemyID % framesBetweenAIChecks) {
            //Separate case statement for potentially intensive state transition checks
            switch ((SlasherStates)enemyBrain.currentState) {
                case SlasherStates.IDLE:
                    //SPOTS_PLAYER code if in idle state: If player is within range, raycast to check if you see them
                    if (Vector2.Distance(player.transform.position, transform.position) <= viewDistance) {
                        RaycastHit2D hit = Physics2D.Raycast(transform.position, player.transform.position - transform.position, Vector2.Distance(transform.position, player.transform.position), layerMask);
                        if (hit && hit.collider.tag.Equals("playerHitbox")) {
                            enemyBrain.applyTransition((uint)SlasherActions.SPOTS_PLAYER);
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

    private IEnumerator Attack1() {
        //NOTE: Simple implementation assuming attackTime is something we know.  May be complexified later, but is abstracted for that reason
        isAttacking = true;
        attackDirection = currentDir.normalized;
        animator.SetFloat("FaceX", attackDirection.x);
        animator.SetTrigger("Attack"); // TODO: Change this to be custom based on new animator
        rb2d.velocity = Vector2.zero;
        yield return new WaitForSeconds(attack1Time);

        // Apply transition when in range
        if (Vector2.Distance(player.transform.position, transform.position) < nearAttackRange) {
            enemyBrain.applyTransition((uint)SlasherActions.NEAR_RANGE);
        } else {
            enemyBrain.applyTransition((uint)SlasherActions.ATTACK_OVER);
        }

        isAttacking = false;
    }

    private IEnumerator Attack2() {
        //NOTE: Simple implementation assuming attackTime is something we know.  May be complexified later, but is abstracted for that reason
        isAttacking = true;
        attackDirection = currentDir.normalized;
        animator.SetFloat("FaceX", attackDirection.x);
        animator.SetTrigger("Attack"); // TODO: Change this to be custom based on new animator
        rb2d.velocity = Vector2.zero;
        yield return new WaitForSeconds(attack2Time);
        // Apply transition when in range
        if (Vector2.Distance(player.transform.position, transform.position) < nearAttackRange) {
            enemyBrain.applyTransition((uint)SlasherActions.NEAR_RANGE);
        } else {
            enemyBrain.applyTransition((uint)SlasherActions.ATTACK_OVER);
        }
        isAttacking = false;
    }

    private IEnumerator Attack3() {
        //NOTE: Simple implementation assuming attackTime is something we know.  May be complexified later, but is abstracted for that reason
        isAttacking = true;
        attackDirection = currentDir.normalized;
        animator.SetFloat("FaceX", attackDirection.x);
        animator.SetTrigger("Attack"); // TODO: Change this to be custom based on new animator
        rb2d.velocity = Vector2.zero;
        yield return new WaitForSeconds(attack3Time);
        enemyBrain.applyTransition((uint)SlasherActions.ATTACK_OVER);
        isAttacking = false;
    }

    private IEnumerator Slash() {
        //NOTE: Simple implementation assuming attackTime is something we know.  May be complexified later, but is abstracted for that reason
        isAttacking = true;
        attackDirection = currentDir.normalized;
        animator.SetFloat("FaceX", attackDirection.x);
        animator.SetTrigger("Attack"); // TODO: Change this to be custom based on new animator
        rb2d.velocity = Vector2.zero;
        yield return new WaitForSeconds(slashTime);
        // Apply transition when in range
        if (Vector2.Distance(player.transform.position, transform.position) < nearAttackRange) {
            enemyBrain.applyTransition((uint)SlasherActions.NEAR_RANGE);
        } else {
            enemyBrain.applyTransition((uint)SlasherActions.ATTACK_OVER);
        }
        isAttacking = false;
    }

    private IEnumerator PrepareToAttack() {
        isPreparingToAttack = true;
        float waitTime = Random.Range(minReadyToAttackTime, maxReadyToAttackTime);
        yield return new WaitForSeconds(waitTime);
        enemyBrain.applyTransition((uint)SlasherActions.READY_TO_ATTACK);
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

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        //Drawing Gizmos like radius for debug purposes in editor.  Nothing here will be drawn in build :)
        if (!drawGizmos) {
            return;
        }
        //Since lots of things aren't initialized until the editor's started, need a conditional branch based on whether or not Start has been called (aka whether or not you're editing in the editor)
        if (hasStarted) {
            switch ((SlasherStates)enemyBrain.currentState) {
                case SlasherStates.IDLE:
                    Handles.color = new Color(0, 1f, 0f, 1);
                    Handles.DrawWireDisc(initialPos, Vector3.forward, patrolRadius);
                    Debug.DrawLine(transform.position, nextPos, Color.green);
                    Handles.color = new Color(0f, 1f, 0f, 0.25f);
                    Handles.DrawSolidDisc(nextPos, Vector3.forward, 0.25f);
                    Handles.color = new Color(1f, 1f, 0f, 0.25f);
                    Handles.DrawSolidDisc(transform.position, Vector3.forward, viewDistance);
                    break;
                case SlasherStates.MOVE_TOWARDS_PLAYER:
                    Debug.DrawLine(transform.position, player.transform.position, Color.red);
                    Debug.DrawRay(transform.position, currentDir, Color.red);
                    Handles.color = new Color(1f, 0f, 0f, 0.25f);
                    Handles.DrawSolidDisc(transform.position, Vector3.forward, nearAttackRange);
                    Handles.color = new Color(1f, 0f, 0f, 0.25f);
                    Handles.DrawSolidDisc(transform.position, Vector3.forward, farAttackRange);
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

            //Attack Draws: Attack Range
            Handles.color = new Color(1f, 0f, 0f, 0.25f);
            Handles.DrawSolidDisc(transform.position, Vector3.forward, nearAttackRange);
            Handles.DrawSolidDisc(transform.position, Vector3.forward, farAttackRange);
        }
    }
#endif
    //Simple Debug Colllision Code:  If IDLE and collide with something, change waypoing.  If MovingAround player and collider with something, change direction
    public override void CollisionMovementDetection() //Feel free to rename this lmao
    {
        //called by child enemyHitbox object in OnCollisionEnter
        //just. exactly what was in Nick's original OnCollisionEnter2D
        //refactor into an event? 
        switch ((SlasherStates)enemyBrain.currentState) {
            case SlasherStates.IDLE:
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
        enemyBrain.applyTransition((uint)SlasherActions.STAGGER);
        yield return new WaitForSeconds(staggerTime);
        enemyBrain.applyTransition((uint)SlasherActions.EXIT_STAGGER);
        Debug.Log("Stagger Over!");
    }

}