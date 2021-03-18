using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    //object references
    public GameObject[] wayPoints; //empty game objects for the sprite to move between when idle
    public GameObject player; //Player game object

    //component references 
    Rigidbody2D enemyRB; //enemy's rigidbody
    Animator myAnimator; //enemy's animator component
    Animator myChildAnimator; //enemy childs' collidor animation component

    //states
    public bool hasSpotted = false; //has spotted the player
    public bool inRange; //in range of moving towards the player
    public bool inAttackRange = false; //in range for attacking the player
    public bool nextToPlayer = false; //directly next to the player - used to prevent standing ON TOP of player
    public bool isIdling;
    public bool knockedBack = false; //in knock back frames
    public bool canTakeDamage = true; //invincibility frames to prevent double hits
    public bool canAttack = true; //used to force time between attacks

    //stats
    public float knockBack; //knockback force
    public float health = 20;
    public float damage = 5; //damage enemy can do to player
    public float maxSightDistance; //distance enemy can "see" towards the player
    public float attackRange;
    public float enemySpeed;
    public float stopMoveRange; //distance away from the player the enemy should stop moving - to prevent overlap

    //cached/stored information
    Vector2 currentPosition; //current position of the enemy
    Vector2 nextPosition; //next position enemy will move towards while Idling
    int placeInWayPoints = 0; //way to mark where in waypoint array enemy is
    Vector2 towardsPlayer; //direction towards the player
    public Vector2 FacingDirection; //direction enemy is facing
    PlayerScript playerScript; //store player script

         
    // Start is called before the first frame update
    void Start()//what
    {
        isIdling = true; //begin idling
        currentPosition = wayPoints[0].transform.position; //set current place in waypoints
        nextPosition = wayPoints[1].transform.position; //set next waypoint to move towards
        enemyRB = GetComponent<Rigidbody2D>(); //get enemy rigidbody
        myAnimator = GetComponent<Animator>();
        myChildAnimator = transform.GetChild(0).GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player"); //find player game object
        playerScript = player.GetComponent<PlayerScript>(); //store player script
    }

    private void FixedUpdate()
    {
        CheckForMovementRange();
    }

    // Update is called once per frame
    void Update()
    {
        

        //if sees player, and is not in knock back frames, and is not DIRECTLY next to the player
        if (hasSpotted && !knockedBack && !nextToPlayer)
        {
            //move towards the player
            transform.position = Vector2.MoveTowards(transform.position, player.transform.position, enemySpeed * Time.deltaTime);
            //set animations
            myAnimator.SetBool("Walking", true);
            myChildAnimator.SetBool("Walking", true);
            myAnimator.SetBool("Idling", false);
            myChildAnimator.SetBool("Idling", false);
            FacingDirection = towardsPlayer; //set facing direction
            AnimBlendSetFloats(); //set facing direction floats for animation blend tree
        }
        else
        {
            if (isIdling)
            {
                //move between waypoints
                transform.position = Vector2.MoveTowards(transform.position, nextPosition, enemySpeed * Time.deltaTime);
                FacingDirection = (nextPosition - new Vector2(transform.position.x,transform.position.y)).normalized;
                AnimBlendSetFloats(); //set facing direction floats for animation blend tree
                if (new Vector2(transform.position.x, transform.position.y) == nextPosition) //if we have reached the next position
                {
                    isIdling = false; //stop moving
                    //set animations back to standing still/idling
                    myAnimator.SetBool("Walking", false);
                    myChildAnimator.SetBool("Walking", false);
                    myAnimator.SetBool("Idling", true);
                    myChildAnimator.SetBool("Idling", true);
                    StartCoroutine(IdleWait()); //start coroutine to wait then chose another waypoint and start moving again
                }
                else
                {
                    //set animations
                    myAnimator.SetBool("Walking", true);
                    myChildAnimator.SetBool("Walking", true);
                    myAnimator.SetBool("Idling", false);
                    myChildAnimator.SetBool("Idling", false);
                }
            }
        }
        //if in range to attack and can attack
        if (inAttackRange && canAttack)
        {
            StartCoroutine(BlockHealTrigger()); //start time window for player to block heal
            StartCoroutine(Attack());//start delay (for block heal window) and then attack
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("enemy triggered collision");
        Debug.Log(collision.name);
        if (canTakeDamage && collision.tag == "Player")
        {
            Debug.Log("hit an enemy!");
            health -= playerScript.damage;
            canTakeDamage = false;
            StartCoroutine(NoDoubleHit());

            if (health <= 0)
            {
                //die
                Destroy(transform.parent.gameObject);
            }

            //knock back
            knockedBack = true;
            StartCoroutine(KnockBackTimer());
            enemyRB.AddForce(-towardsPlayer.normalized * knockBack);
        }

    }

    private void CheckForMovementRange()
    {
        //calculate direction towards player
        towardsPlayer = (player.transform.position - transform.position).normalized;
        if (!hasSpotted) //if not has spotted player
        {
            //idle
            int layerMask = LayerMask.GetMask("Hitbox", "Map"); //make layer mask that includes only player and map
            Debug.DrawRay(transform.position, towardsPlayer, Color.red, 1f, false); //draw debug ray to see in scene view
            RaycastHit2D hit = Physics2D.Raycast(transform.position, towardsPlayer, maxSightDistance, layerMask); //send raycast towards player
            if (hit) //if hits something
            {
                Debug.Log("enemy raycast hit something!");
                if (hit.collider.tag == "playerHitbox") //if hit player
                {
                    //enemy is in range of seeing player
                    hasSpotted = true; //has spotted plaer
                    isIdling = false; //no longer idling (moving between waypoints)
                    inRange = true; //in range of movement
                }
                else //if hit something else (obstacle or edge of map)
                {
                    inRange = false; //not in range
                }
            }
            else //if raycast didn't hit anything
            {
                inRange = false; //not in range
            }
        }
        else //if enemy HAS spotted the player
        {
            CheckForAttackRange();
        }
    }

    private void CheckForAttackRange()
    {
        int layerMask = LayerMask.GetMask("Hitbox", "Map"); //make layer mask for only player and map
        Debug.DrawRay(transform.position, towardsPlayer, Color.red, 1f, false); //draw debug ray to view in scene view
        //draw ray towards player with attack range length
        RaycastHit2D hit = Physics2D.Raycast(transform.position, towardsPlayer, attackRange, layerMask);
        if (hit)
        {
            if (hit.collider.tag == "playerHitbox")
            {
                inAttackRange = true;
            }
            else
            {
                inAttackRange = false;
            }
        }
        else //if didn't hit anything
        {
            inAttackRange = false;
        }
        //draw ray towards player with length for stopping movement to prevent sprite overlap
        RaycastHit2D hit2 = Physics2D.Raycast(transform.position, towardsPlayer, stopMoveRange, layerMask);
        if (hit2)
        {
            if (hit.collider.tag == "playerHitbox")
            {
                nextToPlayer = true;
            }
            else
            {
                nextToPlayer = false;
            }
        }
        else
        {
            nextToPlayer = false;
        }
    }

    //timer for how long emeny is knocked back after being hit
    IEnumerator KnockBackTimer()
    {
        yield return new WaitForSeconds(.2f);
        knockedBack = false;
        enemyRB.velocity = Vector2.zero; //return velocity to zero!
    }

    //timer for a random amound of time to wait between movement between waypoints
    IEnumerator IdleWait()
    {
        //wait a random amount of seconds between 0 and 4
        yield return new WaitForSeconds(Random.Range(0, 5));
        isIdling = true; //start moving between waypoints again
        placeInWayPoints = Random.Range(0, wayPoints.Length); //choose a random waypoint to move towards
        nextPosition = wayPoints[placeInWayPoints].transform.position; //set that random waypoint as nextposition
    }

    //prevents weapon collider attack animation 
    //from dealing damage more than once an attack
    IEnumerator NoDoubleHit()
    {
        for (int i = 0; i < 20; i++)  //wait 3 frames
        {
            yield return null;
        }
        canTakeDamage = true; //then can take damage again
    }

    //draw circles on scene view to see attack and movement range of enemies
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, maxSightDistance);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    // set facing direction floats for animation blend tree (idling and walking)
    private void AnimBlendSetFloats()
    {
        myAnimator.SetFloat("FaceX", FacingDirection.x);
        myAnimator.SetFloat("FaceY", FacingDirection.y);
    }

    private void PlayAttackAnimations()
    {
        //checks direction enemy is facing, then triggers animations for that direction.
        if (FacingDirection.x > 0 &&
            Mathf.Abs(FacingDirection.x) > Mathf.Abs(FacingDirection.y))
        {
            myChildAnimator.SetTrigger("AttackRight");
            myAnimator.SetTrigger("AttackingRight");
        }
        if (FacingDirection.x < 0 &&
            Mathf.Abs(FacingDirection.x) > Mathf.Abs(FacingDirection.y))
        {
            myChildAnimator.SetTrigger("AttackLeft");
            myAnimator.SetTrigger("AttackingLeft");
        }
        if (FacingDirection.y > 0 &&
            Mathf.Abs(FacingDirection.y) > Mathf.Abs(FacingDirection.x))
        {
            myChildAnimator.SetTrigger("AttackUp");
            myAnimator.SetTrigger("AttackingUp");
        }
        if (FacingDirection.y < 0 &&
            Mathf.Abs(FacingDirection.y) > Mathf.Abs(FacingDirection.x))
        {
            myChildAnimator.SetTrigger("AttackDown");
            myAnimator.SetTrigger("AttackingDown");
        }

    }

    //plays attack after a short delay
    IEnumerator Attack()
    {
        canAttack = false;
        yield return new WaitForSeconds(.7f);
        Debug.Log("playing enemy attack anims");
        PlayAttackAnimations();
        StartCoroutine(playerScript.ResetWeaponHitGOs());
        yield return new WaitForSeconds(2);
        canAttack = true;
    }

    //Allows player to block heal
    IEnumerator BlockHealTrigger()
    {
        playerScript.canBlockHeal = true;
        //should eventually be the wind up time plus attack time
        yield return new WaitForSeconds(.7f + .2f);
        playerScript.canBlockHeal = false;
    }
}
