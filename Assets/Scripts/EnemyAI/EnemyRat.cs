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
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyRat : Enemy {
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
    public bool isAttacking;
    private Vector2 attackDirection;
    [Header("Follow Through")]
    public float minFollowThroughTime = 1;
    public float maxFollowThroughTime = 3;
    public float followThroughSpeed = 8;
    private bool isFollowingThrough;

    private Vector2 currentDir;

    [SerializeField] private float currentSpeed;
    private float r;
    private float angle;

    [SerializeField] AnimationClip ratAttackAnim;
    [SerializeField] AnimationClip ratStunAnim;
    private List<GameObject> hitGOs = new List<GameObject>(); //a list of game objects the enemy has hit in one strike. used to check for double hits. 
    

    //Ensuring that enums convert cleanly to uint as expected
    enum RatStates : uint {
        IDLE,                   // 0
        MOVE_AROUND_PLAYER,     // 1
        MOVE_TOWARDS_PLAYER,    // 2
        ATTACK_PLAYER,          // 3
        MOVE_PAST_PLAYER,       // 4
        STAGGER,                // 5
        NUM_STATES              // 6
    }

    enum RatActions : uint {
        SPOTS_PLAYER,           // 0
        READY_TO_ATTACK,        // 1
        IN_ATTACK_RANGE,        // 2
        ATTACK_OVER,            // 3
        STOP_MOVE_PAST,         // 4
        STAGGER,                // 5
        EXIT_STAGGER,           // 6
        NUM_ACTIONS             // 7
    }

    // Start is called before the first frame update
    void Start() {
        //Initialize FSM with proper initial state and transitions
        enemyBrain = new FSM((uint) RatStates.IDLE);
        enemyBrain.addTransition((uint) RatStates.IDLE,                (uint) RatStates.MOVE_AROUND_PLAYER,  (uint) RatActions.SPOTS_PLAYER);
        enemyBrain.addTransition((uint) RatStates.MOVE_AROUND_PLAYER,  (uint) RatStates.MOVE_TOWARDS_PLAYER, (uint) RatActions.READY_TO_ATTACK);
        enemyBrain.addTransition((uint) RatStates.MOVE_TOWARDS_PLAYER, (uint) RatStates.ATTACK_PLAYER,       (uint) RatActions.IN_ATTACK_RANGE);
        enemyBrain.addTransition((uint) RatStates.ATTACK_PLAYER,       (uint) RatStates.MOVE_PAST_PLAYER,    (uint) RatActions.ATTACK_OVER);
        enemyBrain.addTransition((uint) RatStates.MOVE_PAST_PLAYER,    (uint) RatStates.MOVE_AROUND_PLAYER,  (uint)RatActions.SPOTS_PLAYER);
        enemyBrain.addTransition((uint) RatStates.STAGGER,             (uint) RatStates.MOVE_AROUND_PLAYER,  (uint) RatActions.EXIT_STAGGER);
        enemyBrain.addTransition((uint) RatStates.STAGGER,             (uint) RatActions.STAGGER);


        isAttacking = false;
        nextPos = transform.position;
        initialPos = transform.position;
        currentSpeed = 0;
        flipDirection = false;

        InitializeEnemy();

       //animator.SetBool("Moving", true); // will need to change, right now is a placeholder as there is no time when the enemy isn't moving

        attackTime = ratAttackAnim.length;
        staggerTime = ratStunAnim.length;
    }

    // Update is called once per frame
    void Update() {

        //Calculate desired movement
        switch ((RatStates)enemyBrain.currentState) {
            case RatStates.IDLE:
                // Moves randomly around/within a confined area
                if(Vector2.Distance(transform.position, nextPos) <= 0.5) {
                    //Generates a random point within the circle via polar coordinates
                    r = Random.Range(0, patrolRadius);
                    angle = Random.Range((float) 0, 2) * Mathf.PI;
                    //Shoot out a raycast to find any walls in the given direction, and scale down r accordingly to prevent any collisions
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2 (Mathf.Cos(angle), Mathf.Sin(angle)), patrolRadius * 1.2f, wallMask);
                    if(hit) {
                        r = r * (hit.distance / (patrolRadius * 1.2f));
                    }
                    nextPos = initialPos + new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
                }
                animator.SetBool("Moving", true);
                currentDir = Vector2.Lerp(currentDir, (nextPos - (Vector2)transform.position).normalized, lerpCoefficient);
                currentSpeed = patrolSpeed;
                break;
            case RatStates.MOVE_AROUND_PLAYER:
                // TODO: Moves around, rather quickly, in a wide range, generally towards the player and then continuing past the player if not ready to attack.
                // Even when moving past player, tries to keep out of player's attack range
                animator.SetBool("Moving", true);
                animator.SetBool("Idling", false);
                if (isPreparingToAttack) {
                    nextPos = (Vector2)player.transform.position + new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
                    if (Vector2.Distance(transform.position, nextPos) <= 0.5) {
                        r = Random.Range(enemyCircleDistance - enemyCircleTolerance, enemyCircleDistance + enemyCircleTolerance);
                        angle = Random.Range((float)0, .75f) * Mathf.PI * (flipDirection ? -1 : 1);
                        angle += Mathf.Atan2((player.transform.position - transform.position).y, (player.transform.position - transform.position).x);
                    }
                }

                // TODO: Better Jitter
                noise = Mathf.PerlinNoise(Time.time % 1, enemyID * 100);
                jitter =  noise * jitterStrength * Vector2.Perpendicular(nextPos - (Vector2)transform.position).normalized;

                currentDir = Vector2.Lerp(currentDir, ((nextPos - (Vector2)transform.position).normalized + jitter).normalized, lerpCoefficient);

                // Starts a timer with a random amount of seconds [2,4] seconds, then is "Ready to Attack"
                if (!isPreparingToAttack) {
                    StartCoroutine(PrepareToAttack());
                    r = Random.Range(enemyCircleDistance - enemyCircleTolerance, enemyCircleDistance + enemyCircleTolerance);
                    angle = Random.Range((float)0, .75f) * Mathf.PI * (flipDirection ? -1 : 1);
                }

                currentSpeed = enemySpeed;

                break;
            case RatStates.MOVE_TOWARDS_PLAYER:
                // Move towards player, but still include a little bit of randomness/jitter perpendicular to the player's location to keep things interesting
                // TODO: Better Jitter
                noise = Mathf.PerlinNoise(Time.time % 1, enemyID*100);
                jitter =  noise * jitterStrength *  Vector2.Perpendicular(player.transform.position - transform.position).normalized;
                currentDir = Vector2.Lerp(currentDir,  ((Vector2) (player.transform.position - transform.position).normalized + jitter).normalized, lerpCoefficient);
                animator.SetBool("Moving", true);
                animator.SetBool("Idling", false);
                currentSpeed = enemySpeed;
                // When in range of attack, "Attack Player"
                if (Vector2.Distance(player.transform.position, transform.position) < attackRange) {
                    enemyBrain.applyTransition((uint)RatActions.IN_ATTACK_RANGE);
                }

                break;
            case RatStates.ATTACK_PLAYER:
                // TODO: Attack is a "long jump/long/slash"?  Animation will need a function to call that propels enemy forward in the direction of the player
                // (Can and should go a little past) when wind up is done
                if(!isAttacking) {
                    StartCoroutine(AttackPlayer());
                }
                currentSpeed = enemySpeed;

                break;
            case RatStates.MOVE_PAST_PLAYER:
                // TODO: Completes momentum of attack and then continues forward a random amount within a range
                // When random amount within the range has been reached, "Stop Move Past"
                animator.SetBool("Moving", true);
                animator.SetBool("Idling", false);
                currentSpeed = followThroughSpeed;
                if(!isFollowingThrough) {
                    StartCoroutine(FollowThrough());
                }
                break;
            case RatStates.STAGGER:
                towardsPlayer = (player.transform.position - transform.position).normalized;

                rb2d.velocity = -towardsPlayer.normalized * knockBackForce;
                //rb2d.velocity = new Vector2(1,0) * knockBackForce;
                //currentSpeed = 0;
                break;
        }

        // Enemies check for certain transitions not every frame for efficiency, and checks are offset based on enemyID so different enemy checks are at different frames.
        if (Time.frameCount % framesBetweenAIChecks == enemyID % framesBetweenAIChecks) {
            //Separate case statement for potentially intensive state transition checks
            switch ((RatStates)enemyBrain.currentState) {
                case RatStates.IDLE:
                    //SPOTS_PLAYER code if in idle state: If player is within range, raycast to check if you see them
                    if (Vector2.Distance(player.transform.position, transform.position) <= viewDistance) {
                        RaycastHit2D hit = Physics2D.Raycast(transform.position, player.transform.position - transform.position, Vector2.Distance(transform.position, player.transform.position), layerMask);
                        if (hit && hit.collider.tag.Equals("playerHitbox")) {
                            enemyBrain.applyTransition((uint)RatActions.SPOTS_PLAYER);
                        }
                    }
                    break;
                default:
                    break;
            }

        }
        if (!isAttacking && !knockedBack)
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
        enemyBrain.applyTransition((uint)RatActions.ATTACK_OVER);
        isAttacking = false;
    }

    private IEnumerator PrepareToAttack() {
        isPreparingToAttack = true;
        float waitTime = Random.Range(minReadyToAttackTime, maxReadyToAttackTime);
        yield return new WaitForSeconds(waitTime);
        enemyBrain.applyTransition((uint)RatActions.READY_TO_ATTACK);
        isPreparingToAttack = false;
    }

    private IEnumerator FollowThrough() {
        isFollowingThrough = true;
        float waitTime = Random.Range(minFollowThroughTime, maxFollowThroughTime);
        yield return new WaitForSeconds(waitTime);
        enemyBrain.applyTransition((uint)RatActions.SPOTS_PLAYER);
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
#if UNITY_EDITOR
    private void OnDrawGizmos() {
        //Drawing Gizmos like radius for debug purposes in editor.  Nothing here will be drawn in build :)
        if (!drawGizmos) {
            return;
        }
        //Since lots of things aren't initialized until the editor's started, need a conditional branch based on whether or not Start has been called (aka whether or not you're editing in the editor)
        if (hasStarted) {
            switch ((RatStates)enemyBrain.currentState) {
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
                    Debug.DrawLine(transform.position, nextPos, Color.cyan);
                    Handles.color = new Color(0f, 1f, 1f, 0.25f);
                    Handles.DrawSolidDisc(nextPos, Vector3.forward, 0.25f);
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
#endif
    //Simple Debug Colllision Code:  If IDLE and collide with something, change waypoing.  If MovingAround player and collider with something, change direction
    public override void CollisionMovementDetection() //Feel free to rename this lmao
    {
        //called by child enemyHitbox object in OnCollisionEnter
        //just. exactly what was in Nick's original OnCollisionEnter2D
        //refactor into an event? 
        switch ((RatStates)enemyBrain.currentState) {
            case RatStates.IDLE:
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
            case RatStates.MOVE_AROUND_PLAYER:
                flipDirection = !flipDirection;
                r = Random.Range(enemyCircleDistance - enemyCircleTolerance, enemyCircleDistance + enemyCircleTolerance);
                angle = Random.Range((float)0, .75f) * Mathf.PI * (flipDirection ? -1 : 1);
                angle += Mathf.Atan2((player.transform.position - transform.position).y, (player.transform.position - transform.position).x);
                break;
        }    

    }

    // need to figure something out with colliders to refactor with Nick 
    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.tag == "playerHitbox")
        {
            if (!hitGOs.Contains(collision.gameObject))
            {
                //at the moment, the only gameobject the enemy should ever hit will be the player, however
                //i want this to be expandable in case we have a "two player" system in the final boss battle or there are other edge cases
                hitGOs.Add(collision.gameObject);
                player.GetComponent<PlayerScript>().ChangePlayerHealth(-attackDamage, "hit");
                //play player taken damage animation? 
            }
        }
    }

    //called by attack animation event 
    public void RemoveFromHitGOs()
    {
        hitGOs.Clear();
    }

    protected override IEnumerator Stagger() {
        enemyBrain.applyTransition((uint)RatActions.STAGGER);
        isAttacking = false;
        currentSpeed = 0;
        yield return new WaitForSeconds(staggerTime);
        enemyBrain.applyTransition((uint)RatActions.EXIT_STAGGER);
    }

    public override void SpawnAshes()
    {
        GameObject ashes = Instantiate(_refMan.ashesGO, transform.position, Quaternion.identity);
        Ashes2 ashesScript = ashes.GetComponent<Ashes2>();
        ashesScript.despawnDuration = 10f;
        ashesScript.despawnRate = 1f;
        ashesScript.rechargeRate = 6f;
        ashesScript.acceptHealAmount = 2f;
    }
}