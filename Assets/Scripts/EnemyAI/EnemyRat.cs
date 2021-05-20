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
    public int framesBetweenAIChecks = 3; //TODO: Move to Enemy (Should be consistent with curID which is consistent with enemyID)
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
    public float attackForce;
    private bool isAttacking;
    private Vector2 attackDirection;
    [Header("Follow Through")]
    public float minFollowThroughTime = 1;
    public float maxFollowThroughTime = 3;
    public float followThroughSpeed = 8;
    private bool isFollowingThrough;

    //NOTE: FSM is just public for debug
    public FSM ratBrain;   //TODO: Move to Enemy and rename enemyBrain
    static int curID = 0; //TODO: Move to Enemy
    private int enemyID; //TODO: Move to Enemy
    private GameObject player;
    private Vector2 currentDir;
    private Rigidbody2D rb2d;
    private Animator animator;
    private int layerMask;
    private int wallMask;
    private int enemyMask;

    private bool hasStarted = false; //Useful for drawGizmos
    private float currentSpeed;

    [SerializeField] AnimationClip ratAttackAnim;

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
        ratBrain.addTransition((uint) RatStates.MOVE_PAST_PLAYER,    (uint)RatStates.MOVE_AROUND_PLAYER,   (uint)RatActions.SPOTS_PLAYER);

        //Set Enemy ID
        enemyID = curID;
        curID += 1;

        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        layerMask = LayerMask.GetMask("Hitbox", "Map");
        wallMask = LayerMask.GetMask("Map");
        enemyMask = LayerMask.GetMask("Enemies");
        isAttacking = false;
        nextPos = transform.position;
        initialPos = transform.position;
        currentSpeed = 0;
        flipDirection = false;

        //TODO: Instead of doing this do something with ReferenceManager/PlayerScript as a singleton.
        player = GameObject.FindGameObjectWithTag("Player"); //find player game object

        hasStarted = true;

        animator.SetBool("Moving", true); // will need to change, right now is a placeholder as there is no time when the enemy isn't moving

        attackTime = ratAttackAnim.length;
    }

    // Update is called once per frame
    void Update() {
        
        //Calculate desired movement
        switch ((RatStates)ratBrain.currentState) {
            case RatStates.IDLE:
                // Moves randomly around/within a confined area
                if(Vector2.Distance(transform.position, nextPos) <= 0.5) {
                    //Generates a random point within the circle via polar coordinates
                    float r = Random.Range(0, patrolRadius);
                    float angle = Random.Range((float) 0, 2) * Mathf.PI;
                    //Shoot out a raycast to find any walls in the given direction, and scale down r accordingly to prevent any collisions
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2 (Mathf.Cos(angle), Mathf.Sin(angle)), patrolRadius * 1.2f, wallMask);
                    if(hit) {
                        r = r * (hit.distance / (patrolRadius * 1.2f));
                    }
                    nextPos = initialPos + new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
                }

                currentDir = Vector2.Lerp(currentDir, (nextPos - (Vector2)transform.position).normalized, lerpCoefficient);
                currentSpeed = patrolSpeed;
                break;
            case RatStates.MOVE_AROUND_PLAYER:
                // TODO: Moves around, rather quickly, in a wide range, generally towards the player and then continuing past the player if not ready to attack.
                // Even when moving past player, tries to keep out of player's attack range

                Vector2 newDir = (flipDirection ? -1 : 1) * Vector2.Perpendicular(player.transform.position - transform.position).normalized;

                if (Vector2.Distance(player.transform.position, transform.position) > enemyCircleDistance + enemyCircleTolerance) {
                    newDir += (Vector2) (player.transform.position - transform.position).normalized;
                } else if (Vector2.Distance(player.transform.position, transform.position) < enemyCircleDistance - enemyCircleTolerance) {
                    newDir -= (Vector2) (player.transform.position - transform.position).normalized;
                }

                //TODO: Use some sort of smooth noise to control jitter instead of the sinusoid
                //noise = Mathf.Sin(Time.time * jitterSpeed - enemyID) + Mathf.Sin(-3 * Time.time * jitterSpeed + enemyID);
                //noise = Mathf.Lerp(noise, Random.Range(-1f, 1), lerpCoefficient);
                noise = Mathf.PerlinNoise(Time.time % 1, enemyID * 100);
                jitter =  noise * jitterStrength * (player.transform.position - transform.position).normalized;
                newDir = (newDir + jitter).normalized;


                currentDir = Vector2.Lerp(currentDir, newDir, lerpCoefficient);

                // Starts a timer with a random amount of seconds [2,4] seconds, then is "Ready to Attack"
                if (!isPreparingToAttack) {
                    StartCoroutine(PrepareToAttack());
                }

                currentSpeed = enemySpeed;

                break;
            case RatStates.MOVE_TOWARDS_PLAYER:
                // TODO: Move towards player, but still include a little bit of randomness/jitter perpendicular to the player's location to keep things interesting

                //TODO: Use some sort of smooth noise to control jitter instead of the sinusoid
                //noise = Mathf.Sin(Time.time * jitterSpeed - enemyID) + Mathf.Sin(-3 * Time.time * jitterSpeed + enemyID);
                //noise = Mathf.Lerp(noise, Random.Range(-1f, 1), lerpCoefficient);
                noise = Mathf.PerlinNoise(Time.time % 1, enemyID*100);
                jitter =  noise * jitterStrength *  Vector2.Perpendicular(player.transform.position - transform.position).normalized;
                currentDir = Vector2.Lerp(currentDir,  ((Vector2) (player.transform.position - transform.position).normalized + jitter).normalized, lerpCoefficient);

                currentSpeed = enemySpeed;
                // When in range of attack, "Attack Player"
                if (Vector2.Distance(player.transform.position, transform.position) < attackRange) {
                    ratBrain.applyTransition((uint)RatActions.IN_ATTACK_RANGE);
                }
                break;
            case RatStates.ATTACK_PLAYER:
                // TODO: Attack is a "long jump/long/slash"?  Animation will need a function to call that propels enemy forward in the direction of the player
                // (Can and should go a little past) when wind up is done
                if(!isAttacking) {
                    StartCoroutine(AttackPlayer());
                }

                //DEBUG: Stop when attacking.  Since there's no attack animation, just easier for me to tell
                //currentDir = Vector2.zero;
               // currentSpeed = 0;
                break;
            case RatStates.MOVE_PAST_PLAYER:
                // TODO: Completes momentum of attack and then continues forward a random amount within a range
                // When random amount within the range has been reached, "Stop Move Past"
                currentSpeed = followThroughSpeed;
                if(!isFollowingThrough) {
                    StartCoroutine(FollowThrough());
                }
                break;
        }

        // Enemies check for certain transitions not every frame for efficiency, and checks are offset based on enemyID so different enemy checks are at different frames.
        if (Time.frameCount % framesBetweenAIChecks == enemyID % framesBetweenAIChecks) {
            //Separate case statement for potentially intensive state transition checks
            switch ((RatStates)ratBrain.currentState) {
                case RatStates.IDLE:
                    //SPOTS_PLAYER code if in idle state: If player is within range, raycast to check if you see them
                    if (Vector2.Distance(player.transform.position, transform.position) <= viewDistance) {
                        RaycastHit2D hit = Physics2D.Raycast(transform.position, player.transform.position - transform.position, Vector2.Distance(transform.position, player.transform.position), layerMask);
                        if (hit && hit.collider.tag.Equals("playerHitbox")) {
                            ratBrain.applyTransition((uint)RatActions.SPOTS_PLAYER);
                        }
                    }
                    break;
                default:
                    break;
            }

        }
        if (!isAttacking)
        {
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
        rb2d.velocity = Vector2.zero;
        yield return new WaitForSeconds(attackTime);
        ratBrain.applyTransition((uint)RatActions.ATTACK_OVER);
        isAttacking = false;
        //currentDir = -(player.transform.position - transform.position).normalized; //Setting currentDir here as it's an easy "only once before MOVE_PAST_PLAYER"
        //momentum should continue onward in the direction of the attack
        currentDir = attackDirection;
    }

    private IEnumerator PrepareToAttack() {
        isPreparingToAttack = true;
        float waitTime = Random.Range(minReadyToAttackTime, maxReadyToAttackTime);
        yield return new WaitForSeconds(waitTime);
        ratBrain.applyTransition((uint)RatActions.READY_TO_ATTACK);
        isPreparingToAttack = false;
    }

    private IEnumerator FollowThrough() {
        isFollowingThrough = true;
        float waitTime = Random.Range(minFollowThroughTime, maxFollowThroughTime);
        yield return new WaitForSeconds(waitTime);
        ratBrain.applyTransition((uint)RatActions.SPOTS_PLAYER);
        isFollowingThrough = false;
    }

    // called by the attack animation when wind up is done
    public void AddAttackForce(AnimationEvent evt)
    {
        //need to check the weight, otherwise mecanim calls all animation events on all anims in the blend tree at once. 
        if (evt.animatorClipInfo.weight > 0.5)
        {
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
                case RatStates.MOVE_AROUND_PLAYER:
                    Handles.color = new Color(1f, 0f, 1f, 1f);
                    Handles.DrawWireDisc(player.transform.position, Vector3.forward, enemyCircleDistance + enemyCircleTolerance);
                    Handles.color = new Color(1f, 0f, 1f, 1f);
                    Handles.DrawWireDisc(player.transform.position, Vector3.forward, enemyCircleDistance - enemyCircleTolerance);
                    break;
                case RatStates.MOVE_TOWARDS_PLAYER:
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

            //Attack Draws: Attack Range
            Handles.color = new Color(1f, 0f, 0f, 0.25f);
            Handles.DrawSolidDisc(transform.position, Vector3.forward, attackRange);

        }
    }

    //Simple Debug Colllision Code:  If IDLE and collide with something, change waypoing.  If MovingAround player and collider with something, change direction
    private void OnCollisionEnter2D(Collision2D collision) {
        switch ((RatStates)ratBrain.currentState) {
            case RatStates.IDLE:
                //Generates a random point within the circle via polar coordinates
                float r = Random.Range(0, patrolRadius);
                float angle = Random.Range((float)0, 2) * Mathf.PI;
                //Shoot out a raycast to find any walls in the given direction, and scale down r accordingly to prevent any collisions
                RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), patrolRadius * 1.2f, wallMask);
                if (hit) {
                    r = r * (hit.distance / (patrolRadius * 1.2f));
                }
                nextPos = initialPos + new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
                break;
            case RatStates.MOVE_AROUND_PLAYER:
                flipDirection = !flipDirection;
                break;
        }    
    }
}