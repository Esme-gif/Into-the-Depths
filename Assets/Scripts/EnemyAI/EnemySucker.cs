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

public class EnemySucker : Enemy {
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
    public float slapTime;
    public float suckTime;
    public float slapAttackRange = 1;
    public float suckAttackRange = 2;
    public float attackForce;
    [Range(0,1)] public float slapChance = 0.8f;
    private bool isAttacking;
    private Vector2 attackDirection;
    private SuckerAttack targetAttack;
    private enum SuckerAttack {
        UNDECIDED,
        SLAP,
        SUCK
    };

    private Vector2 currentDir;

    private float currentSpeed;
    private float r;
    private float angle;

    [SerializeField] AnimationClip suckerSlapAnim;
    [SerializeField] AnimationClip suckerSuckAnim;

    private List<GameObject> hitGOs = new List<GameObject>(); //a list of game objects the enemy has hit in one strike. used to check for double hits. 

    //Ensuring that enums convert cleanly to uint as expected
    enum SuckerStates : uint {
        IDLE,                   // 0
        MOVE_AROUND_PLAYER,     // 1
        IN_EITHER_RANGE,        // 2
        READY_TO_ATTACK,        // 3
        SLAP_ATTACK,            // 4
        SUCK_ATTACK,            // 5
        STAGGER,                // 6
        NUM_STATES              // 7
    }

    enum SuckerActions : uint {
        SPOTS_PLAYER,           // 0
        SLAP_RANGE,             // 1
        SUCK_RANGE,             // 2
        READY_TO_ATTACK,        // 3
        ATTACK_OVER,            // 4
        STAGGER,                // 5
        EXIT_STAGGER,           // 6
        NUM_ACTIONS             // 7
    }

    // Start is called before the first frame update
    void Start() {

        //Initialize FSM with proper initial state and transitions
        enemyBrain = new FSM((uint)SuckerStates.IDLE);
        enemyBrain.addTransition((uint)SuckerStates.IDLE, (uint)SuckerStates.MOVE_AROUND_PLAYER, (uint)SuckerActions.SPOTS_PLAYER);
        enemyBrain.addTransition((uint)SuckerStates.MOVE_AROUND_PLAYER, (uint)SuckerStates.READY_TO_ATTACK, (uint)SuckerActions.READY_TO_ATTACK);
        // NOTE: While the FSM is setup to trigger from ready -> specific attack when the range action is proc'ed, it's only applied when that is the desired attack
        enemyBrain.addTransition((uint)SuckerStates.READY_TO_ATTACK, (uint)SuckerStates.SLAP_ATTACK, (uint)SuckerActions.SLAP_RANGE);
        enemyBrain.addTransition((uint)SuckerStates.READY_TO_ATTACK, (uint)SuckerStates.SUCK_ATTACK, (uint)SuckerActions.SUCK_RANGE);
        enemyBrain.addTransition((uint)SuckerStates.SLAP_ATTACK, (uint)SuckerStates.MOVE_AROUND_PLAYER, (uint)SuckerActions.ATTACK_OVER);
        enemyBrain.addTransition((uint)SuckerStates.SUCK_ATTACK, (uint)SuckerStates.MOVE_AROUND_PLAYER, (uint)SuckerActions.ATTACK_OVER);
        enemyBrain.addTransition((uint)SuckerStates.STAGGER, (uint)SuckerStates.MOVE_AROUND_PLAYER, (uint)SuckerActions.EXIT_STAGGER);
        enemyBrain.addTransition((uint)SuckerStates.STAGGER, (uint)SuckerActions.STAGGER);

        isAttacking = false;
        isPreparingToAttack = false;

        nextPos = transform.position;
        initialPos = transform.position;
        currentSpeed = 0;

        InitializeEnemy();

        animator.SetBool("Moving", true); // will need to change, right now is a placeholder as there is no time when the enemy isn't moving

        slapTime = suckerSlapAnim.length;
        suckTime = suckerSuckAnim.length;
        targetAttack = SuckerAttack.UNDECIDED;
    }

    // Update is called once per frame
    void Update() {
        //Calculate desired movement
        switch ((SuckerStates)enemyBrain.currentState) {
            case SuckerStates.IDLE:
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
            case SuckerStates.MOVE_AROUND_PLAYER: // TODO: MOVE_AROUND_PLAYER
                // Even when moving past player, tries to keep out of player's attack range
                animator.SetBool("Moving", true);
                animator.SetBool("Idling", false);

                if (isPreparingToAttack) {
                    nextPos = (Vector2)player.transform.position + new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
                    if (Vector2.Distance(transform.position, nextPos) <= 0.5) {
                        r = Random.Range(enemyCircleDistance - enemyCircleTolerance, enemyCircleDistance + enemyCircleTolerance);
                        angle = Random.Range(0f, .75f) * Mathf.PI * (flipDirection ? -1 : 1);
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
                    angle = Random.Range((float)0f, .75f) * Mathf.PI * (flipDirection ? -1 : 1);
                    angle += Mathf.Atan2((transform.position - player.transform.position).y, (transform.position - player.transform.position).x);
                }

                currentSpeed = enemySpeed;
                break;
            case SuckerStates.READY_TO_ATTACK:
                // Move towards player, but still include a little bit of randomness/jitter perpendicular to the player's location to keep things interesting
                // TODO: Better Jitter
                noise = Mathf.PerlinNoise(Time.time % 1, enemyID * 100);
                jitter = noise * jitterStrength * Vector2.Perpendicular(player.transform.position - transform.position).normalized;
                currentDir = Vector2.Lerp(currentDir, ((Vector2)(player.transform.position - transform.position).normalized + jitter).normalized, lerpCoefficient);
                animator.SetBool("Moving", true);
                animator.SetBool("Idling", false);
                currentSpeed = enemySpeed;

                // Randomly pick between Suck / Slap
                if(targetAttack == SuckerAttack.UNDECIDED) {
                    if (Random.Range(0f, 1f) <= slapChance) {
                        targetAttack = SuckerAttack.SLAP;
                    } else {
                        targetAttack = SuckerAttack.SUCK;
                    }
                }

                // When in range of target attack, then apply the transition
                if(targetAttack == SuckerAttack.SLAP && Vector2.Distance(player.transform.position, transform.position) < slapAttackRange) {
                    enemyBrain.applyTransition((uint)SuckerActions.SLAP_RANGE);
                    targetAttack = SuckerAttack.UNDECIDED;
                }

                if(targetAttack == SuckerAttack.SUCK && Vector2.Distance(player.transform.position, transform.position) < suckAttackRange) {
                    enemyBrain.applyTransition((uint)SuckerActions.SUCK_RANGE);
                    targetAttack = SuckerAttack.UNDECIDED;
                }

                break;
            case SuckerStates.SLAP_ATTACK:
                // TODO: Attack is a "long jump/long/slash"?  Animation will need a function to call that propels enemy forward in the direction of the player
                // (Can and should go a little past) when wind up is done
                if (!isAttacking) {
                    StartCoroutine(Slap());
                }
                break;
            case SuckerStates.SUCK_ATTACK:
                if (!isAttacking) {
                    StartCoroutine(Suck());
                }
                break;
            case SuckerStates.STAGGER:
                currentSpeed = 0;
                break;
        }

        // Enemies check for certain transitions not every frame for efficiency, and checks are offset based on enemyID so different enemy checks are at different frames.
        if (Time.frameCount % framesBetweenAIChecks == enemyID % framesBetweenAIChecks) {
            //Separate case statement for potentially intensive state transition checks
            switch ((SuckerStates)enemyBrain.currentState) {
                case SuckerStates.IDLE:
                    //SPOTS_PLAYER code if in idle state: If player is within range, raycast to check if you see them
                    if (Vector2.Distance(player.transform.position, transform.position) <= viewDistance) {
                        RaycastHit2D hit = Physics2D.Raycast(transform.position, player.transform.position - transform.position, Vector2.Distance(transform.position, player.transform.position), layerMask);
                        if (hit && hit.collider.tag.Equals("playerHitbox")) {
                            enemyBrain.applyTransition((uint)SuckerActions.SPOTS_PLAYER);
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

    private IEnumerator Slap() {
        Debug.Log("SLAP!");
        //NOTE: Simple implementation assuming attackTime is something we know.  May be complexified later, but is abstracted for that reason
        isAttacking = true;
        attackDirection = currentDir.normalized;
        animator.SetFloat("FaceX", attackDirection.x);
        animator.SetTrigger("Attack"); // TODO: Change this to be custom based on new animator
        rb2d.velocity = Vector2.zero;
        yield return new WaitForSeconds(slapTime);

        enemyBrain.applyTransition((uint)SuckerActions.ATTACK_OVER);

        isAttacking = false;
    }

    private IEnumerator Suck() {
        Debug.Log("SUCK!");
        //NOTE: Simple implementation assuming attackTime is something we know.  May be complexified later, but is abstracted for that reason
        isAttacking = true;
        attackDirection = currentDir.normalized;
        animator.SetFloat("FaceX", attackDirection.x);
        animator.SetTrigger("Attack"); // TODO: Change this to be custom based on new animator
        rb2d.velocity = Vector2.zero;
        yield return new WaitForSeconds(suckTime);
        enemyBrain.applyTransition((uint)SuckerActions.ATTACK_OVER);
        isAttacking = false;
    }

    private IEnumerator PrepareToAttack() {
        isPreparingToAttack = true;
        float waitTime = Random.Range(minReadyToAttackTime, maxReadyToAttackTime);
        yield return new WaitForSeconds(waitTime);
        enemyBrain.applyTransition((uint)SuckerActions.READY_TO_ATTACK);
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
            switch ((SuckerStates)enemyBrain.currentState) {
                case SuckerStates.IDLE:
                    Handles.color = new Color(0, 1f, 0f, 1);
                    Handles.DrawWireDisc(initialPos, Vector3.forward, patrolRadius);
                    Debug.DrawLine(transform.position, nextPos, Color.green);
                    Handles.color = new Color(0f, 1f, 0f, 0.25f);
                    Handles.DrawSolidDisc(nextPos, Vector3.forward, 0.25f);
                    Handles.color = new Color(1f, 1f, 0f, 0.25f);
                    Handles.DrawSolidDisc(transform.position, Vector3.forward, viewDistance);
                    break;
                case SuckerStates.MOVE_AROUND_PLAYER:
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
            Handles.DrawSolidDisc(transform.position, Vector3.forward, slapAttackRange);
            Handles.DrawSolidDisc(transform.position, Vector3.forward, suckAttackRange);
        }
    }
#endif

    //Simple Debug Colllision Code:  If IDLE and collide with something, change waypoing.  If MovingAround player and collider with something, change direction
    public override void CollisionMovementDetection() //Feel free to rename this lmao
    {
        //called by child enemyHitbox object in OnCollisionEnter
        //just. exactly what was in Nick's original OnCollisionEnter2D
        //refactor into an event? 
        switch ((SuckerStates)enemyBrain.currentState) {
            case SuckerStates.IDLE:
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
                Debug.Log("Sucker hit the player!");
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
        enemyBrain.applyTransition((uint)SuckerActions.STAGGER);
        yield return new WaitForSeconds(staggerTime);
        enemyBrain.applyTransition((uint)SuckerActions.EXIT_STAGGER);
        Debug.Log("Stagger Over!");
    }

}